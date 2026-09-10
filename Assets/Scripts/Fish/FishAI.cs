using UnityEngine;

/// <summary>
/// AI de uma carpa: patrulha, deteta o rig, aproxima-se de forma cautelosa,
/// investiga o isco e tenta alimentar-se. A luta é delegada ao FishFightController.
/// </summary>
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

    [Header("Fish Profile")]
    [SerializeField] private FishSpeciesData speciesData;

    [Header("State")]
    [SerializeField] private FishState currentState = FishState.Roaming;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 3f;
    [SerializeField, Min(0.1f)] private float destinationReachedDistance = 0.5f;

    [Header("Swim Area")]
    [SerializeField] private Vector3 lakeCenter;
    [SerializeField, Min(1f)] private float swimRadius = 20f;
    [SerializeField] private WaterDepth waterDepth;
    [SerializeField] private float surfaceMargin = 0.5f;
    [SerializeField] private float bottomMargin = 0.5f;

    [Header("Bait Behaviour")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 4f;
    [SerializeField, Min(0.1f)] private float investigateDistance = 0.6f;
    [SerializeField, Min(0.1f)] private float inspectDuration = 2.5f;
    [SerializeField, Range(0f, 1f)] private float baseBiteChance = 0.7f;
    [SerializeField, Range(0f, 1f)] private float baseHookChance = 0.85f;
    [SerializeField, Min(0f)] private float baitCooldown = 8f;

    [Header("Behaviour Variation")]
    [SerializeField, Min(0.1f)] private float destinationChangeMin = 3f;
    [SerializeField, Min(0.1f)] private float destinationChangeMax = 8f;
    [SerializeField, Range(0f, 1f)] private float randomInterestVariation = 0.15f;

    private FishFightController fightController;
    private Transform baitTarget;
    private Vector3 targetPosition;
    private float inspectTimer;
    private float nextBaitCheckTime;
    private float destinationTimer;

    public FishState CurrentState => currentState;
    public FishSpeciesData SpeciesData => speciesData;
    public float Caution => speciesData != null ? speciesData.Caution : 0.5f;
    public float Aggression => speciesData != null ? speciesData.Aggression : 0.5f;
    public float Intelligence => speciesData != null ? speciesData.Intelligence : 0.5f;

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
        if (fightController != null && fightController.IsFighting)
            return;

        switch (currentState)
        {
            case FishState.Roaming:
                UpdateRoaming();
                break;

            case FishState.InvestigatingBait:
                UpdateInvestigation();
                break;

            case FishState.Feeding:
                UpdateFeeding();
                break;
        }
    }

    private void UpdateRoaming()
    {
        SwimTowards(targetPosition, GetMovementSpeed());
        destinationTimer -= Time.deltaTime;

        CheckForBait();

        if (destinationTimer <= 0f ||
            Vector3.Distance(transform.position, targetPosition) <= destinationReachedDistance)
        {
            ChooseNewDestination();
        }
    }

    private void CheckForBait()
    {
        if (Time.time < nextBaitCheckTime)
            return;

        if (Sinker.Current == null || !Sinker.Current.IsInWater)
            return;

        if (fightController != null && fightController.IsFighting)
            return;

        float distance = Vector3.Distance(transform.position, Sinker.Current.transform.position);
        if (distance > detectionRadius)
            return;

        baitTarget = Sinker.Current.transform;
        inspectTimer = 0f;
        ChangeState(FishState.InvestigatingBait);
    }

    private void UpdateInvestigation()
    {
        if (baitTarget == null)
        {
            LeaveBait();
            return;
        }

        float distance = Vector3.Distance(transform.position, baitTarget.position);

        if (distance > investigateDistance)
        {
            float cautiousSpeed = Mathf.Lerp(GetMovementSpeed(), GetMovementSpeed() * 0.35f, Caution);
            SwimTowards(baitTarget.position, cautiousSpeed);
            return;
        }

        inspectTimer += Time.deltaTime;

        // Um peixe cauteloso demora mais a decidir.
        float requiredInspection = Mathf.Lerp(
            inspectDuration * 0.6f,
            inspectDuration * 1.8f,
            Caution
        );

        if (inspectTimer < requiredInspection)
            return;

        float biteChance = Mathf.Clamp01(
            baseBiteChance
            + (Aggression - 0.5f) * 0.2f
            - Caution * 0.2f
            + Random.Range(-randomInterestVariation, randomInterestVariation)
        );

        if (Random.value <= biteChance)
        {
            ChangeState(FishState.Feeding);
        }
        else
        {
            LeaveBait();
        }
    }

    private void UpdateFeeding()
    {
        if (baitTarget == null || Sinker.Current == null || !Sinker.Current.IsInWater)
        {
            LeaveBait();
            return;
        }

        float distance = Vector3.Distance(transform.position, baitTarget.position);
        if (distance > investigateDistance * 1.5f)
        {
            LeaveBait();
            return;
        }

        // Pequeno atraso irregular para evitar que todas as carpas comam no mesmo instante.
        inspectTimer += Time.deltaTime;

        float takeDelay = Mathf.Lerp(0.4f, 1.8f, Caution);
        if (inspectTimer < takeDelay)
            return;

        float hookChance = Mathf.Clamp01(
            baseHookChance
            + Aggression * 0.12f
            - Caution * 0.18f
        );

        if (Random.value <= hookChance)
        {
            baitTarget = Sinker.Current.transform;
            ChangeState(FishState.Hooked);

            if (fightController != null)
                fightController.BeginFight();
        }
        else
        {
            LeaveBait();
        }
    }

    private void ChooseNewDestination()
    {
        Vector2 randomCircle = Random.insideUnitCircle * swimRadius;

        float x = lakeCenter.x + randomCircle.x;
        float z = lakeCenter.z + randomCircle.y;
        float y = ChooseDepthAt(x, z);

        targetPosition = new Vector3(x, y, z);
        destinationTimer = Random.Range(
            destinationChangeMin,
            Mathf.Max(destinationChangeMin, destinationChangeMax)
        );
    }

    private float ChooseDepthAt(float x, float z)
    {
        if (waterDepth == null)
            return transform.position.y;

        Vector3 point = new Vector3(x, waterDepth.SurfaceHeight, z);
        float surface = waterDepth.SurfaceHeight - surfaceMargin;
        float bottom = waterDepth.GetBottomHeightAt(point) + bottomMargin;

        if (bottom >= surface)
            return transform.position.y;

        // Carpas não passam constantemente rente à superfície nem ao fundo.
        return Random.Range(bottom, surface);
    }

    private void SwimTowards(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            turnSpeed * Time.deltaTime
        );

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private float GetMovementSpeed()
    {
        return speciesData != null
            ? speciesData.SwimmingSpeed
            : moveSpeed;
    }

    private void LeaveBait()
    {
        baitTarget = null;
        inspectTimer = 0f;
        nextBaitCheckTime = Time.time + baitCooldown;
        ChooseNewDestination();
        ChangeState(FishState.Roaming);
    }

    public void OnFightStarted()
    {
        ChangeState(FishState.Fighting);
    }

    public void OnFightTired()
    {
        ChangeState(FishState.Tired);
    }

    public void OnFishLanded()
    {
        ChangeState(FishState.Landed);
    }

    public void OnFishLost()
    {
        baitTarget = null;
        ChooseNewDestination();
        ChangeState(FishState.Lost);
        nextBaitCheckTime = Time.time + baitCooldown;
        ChangeState(FishState.Roaming);
    }

    private void ChangeState(FishState newState)
    {
        currentState = newState;
    }
}
