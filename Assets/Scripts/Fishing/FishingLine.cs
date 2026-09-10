using UnityEngine;

// Represents the line as one source of truth: target, available length and tension.
// It owns the joint/visual; other systems may request line recovery but must not
// move the fish or rig directly.
[RequireComponent(typeof(LineRenderer))]
public class FishingLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform lineStart;
    [SerializeField, Min(0.1f)] private float defaultLineLength = 8f;

    [Header("Line Physics")]
    [SerializeField, Min(0.1f)] private float breakingStrain = 6f;
    [SerializeField, Min(0f)] private float jointDamper = 1.5f;
    [SerializeField, Min(0f)] private float jointSpring = 30f;
    [SerializeField, Min(0f)] private float lineSlack = 0.02f;
    [SerializeField, Min(0.01f)] private float minimumLineLength = 0.25f;
    [SerializeField, Min(0.01f)] private float tensionSmoothing = 12f;

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
    public float BreakingStrain => breakingStrain;
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

        if (target == null || lineStart == null || IsBroken)
            return;

        float distance = Vector3.Distance(lineStart.position, target.position);
        float extension = Mathf.Max(0f, distance - (physicalLineLength + lineSlack));
        float radialSpeed = GetRadialTargetSpeed();
        float targetTension = extension * jointSpring + Mathf.Max(0f, radialSpeed) * jointDamper;

        tension = Mathf.MoveTowards(tension, targetTension, tensionSmoothing * Time.fixedDeltaTime);

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
        physicalLineLength = Mathf.Max(minimumLineLength, defaultLineLength);

        if (target == null || lineStart == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        Rigidbody targetBody = target.GetComponent<Rigidbody>();
        if (targetBody == null)
        {
            Debug.LogError("FishingLine: o alvo precisa de um Rigidbody.", target);
            target = null;
            lineRenderer.enabled = false;
            return;
        }

        CreatePhysicalJoint(targetBody);
        lineRenderer.enabled = true;
    }

    public void ReelIn(float distance)
    {
        if (target == null || IsBroken || distance <= 0f)
            return;

        SetLineLength(physicalLineLength - distance);
    }

    public void PayOut(float distance)
    {
        if (target == null || IsBroken || distance <= 0f)
            return;

        SetLineLength(physicalLineLength + distance);
    }

    public void Clear()
    {
        ClearPhysicalJoint();
        target = null;
        tension = 0f;
        IsBroken = false;
        lineRenderer.enabled = false;
    }

    private void SetLineLength(float newLength)
    {
        physicalLineLength = Mathf.Max(minimumLineLength, newLength);

        if (physicalJoint == null)
            return;

        SoftJointLimit limit = physicalJoint.linearLimit;
        limit.limit = physicalLineLength;
        physicalJoint.linearLimit = limit;
    }

    private float GetRadialTargetSpeed()
    {
        if (target == null)
            return 0f;

        Vector3 fromLineStart = target.position - lineStart.position;
        float distance = fromLineStart.magnitude;
        float speed = Time.fixedDeltaTime > 0f
            ? Vector3.Distance(target.position, previousTargetPosition) / Time.fixedDeltaTime
            : 0f;

        if (distance > 0.001f)
        {
            Vector3 displacement = target.position - previousTargetPosition;
            speed = Vector3.Dot(displacement / Time.fixedDeltaTime, fromLineStart / distance);
        }

        previousTargetPosition = target.position;
        return speed;
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

        SoftJointLimitSpring spring = physicalJoint.linearLimitSpring;
        spring.spring = jointSpring;
        spring.damper = jointDamper;
        physicalJoint.linearLimitSpring = spring;

        physicalJoint.angularXMotion = ConfigurableJointMotion.Free;
        physicalJoint.angularYMotion = ConfigurableJointMotion.Free;
        physicalJoint.angularZMotion = ConfigurableJointMotion.Free;
        physicalJoint.enableCollision = false;
    }

    private void ClearPhysicalJoint()
    {
        if (physicalJoint == null)
            return;

        Destroy(physicalJoint);
        physicalJoint = null;
    }

    private void BreakLine()
    {
        if (IsBroken)
            return;

        IsBroken = true;
        tension = breakingStrain;
        ClearPhysicalJoint();
        lineRenderer.enabled = false;
        Debug.Log("LINHA PARTIU!");
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
