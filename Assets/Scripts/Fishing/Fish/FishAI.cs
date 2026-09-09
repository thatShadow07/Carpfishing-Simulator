using UnityEngine;

/// <summary>
/// Basic carp behaviour for the first playable prototype.
/// The fish moves between simple roaming points and can later be extended
/// with feeding, bait investigation and hooked/fighting states.
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
    [SerializeField] private float swimRadius = 8f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = -1f;
    [SerializeField] private float destinationReachedDistance = 0.5f;

    private Vector3 targetPosition;

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
                break;

            case FishState.Feeding:
            case FishState.InvestigatingBait:
            case FishState.TakingBait:
            case FishState.Hooked:
            case FishState.Fighting:
            case FishState.Landing:
                // These states will receive their own behaviour in later steps.
                break;
        }
    }

    private void SwimTowardsTarget()
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= destinationReachedDistance * destinationReachedDistance)
        {
            ChooseNewDestination();
            return;
        }

        Vector3 desiredDirection = direction.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private void ChooseNewDestination()
    {
        Vector3 center = transform.position;
        Vector2 randomCircle = Random.insideUnitCircle * swimRadius;

        targetPosition = new Vector3(
            center.x + randomCircle.x,
            Random.Range(minY, maxY),
            center.z + randomCircle.y);
    }

    public FishState GetState() => currentState;

    public void SetState(FishState newState)
    {
        currentState = newState;
    }
}
