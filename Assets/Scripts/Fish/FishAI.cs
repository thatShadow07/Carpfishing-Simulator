using UnityEngine;

/// <summary>
/// Comportamento básico da carpa. Nada em 3D dentro de uma área fixa do
/// lago, deteta o isco, investiga-o e pode alimentar-se dele.
/// A ferragem é determinada pela interação peixe + montagem, não pelo F.
/// O F representa a reação do pescador ao alarme.
/// </summary>
[RequireComponent(typeof(FishFightController))]
public class FishAI : MonoBehaviour
{
    public enum FishState
    {
        Roaming,
        Exploring,
        Feeding,
        InvestigatingBait,
        TakingBait,
        Hooked,
        Fighting,
        Landing
    }

    [Header("State")]
    [SerializeField] private FishState currentState = FishState.Roaming;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 3f;
    [SerializeField] private float destinationReachedDistance = 0.5f;

    [Header("Swim Area")]
    [SerializeField] private Vector3 lakeCenter;
    [SerializeField] private float swimRadius = 20f;
    [SerializeField] private WaterDepth waterDepth;
    [SerializeField] private float surfaceMargin = 0.5f;
    [SerializeField] private float bottomMargin = 0.5f;

    [Header("Bait Detection")]
    [SerializeField] private float detectionRadius = 4f;
    [SerializeField] private float biteDistance = 0.5f;
    [SerializeField] private float investigateDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float biteChance = 0.7f;
    [SerializeField] private float baitCooldown = 6f;

    [Header("Hooking")]
    [SerializeField, Range(0f, 1f)] private float hookChance = 0.85f;
    [SerializeField] private float hookDelay = 0.75f;
    [SerializeField] private float alarmReactionWindow = 2f;

    private Vector3 targetPosition;
    private Transform baitTarget;
    private float investigateTimer;
    private float hookTimer;
    private float nextBaitCheckTime;
    private bool hookSet;
    private FishFightController fightController;

    private void Awake()
    {
        fightController = GetComponent<FishFightController>();
    }

    private void Start()
    {
        ChooseNewDestination();
    }

    private void Update()
    {
        switch (currentState)
        {
            case FishState.Roaming:
            case FishState.Exploring:
                SwimTowardsTarget();
                CheckForBait();
                break;

            case FishState.InvestigatingBait:
                SwimTowardsBait();
                break;

            case FishState.TakingBait:
                HandleTakingBait();
                break;

            case FishState.Hooked:
                HandleHooked();
                break;

            case FishState.Feeding:
            case FishState.Fighting:
            case FishState.Landing:
                break;
        }
    }

    private void CheckForBait()
    {
        if (Time.time < nextBaitCheckTime) return;
        if (Sinker.Current == null || !Sinker.Current.IsInWater) return;

        float distance = Vector3.Distance(transform.position, Sinker.Current.transform.position);

        if (distance <= detectionRadius)
        {
            baitTarget = Sinker.Current.transform;
            currentState = FishState.InvestigatingBait;
        }
    }

    private void SwimTowardsBait()
    {
        if (baitTarget == null)
        {
            ReturnToRoaming();
            return;
        }

        Vector3 direction = baitTarget.position - transform.position;

        if (direction.magnitude <= biteDistance)
        {
            currentState = FishState.TakingBait;
            investigateTimer = 0f;
            return;
        }

        MoveAndTurn(direction);
    }

    private void HandleTakingBait()
    {
        if (baitTarget == null)
        {
            ReturnToRoaming();
            return;
        }

        investigateTimer += Time.deltaTime;

        if (investigateTimer < investigateDuration) return;

        if (Random.value > biteChance)
        {
            Debug.Log("A carpa investigou o isco, mas não o comeu.");
            nextBaitCheckTime = Time.time + baitCooldown;
            ReturnToRoaming();
            return;
        }

        hookSet = Random.value <= hookChance;
        hookTimer = 0f;

        if (hookSet)
        {
            Debug.Log("🐟 A carpa comeu o isco — o anzol começou a cravar.");
            currentState = FishState.Hooked;
        }
        else
        {
            Debug.Log("🐟 A carpa comeu o isco, mas o anzol não ficou cravado.");
            nextBaitCheckTime = Time.time + baitCooldown;
            ReturnToRoaming();
        }
    }

    private void HandleHooked()
    {
        hookTimer += Time.deltaTime;

        if (hookTimer >= hookDelay && hookTimer < hookDelay + Time.deltaTime)
        {
            Debug.Log("🔔 BITE ALARM! A carpa está presa — reage e pega na cana!");
        }

        if (hookTimer >= alarmReactionWindow)
        {
            BeginFighting("🐟 A carpa continua a puxar — combate começa mesmo sem reação imediata.");
            return;
        }

        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame &&
            hookTimer >= hookDelay)
        {
            BeginFighting("🎣 Pegaste na cana! A carpa está presa e começa o combate.");
        }
    }

    private void BeginFighting(string message)
    {
        Debug.Log(message);
        currentState = FishState.Fighting;
        fightController.BeginFight();
    }

    private void SwimTowardsTarget()
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude <= destinationReachedDistance * destinationReachedDistance)
        {
            ChooseNewDestination();
            return;
        }

        MoveAndTurn(direction);
    }

    private void MoveAndTurn(Vector3 direction)
    {
        Vector3 desiredDirection = direction.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private void ChooseNewDestination()
    {
        Vector2 randomCircle = Random.insideUnitCircle * swimRadius;
        float x = lakeCenter.x + randomCircle.x;
        float z = lakeCenter.z + randomCircle.y;
        float y = ChooseDepthAt(x, z);

        targetPosition = new Vector3(x, y, z);
    }

    private float ChooseDepthAt(float x, float z)
    {
        if (waterDepth == null)
        {
            return transform.position.y;
        }

        Vector3 point = new Vector3(x, waterDepth.SurfaceHeight, z);
        float surface = waterDepth.SurfaceHeight - surfaceMargin;
        float bottom = waterDepth.GetBottomHeightAt(point) + bottomMargin;

        if (bottom > surface)
        {
            return (surface + bottom) * 0.5f;
        }

        return Random.Range(bottom, surface);
    }

    private void ReturnToRoaming()
    {
        baitTarget = null;
        hookSet = false;
        currentState = FishState.Roaming;
        ChooseNewDestination();
    }

    public FishState GetState() => currentState;

    public void SetState(FishState newState)
    {
        currentState = newState;
    }
}
