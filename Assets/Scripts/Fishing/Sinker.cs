using UnityEngine;

// Phase 1 physical rig root. The lead Rigidbody remains under physics at every
// stage; the lake bed and a hooked fish are represented by breakable joints.
[RequireComponent(typeof(Rigidbody))]
public class Sinker : MonoBehaviour
{
    public static Sinker Current { get; private set; }

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(0.01f)] private float massKg = 0.09f;
    [SerializeField, Min(0f)] private float sinkAcceleration = 7f;
    [SerializeField, Min(0f)] private float waterDrag = 1.5f;

    [Header("Lake Bed")]
    [SerializeField, Min(0.1f)] private float bottomHoldingForce = 25f;
    [SerializeField, Min(0.1f)] private float bottomHoldingTorque = 25f;

    [Header("Hook Connection")]
    [SerializeField, Min(0.1f)] private float hookConnectionStrength = 12f;

    [Header("Rig")]
    [SerializeField, Min(0.01f)] private float rigLength = 0.35f;

    public bool IsInWater { get; private set; }
    public bool IsOnBottom => bottomHoldJoint != null;
    public bool IsBeingRetrieved { get; private set; }
    public Rigidbody Rigidbody => rb;
    public float MassKg => massKg;
    public float RigLength => rigLength;
    public Transform AttachedFish { get; private set; }
    public WaterDepth CurrentWater => currentWater;

    private WaterDepth currentWater;
    private FixedJoint bottomHoldJoint;
    private FixedJoint fishConnectionJoint;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.mass = massKg;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
    }

    private void OnEnable() => Current = this;

    private void OnDisable()
    {
        if (Current == this)
            Current = null;
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (!IsInWater)
        {
            WaterDepth detectedWater = FindWaterAtPosition();
            if (detectedWater != null && transform.position.y <= detectedWater.SurfaceHeight)
                EnterWater(detectedWater);
        }

        if (!IsInWater || currentWater == null)
            return;

        rb.useGravity = false;
        rb.linearDamping = waterDrag;

        // A bottom joint holds the lead against the lake bed. Reeling or a
        // hookup destroys that joint; neither path writes to the Transform.
        if (IsOnBottom || IsBeingRetrieved || AttachedFish != null)
            return;

        rb.AddForce(Vector3.down * sinkAcceleration, ForceMode.Acceleration);
    }

    private WaterDepth FindWaterAtPosition()
    {
        WaterDepth[] waters = FindObjectsByType<WaterDepth>();
        WaterDepth closest = null;
        float closestHorizontalSqr = float.PositiveInfinity;

        foreach (WaterDepth water in waters)
        {
            if (water == null)
                continue;

            float dx = transform.position.x - water.transform.position.x;
            float dz = transform.position.z - water.transform.position.z;
            float horizontalSqr = dx * dx + dz * dz;

            if (horizontalSqr < closestHorizontalSqr)
            {
                closestHorizontalSqr = horizontalSqr;
                closest = water;
            }
        }

        return closest;
    }

    private void OnTriggerEnter(Collider other)
    {
        WaterDepth depth = other.GetComponentInParent<WaterDepth>();
        if (depth != null)
            EnterWater(depth);
    }

    private void OnCollisionEnter(Collision collision)
    {
        WaterDepth depth = collision.gameObject.GetComponentInParent<WaterDepth>();
        if (depth != null)
        {
            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null)
                Physics.IgnoreCollision(ownCollider, collision.collider, true);

            EnterWater(depth);
            return;
        }

        if (!IsInWater || currentWater == null || IsBeingRetrieved || AttachedFish != null)
            return;

        if (collision.contactCount > 0 && collision.GetContact(0).normal.y > 0.2f)
            HoldOnLakeBed();
    }

    private void EnterWater(WaterDepth depth)
    {
        if (depth == null || currentWater == depth)
            return;

        currentWater = depth;
        IsInWater = true;
        IsBeingRetrieved = false;
        rb.useGravity = false;
        rb.linearDamping = waterDrag;
    }

    private void HoldOnLakeBed()
    {
        if (IsOnBottom)
            return;

        bottomHoldJoint = gameObject.AddComponent<FixedJoint>();
        bottomHoldJoint.connectedBody = null;
        bottomHoldJoint.breakForce = bottomHoldingForce;
        bottomHoldJoint.breakTorque = bottomHoldingTorque;
        bottomHoldJoint.enableCollision = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void BeginRetrieval()
    {
        if (AttachedFish != null || rb == null)
            return;

        ReleaseLakeBedHold();
        IsBeingRetrieved = true;
    }

    public void EndRetrieval()
    {
        IsBeingRetrieved = false;
    }

    public void AttachFish(Transform fish)
    {
        if (fish == null || AttachedFish != null)
            return;

        Rigidbody fishBody = fish.GetComponent<Rigidbody>();
        if (fishBody == null)
        {
            Debug.LogWarning("Sinker: o peixe fisgado precisa de Rigidbody.", fish);
            return;
        }

        ReleaseLakeBedHold();
        IsBeingRetrieved = false;
        AttachedFish = fish;

        fishConnectionJoint = gameObject.AddComponent<FixedJoint>();
        fishConnectionJoint.connectedBody = fishBody;
        fishConnectionJoint.breakForce = hookConnectionStrength;
        fishConnectionJoint.breakTorque = hookConnectionStrength;
        fishConnectionJoint.enableCollision = false;
    }

    public void DetachFish()
    {
        if (fishConnectionJoint != null)
            Destroy(fishConnectionJoint);

        fishConnectionJoint = null;
        AttachedFish = null;
    }

    private void ReleaseLakeBedHold()
    {
        if (bottomHoldJoint != null)
            Destroy(bottomHoldJoint);

        bottomHoldJoint = null;
    }

    public Transform GetAttachedFish() => AttachedFish;
}