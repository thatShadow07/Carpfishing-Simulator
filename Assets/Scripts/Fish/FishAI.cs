using UnityEngine;

[RequireComponent(typeof(FishFightController))]
public class FishAI : MonoBehaviour
{
    public enum FishState { Roaming, InvestigatingBait, Feeding, Hooked, Fighting, Tired, Landing, Lost, Landed }

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
    [SerializeField, Min(0.1f)] private float investigateDistance = 1.4f;
    [SerializeField, Min(0.1f)] private float inspectTimeMin = 1f;
    [SerializeField, Min(0.1f)] private float inspectTimeMax = 4f;
    [SerializeField, Range(0f, 1f)] private float biteChance = 0.95f;
    [SerializeField, Min(0f)] private float baitCooldown = 8f;
    [SerializeField, Min(0.1f)] private float feedingDistance = 1.25f;
    [SerializeField, Min(0.1f)] private float takeTimeMin = 0.6f;
    [SerializeField, Min(0.1f)] private float takeTimeMax = 1.8f;

    [Header("Reaction")]
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
    public float EscapeDistance => escapeDistance;

    private void Awake()
    {
        fightController = GetComponent<FishFightController>();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
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
            case FishState.Roaming: UpdateRoaming(); break;
            case FishState.InvestigatingBait: UpdateInvestigatingBait(); break;
            case FishState.Feeding: UpdateFeeding(); break;
            case FishState.Lost: ChangeState(FishState.Roaming); break;
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
        if (Vector3.Distance(transform.position, sinker.transform.position) > detectionRadius) return;

        IgnoreCollisionWithSinker(sinker);

        baitTarget = sinker.transform;
        inspectTimer = 0f;
        ChangeState(FishState.InvestigatingBait);
    }

    private void IgnoreCollisionWithSinker(Sinker sinker)
    {
        // A carpa é kinemática enquanto nada livremente, e um corpo kinemático
        // empurra fisicamente corpos normais com quem colida - o que fazia o
        // chumbo ser empurrado em vez de "apanhado". A interação deve ser só
        // lógica (este script + o FixedJoint no momento de fisgar).
        Collider fishCollider = GetComponent<Collider>();
        Collider sinkerCollider = sinker.GetComponent<Collider>();

        if (fishCollider != null && sinkerCollider != null)
            Physics.IgnoreCollision(fishCollider, sinkerCollider, true);
    }

    private void UpdateInvestigatingBait()
    {
        if (!IsBaitValid()) { LeaveBait(); return; }

        float distance = Vector3.Distance(transform.position, baitTarget.position);
        if (distance > escapeDistance) { LeaveBait(); return; }

        if (distance > investigateDistance)
        {
            MoveToBait(baitTarget.position, GetSwimmingSpeed() * Mathf.Lerp(1f, 0.45f, Caution), investigateDistance);
            return;
        }

        FaceBait();
        inspectTimer += Time.deltaTime;
        float requiredInspection = Mathf.Lerp(inspectTimeMin, inspectTimeMax, Caution);
        if (inspectTimer < requiredInspection) return;

        float chance = Mathf.Clamp01(biteChance + (Aggression - 0.5f) * 0.2f - Caution * 0.1f + Random.Range(-randomBehaviour, randomBehaviour));
        if (Random.value <= chance)
        {
            inspectTimer = 0f;
            ChangeState(FishState.Feeding);
        }
        else LeaveBait();
    }

    private void UpdateFeeding()
    {
        if (!IsBaitValid()) { LeaveBait(); return; }

        float distance = Vector3.Distance(transform.position, baitTarget.position);
        if (distance > escapeDistance) { LeaveBait(); return; }

        if (distance > feedingDistance)
        {
            MoveToBait(baitTarget.position, GetSwimmingSpeed() * 0.15f, feedingDistance);
            return;
        }

        FaceBait();
        inspectTimer += Time.deltaTime;
        float takeTime = Mathf.Lerp(takeTimeMin, takeTimeMax, Caution);
        if (inspectTimer < takeTime) return;

        TryHookFish();
    }

    private void TryHookFish()
    {
        Sinker sinker = Sinker.Current;
        if (sinker == null || !sinker.IsInWater) { LeaveBait(); return; }
        fightController.BeginFight();

        if (fightController.IsFighting)
        {
            baitTarget = sinker.transform;
            ChangeState(FishState.Hooked);
            Debug.Log("FISGADA CONFIRMADA — carpa ligada ao rig.");
        }
        else
        {
            Debug.LogWarning("FishAI: a fisgada não foi aceite pelo sistema de luta.");
            nextBaitCheckTime = Time.time + 1f;
            ChangeState(FishState.Roaming);
        }
    }

    private bool IsBaitValid() => baitTarget != null && Sinker.Current != null && Sinker.Current.IsInWater;

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
        if (waterDepth == null) return transform.position.y;
        Vector3 sample = new Vector3(x, waterDepth.SurfaceHeight, z);
        float bottom = waterDepth.GetBottomHeightAt(sample);
        float shallowest = Mathf.Max(bottom + 0.5f, waterDepth.SurfaceHeight - maxDepth);
        float deepest = Mathf.Min(waterDepth.SurfaceHeight - minDepth, waterDepth.SurfaceHeight - 0.1f);
        if (shallowest >= deepest) return transform.position.y;
        return Random.Range(shallowest, deepest);
    }

    private void SwimTowards(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude < 0.001f) return;
        FaceDirection(direction);
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    private void MoveToBait(Vector3 destination, float speed, float stopDistance)
    {
        Vector3 offset = destination - transform.position;
        float distance = offset.magnitude;
        if (distance <= stopDistance) return;
        FaceDirection(offset);
        float step = Mathf.Min(speed * Time.deltaTime, distance - stopDistance);
        transform.position += offset.normalized * step;
    }

    private void FaceBait()
    {
        if (baitTarget == null) return;
        FaceDirection(baitTarget.position - transform.position);
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnSpeed * 3f * Time.deltaTime);
    }

    private float GetSwimmingSpeed() => speciesData != null ? speciesData.SwimmingSpeed : moveSpeed;

    private void LeaveBait()
    {
        baitTarget = null;
        inspectTimer = 0f;
        nextBaitCheckTime = Time.time + baitCooldown;
        ChooseNewDestination();
        ChangeState(FishState.Roaming);
    }

    private void ChangeState(FishState newState)
    {
        currentState = newState;
    }

    public void OnFightStarted() => ChangeState(FishState.Fighting);
    public void OnFightTired() => ChangeState(FishState.Tired);
    public void OnFishLanded() { baitTarget = null; ChangeState(FishState.Landed); }
    public void OnFishLost() { baitTarget = null; nextBaitCheckTime = Time.time + baitCooldown; ChooseNewDestination(); ChangeState(FishState.Lost); }
}