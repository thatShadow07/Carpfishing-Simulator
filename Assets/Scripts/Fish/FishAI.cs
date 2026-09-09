using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Comportamento básico da carpa. Nada em 3D dentro de uma área fixa do
/// lago, deteta o isco (Sinker), investiga-o, decide morder, e dá ao
/// jogador uma janela curta para ferrar (F) antes de fugir.
/// </summary>
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

    [Header("Ferragem (Hook Set)")]
    [SerializeField] private float reactionWindow = 2f; // segundos para premir F depois do bite

    private Vector3 targetPosition;
    private Transform baitTarget;
    private float investigateTimer;
    private float hookedTimer;

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
                // Combate/landing: próximos passos
                break;
        }
    }

    private void CheckForBait()
    {
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
            currentState = FishState.Roaming;
            ChooseNewDestination();
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
            currentState = FishState.Roaming;
            ChooseNewDestination();
            return;
        }

        investigateTimer += Time.deltaTime;

        if (investigateTimer < investigateDuration) return;

        if (Random.value <= biteChance)
        {
            Debug.Log("🔔 BITE ALARM! Prime F para ferrar!");
            hookedTimer = 0f;
            currentState = FishState.Hooked;
        }
        else
        {
            Debug.Log("A carpa desistiu do isco.");
            baitTarget = null;
            currentState = FishState.Roaming;
            ChooseNewDestination();
        }
    }

    private void HandleHooked()
    {
        hookedTimer += Time.deltaTime;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("Ferrado! A carpa está presa.");
            currentState = FishState.Fighting; // combate: próximo passo
            return;
        }

        if (hookedTimer >= reactionWindow)
        {
            Debug.Log("A carpa fugiu - não ferraste a tempo.");
            baitTarget = null;
            currentState = FishState.Roaming;
            ChooseNewDestination();
        }
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

    public FishState GetState() => currentState;

    public void SetState(FishState newState)
    {
        currentState = newState;
    }
}