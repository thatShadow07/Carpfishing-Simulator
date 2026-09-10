using UnityEngine;

// Represents the fishing line between the rod tip and the rig/fish.
// It renders the line and calculates tension from distance and relative motion.
[RequireComponent(typeof(LineRenderer))]
public class FishingLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform lineStart;
    [SerializeField] private float slackLength = 0.5f;

    [Header("Line Physics")]
    [SerializeField, Min(0.1f)] private float breakingStrain = 6f;
    [SerializeField, Min(0.01f)] private float stretchResponse = 8f;
    [SerializeField, Min(0.01f)] private float movementTension = 0.35f;

    private LineRenderer lineRenderer;
    private Transform target;
    private Vector3 previousTargetPosition;
    private float tension;

    public float Tension => tension;
    public float TensionNormalized => Mathf.Clamp01(tension / breakingStrain);
    public bool IsBroken { get; private set; }
    public Transform Target => target;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        previousTargetPosition = target != null ? target.position : Vector3.zero;
        IsBroken = false;
        tension = 0f;
        lineRenderer.enabled = target != null;
    }

    public void Clear()
    {
        target = null;
        tension = 0f;
        IsBroken = false;
        lineRenderer.enabled = false;
    }

    public float CalculateTension(float externalForce = 0f)
    {
        if (IsBroken || target == null || lineStart == null) return 0f;

        float distance = Vector3.Distance(lineStart.position, target.position);
        float excessLength = Mathf.Max(0f, distance - slackLength);
        float distanceTension = excessLength * stretchResponse;

        float targetSpeed = Time.deltaTime > 0f
            ? Vector3.Distance(target.position, previousTargetPosition) / Time.deltaTime
            : 0f;

        float movementComponent = targetSpeed * movementTension;
        float desired = Mathf.Max(externalForce, distanceTension + movementComponent);
        tension = Mathf.MoveTowards(tension, desired, stretchResponse * Time.deltaTime);
        previousTargetPosition = target.position;
        return tension;
    }

    public bool ApplyForceToFish(Rigidbody fishBody, Vector3 fishPosition, float externalForce = 0f)
    {
        if (IsBroken || target == null || lineStart == null || fishBody == null) return false;

        float currentTension = CalculateTension(externalForce);
        Vector3 direction = (lineStart.position - fishPosition).normalized;
        if (currentTension > 0f)
            fishBody.AddForce(direction * currentTension, ForceMode.Force);

        if (currentTension >= breakingStrain)
        {
            IsBroken = true;
            tension = breakingStrain;
            Debug.Log("LINHA PARTIU!");
            return true;
        }

        return false;
    }

    private void LateUpdate()
    {
        if (target == null || lineStart == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = !IsBroken;
        if (IsBroken) return;

        lineRenderer.SetPosition(0, lineStart.position);
        lineRenderer.SetPosition(1, target.position);
    }
}
