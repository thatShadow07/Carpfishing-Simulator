using UnityEngine;

[RequireComponent(typeof(FishFightController))]
public class FishAI : MonoBehaviour
{
    public enum FishState
    {
        Roaming,
        InvestigatingBait,
        Feeding,
        Hooked,
        Fighting,
        Tired,
        Landing,
        Lost,
        Landed
    }

    [Header("Fish Data")]
    [SerializeField] private FishSpeciesData speciesData;
    [SerializeField] private FishState currentState = FishState.Roaming;

    [Header("Swimming Area")]
    [SerializeField] private Vector3 swimCenter;
    [SerializeField, Min(1f)] private float swimRadius = 20f;
    [SerializeField] private WaterDepth waterDepth;
    [SerializeField, Min(0f)] private float minDepth = 1f;
    [SerializeField, Min(0f)] private float maxDepth = 8f;

    [Header("Roaming")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 3f;
    [SerializeField, Min(0.1f)] private float destinationReachDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float destinationChangeDelayMin = 3f;
    [SerializeField, Min(0.1f)] private float destinationChangeDelayMax = 8f;

    [Header("Bait")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 4f;
    [SerializeField, Min(0.1f)] private float investigateDistance = 0.9f;
    [SerializeField, Min(0.1f)] private float inspectTimeMin = 1f;
    [SerializeField, Min(0.1f)] private float inspectTimeMax = 4f;
    [SerializeField, Range(0f, 1f)] private float biteChance = 0.8f;
    [SerializeField, Range(0f, 1f)] private float hookChance = 0.95f;
    [SerializeField, Min(0f)] private float baitCooldown = 8f;
    [SerializeField, Min(0.1f)] private float feedingDistanceMultiplier = 1.35f;
    [SerializeField, Min(0.1f)] private float minimumHookDistance = 1.2f;

    [Header("Reaction")]
    [SerializeField, Min(0.1f)] private float fearDistance = 2f;
    [SerializeField, Min(0.1f)] private float escapeDistance = 8f;
    [SerializeField, Range(0f, 1f)] private float randomBehaviour = 0.15f;

    private FishFightController fightController;
    private Transform baitTarget;
    private Vector3 targetPosition;
    private float destinationTimer;
    private float inspectTimer;
    private float nextBaitCheckTime;

    public FishState CurrentState => currentState;
    public FishSpeciesData SpeciesData => speciesData;
    public float Caution => speciesData != null ? speciesData.Caution : 0.5f;
    public float Aggression => speciesData != null ? speciesData.Aggression : 0.5f;
    public float Intelligence => speciesData != null ? speciesData.Intelligence : 0.5f;
    public float FearDistance => fearDistance;
    public float EscapeDistance => escapeDistance;

    private void Awake()
    {
        fightController = GetComponent<FishFightController>();
    }

    private void Start()
    {
        ChooseNewDestination();
        ChangeState(FishState.Roaming);
    }

    private void Update()
    {
        switch (currentState)
        {
            case FishState.Roaming:
                UpdateRoaming();
                break;
            case FishState.InvestigatingBait:
                UpdateInvestigatingBait();
                break;
            case FishState.Feeding:
                UpdateFeeding();
                break;
            case FishState.Lost:
                ResumeRoamingAfterLost();
                break;
        }
    }

    private void UpdateRoaming()
    {
        destinationTimer -= Time.deltaTime;
        SwimTowards(targetPosition, GetSwimmingSpeed());
        DetectBait();

        if (destinationTimer <= 0f || Vector3.Distance(transform.position, targetPosition) <= destinationReachDistance)
            ChooseNewDestination();
    }

    private void DetectBait()
    {
        if (Time.time < nextBaitCheckTime) return;

        Sinker sinker = Sinker.Current;
        if (sinker == null || !sinker.IsInWater) return;

        float distance = Vector3.Distance(transform.position, sinker.transform.position);
        if (distance > detectionRadius) return;

        baitTarget = sinker.transform;
        inspectTimer = 0f;
        ChangeState(FishState.InvestigatingBait);
    }

    private void UpdateInvestigatingBait()
    {
        if (!IsBaitValid())
        {
            LeaveBait();
            return;
        }

        float distance = Vector3.Distance(transform.position, baitTarget.position);
        if (distance > escapeDistance)
        {
            LeaveBait();
            return;
        }

        // Outside the inspection zone the fish approaches normally.
        // Inside it the fish MUST stop translating. This prevents the old
        // overshoot/turn/overshoot loop that looked like trembling.
        if (distance > investigateDistance)
        {
            float approachSpeed = GetSwimmingSpeed() * Mathf.Lerp(1f, 0.45f, Caution);
            MoveTowardsWithoutOvershoot(baitTarget.position, approachSpeed, investigateDistance);
            return;
        }

        FaceTarget(baitTarget.position);
        inspectTimer += Time.deltaTime;

        float requiredInspection = Mathf.Lerp(inspectTimeMin, inspectTimeMax, Caution);
        if (inspectTimer < requiredInspection) return;

        float chance = biteChance;
        chance += (Aggression - 0.5f) * 0.2f;
        chance -= Caution * 0.15f;
        chance += Random.Range(-randomBehaviour, randomBehaviour);
        chance = Mathf.Clamp01(chance);

        if (Random.value <= chance)
        {
            inspectTimer = 0f;
            ChangeState(FishState.Feeding);
        }
        else
        {
            LeaveBait();
        }
    }

    private void UpdateFeeding()
    {
        if (!IsBaitValid())
        {
            LeaveBait();
            return;
        }

        float distance = Vector3.Distance(transform.position, baitTarget.position);
        float feedingDistance = Mathf.Max(minimumHookDistance, investigateDistance * feedingDistanceMultiplier);

        if (distance > escapeDistance)
        {
            LeaveBait();
            return;
        }

        // If the fish drifted slightly away, approach slowly. Never cross the bait.
        if (distance > feedingDistance)
        {
            MoveTowardsWithoutOvershoot(baitTarget.position, GetSwimmingSpeed() * 0.2f, feedingDistance);
            return;
        }

        // Stable feeding position: rotate only, do not translate.
        FaceTarget(baitTarget.position);
        inspectTimer += Time.deltaTime;

        float takeTime = Mathf.Lerp(0.6f, 2.2f, Caution);
        if (inspectTimer < takeTime) return;

        float chance = Mathf.Clamp01(hookChance + Aggression * 0.08f - Caution * 0.12f);
        if (Random.value > chance)
        {
            // The fish rejected the bait. It leaves naturally instead of getting
            // trapped in the feeding state.
            LeaveBait();
            return;
        }

        TryHookFish();
    }

    private void TryHookFish()
    {
        Sinker sinker = Sinker.Current;
        if (sinker == null || !sinker.IsInWater)
        {
            LeaveBait();
            return;
        }

        if (fightController == null)
            fightController = GetComponent<FishFightController>();

        baitTarget = sinker.transform;
        ChangeState(FishState.Hooked);
        fightController.BeginFight();

        if (fightController.IsFighting)
        {
            // FishFightController will immediately move us to Fighting through
            // OnFightStarted(). From this point FishAI no longer controls roaming.
            return;
        }

        // BeginFight refused the hook. Do not lock the fish in Hooked and do not
        // make it stare at the sinker forever. Give the rig a short retry window.
        Debug.LogWarning("FishAI: a fisgada foi tentada, mas o FishFightController não iniciou o combate. Verifica Player Transform e Sinker.");
        inspectTimer = 0f;
        nextBaitCheckTime = Time.time + 1f;
        ChangeState(FishState.Roaming);
    }

    private bool IsBaitValid()
    {
        return baitTarget != null && Sinker.Current != null && Sinker.Current.IsInWater;
    }

    private void ChooseNewDestination()
    {
        Vector2 randomPoint = Random.insideUnitCircle * swimRadius;
        float x = swimCenter.x + randomPoint.x;
        float z = swimCenter.z + randomPoint.y;
        float y = ChooseDepth(x, z);

        targetPosition = new Vector3(x, y, z);
        destinationTimer = Random.Range(destinationChangeDelayMin, Mathf.Max(destinationChangeDelayMin, destinationChangeDelayMax));
    }

    private float ChooseDepth(float x, float z)
    {
        if (waterDepth == null)
            return transform.position.y;

        Vector3 sample = new Vector3(x, waterDepth.SurfaceHeight, z);
        float bottom = waterDepth.GetBottomHeightAt(sample);
        float shallowest = Mathf.Max(bottom + 0.5f, waterDepth.SurfaceHeight - maxDepth);
        float deepest = Mathf.Min(waterDepth.SurfaceHeight - minDepth, waterDepth.SurfaceHeight - 0.1f);

        if (shallowest >= deepest)
            return transform.position.y;

        return Random.Range(shallowest, deepest);
    }

    private void SwimTowards(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void MoveTowardsWithoutOvershoot(Vector3 destination, float speed, float stopDistance)
    {
        Vector3 offset = destination - transform.position;
        float distance = offset.magnitude;
        if (distance <= stopDistance) return;

        Vector3 direction = offset / distance;
        Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);

        float maxStep = Mathf.Max(0f, distance - stopDistance);
        float step = Mathf.Min(speed * Time.deltaTime, maxStep);
        transform.position += transform.forward * step;
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * 2f * Time.deltaTime);
    }

    private float GetSwimmingSpeed()
    {
        return speciesData != null ? speciesData.SwimmingSpeed : moveSpeed;
    }

    private void LeaveBait()
    {
        baitTarget = null;
        inspectTimer = 0f;
        nextBaitCheckTime = Time.time + baitCooldown;
        ChooseNewDestination();
        ChangeState(FishState.Roaming);
    }

    public void OnFightStarted() => ChangeState(FishState.Fighting);

    public void OnFightTired() => ChangeState(FishState.Tired);

    public void OnFishLanded()
    {
        baitTarget = null;
        ChangeState(FishState.Landed);
    }

    public void OnFishLost()
    {
        baitTarget = null;
        nextBaitCheckTime = Time.time + baitCooldown;
        ChooseNewDestination();
        ChangeState(FishState.Lost);
    }

    public void ResumeRoamingAfterLost()
    {
        if (currentState == FishState.Lost)
            ChangeState(FishState.Roaming);
    }

    private void ChangeState(FishState newState)
    {
        currentState = newState;
    }
}
