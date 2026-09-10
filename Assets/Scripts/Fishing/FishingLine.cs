using UnityEngine;

// Physical fishing line connecting the rod tip to the sinker.
[RequireComponent(typeof(LineRenderer))]
public class FishingLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform lineStart;
    [SerializeField, Min(0.1f)] private float defaultLineLength = 8f;

    [Header("Line Physics")]
    [SerializeField, Min(0.1f)] private float breakingStrain = 6f;
    [SerializeField, Min(0f)] private float jointDamper = 1.5f;
    [SerializeField, Min(0f)] private float jointSpring = 0f;
    [SerializeField, Min(0f)] private float lineSlack = 0.02f;

    private LineRenderer lineRenderer;
    private Transform target;
    private Transform jointAnchor;
    private Rigidbody jointAnchorBody;
    private ConfigurableJoint physicalJoint;
    private Vector3 previousTargetPosition;
    private float tension;
    private float physicalLineLength;

    public float Tension => tension;
    public float TensionNormalized => Mathf.Clamp01(tension / breakingStrain);
    public bool IsBroken { get; private set; }
    public Transform Target => target;
    public float LineLength => physicalLineLength;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        CreateAnchor();
    }

    private void FixedUpdate()
    {
        if (jointAnchorBody != null && lineStart != null)
        {
            jointAnchorBody.MovePosition(lineStart.position);
            jointAnchorBody.MoveRotation(lineStart.rotation);
        }

        if (target == null || physicalJoint == null || lineStart == null)
            return;

        float distance = Vector3.Distance(lineStart.position, target.position);
        float extension = Mathf.Max(0f, distance - Mathf.Max(0.01f, physicalLineLength - lineSlack));
        tension = extension * 100f;

        if (jointSpring > 0f && extension > 0f)
            tension += extension * jointSpring;

        if (tension >= breakingStrain)
            BreakLine();
    }

    public void SetTarget(Transform newTarget)
    {
        ClearPhysicalJoint();

        target = newTarget;
        previousTargetPosition = target != null ? target.position : Vector3.zero;
        tension = 0f;
        IsBroken = false;

        if (target == null || lineStart == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        Rigidbody targetBody = target.GetComponent<Rigidbody>();
        if (targetBody == null)
        {
            Debug.LogError("FishingLine: o chumbo precisa de um Rigidbody.", target);
            lineRenderer.enabled = false;
            return;
        }

        // The line has a real, fixed amount of line instead of changing length
        // every time a new sinker is created. This lets the sinker hang naturally.
        physicalLineLength = Mathf.Max(0.1f, defaultLineLength);

        CreatePhysicalJoint(targetBody);
        lineRenderer.enabled = true;
    }

    public void Clear()
    {
        ClearPhysicalJoint();
        target = null;
        tension = 0f;
        IsBroken = false;
        lineRenderer.enabled = false;
    }

    private void CreateAnchor()
    {
        GameObject anchor = new GameObject("FishingLineAnchor");
        anchor.transform.SetParent(transform, false);
        jointAnchor = anchor.transform;

        jointAnchorBody = anchor.AddComponent<Rigidbody>();
        jointAnchorBody.isKinematic = true;
        jointAnchorBody.useGravity = false;
    }

    private void CreatePhysicalJoint(Rigidbody targetBody)
    {
        physicalJoint = targetBody.gameObject.AddComponent<ConfigurableJoint>();
        physicalJoint.connectedBody = jointAnchorBody;
        physicalJoint.autoConfigureConnectedAnchor = false;
        physicalJoint.anchor = Vector3.zero;
        physicalJoint.connectedAnchor = Vector3.zero;

        physicalJoint.xMotion = ConfigurableJointMotion.Limited;
        physicalJoint.yMotion = ConfigurableJointMotion.Limited;
        physicalJoint.zMotion = ConfigurableJointMotion.Limited;

        SoftJointLimit limit = physicalJoint.linearLimit;
        limit.limit = physicalLineLength;
        physicalJoint.linearLimit = limit;

        SoftJointLimitSpring limitSpring = physicalJoint.linearLimitSpring;
        limitSpring.spring = jointSpring;
        limitSpring.damper = jointDamper;
        physicalJoint.linearLimitSpring = limitSpring;

        physicalJoint.angularXMotion = ConfigurableJointMotion.Free;
        physicalJoint.angularYMotion = ConfigurableJointMotion.Free;
        physicalJoint.angularZMotion = ConfigurableJointMotion.Free;
        physicalJoint.enableCollision = false;
    }

    private void ClearPhysicalJoint()
    {
        if (physicalJoint != null)
        {
            Destroy(physicalJoint);
            physicalJoint = null;
        }
    }

    private void BreakLine()
    {
        if (IsBroken) return;

        IsBroken = true;
        tension = breakingStrain;
        ClearPhysicalJoint();
        lineRenderer.enabled = false;
        Debug.Log("LINHA PARTIU!");
    }

    public float CalculateTension(float externalForce = 0f)
    {
        if (IsBroken || target == null || lineStart == null) return 0f;

        float distance = Vector3.Distance(lineStart.position, target.position);
        float excessLength = Mathf.Max(0f, distance - physicalLineLength);

        float targetSpeed = Time.fixedDeltaTime > 0f
            ? Vector3.Distance(target.position, previousTargetPosition) / Time.fixedDeltaTime
            : 0f;

        tension = Mathf.Max(externalForce, excessLength * 100f + targetSpeed * 0.35f);
        tension = Mathf.Min(tension, breakingStrain);
        previousTargetPosition = target.position;
        return tension;
    }

    private void LateUpdate()
    {
        if (target == null || lineStart == null || IsBroken)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, lineStart.position);
        lineRenderer.SetPosition(1, target.position);
    }

    private void OnDestroy()
    {
        if (jointAnchor != null)
            Destroy(jointAnchor.gameObject);
    }
}