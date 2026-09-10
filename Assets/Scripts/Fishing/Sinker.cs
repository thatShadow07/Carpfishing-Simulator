using UnityEngine;

public class Sinker : MonoBehaviour
{
    public static Sinker Current { get; private set; }

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(0.01f)] private float massKg = 0.09f;
    [SerializeField, Min(0f)] private float sinkAcceleration = 7f;
    [SerializeField, Min(0f)] private float waterDrag = 1.5f;

    [Header("Water Detection")]
    [SerializeField, Min(0f)] private float waterEntryTolerance = 0.05f;
    [SerializeField, Min(0f)] private float bottomStopDistance = 0.03f;

    [Header("Rig")]
    [SerializeField, Min(0.01f)] private float rigLength = 0.35f;

    public bool IsInWater { get; private set; }
    public bool IsOnBottom { get; private set; }
    public Rigidbody Rigidbody => rb;
    public float MassKg => massKg;
    public float RigLength => rigLength;
    public Transform AttachedFish { get; private set; }
    public WaterDepth CurrentWater => currentWater;

    private WaterDepth currentWater;
    private float targetBottomY;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

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

        // Do not depend on water collision events. Detect the water from its
        // actual surface height so the sinker cannot simply pass through it.
        if (!IsInWater)
        {
            WaterDepth detectedWater = FindWaterAtPosition();
            if (detectedWater != null && transform.position.y <= detectedWater.SurfaceHeight + waterEntryTolerance)
                EnterWater(detectedWater);
        }

        if (!IsInWater || currentWater == null || IsOnBottom)
        {
            rb.useGravity = true;
            return;
        }

        rb.useGravity = false;
        rb.linearDamping = waterDrag;

        targetBottomY = currentWater.GetBottomHeightAt(transform.position);

        // The bottom is the only thing that stops the sinker.
        if (transform.position.y <= targetBottomY + bottomStopDistance)
        {
            SetOnBottom();
            return;
        }

        // Effective downward weight while submerged.
        rb.AddForce(Vector3.down * sinkAcceleration, ForceMode.Acceleration);
    }

    private WaterDepth FindWaterAtPosition()
    {
        WaterDepth[] waters = FindObjectsByType<WaterDepth>(FindObjectsSortMode.None);
        WaterDepth closest = null;
        float closestHorizontalSqr = float.PositiveInfinity;

        foreach (WaterDepth water in waters)
        {
            if (water == null) continue;

            float dx = transform.position.x - water.transform.position.x;
            float dz = transform.position.z - water.transform.position.z;
            float sqr = dx * dx + dz * dz;

            if (sqr < closestHorizontalSqr)
            {
                closestHorizontalSqr = sqr;
                closest = water;
            }
        }

        return closest;
    }

    private void OnTriggerEnter(Collider other)
    {
        WaterDepth depth = other.GetComponent<WaterDepth>();
        if (depth == null)
            depth = other.GetComponentInParent<WaterDepth>();

        if (depth != null)
            EnterWater(depth);
    }

    private void OnCollisionEnter(Collision collision)
    {
        WaterDepth depth = collision.gameObject.GetComponent<WaterDepth>();
        if (depth == null)
            depth = collision.gameObject.GetComponentInParent<WaterDepth>();

        if (depth != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), collision.collider, true);
            EnterWater(depth);
            return;
        }

        if (!IsInWater || currentWater == null)
            return;

        targetBottomY = currentWater.GetBottomHeightAt(transform.position);
        if (transform.position.y <= targetBottomY + bottomStopDistance)
            SetOnBottom();
    }

    private void EnterWater(WaterDepth depth)
    {
        if (depth == null)
            return;

        currentWater = depth;
        IsInWater = true;
        IsOnBottom = false;
        targetBottomY = currentWater.GetBottomHeightAt(transform.position);

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearDamping = waterDrag;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0f), rb.linearVelocity.z);
    }

    private void SetOnBottom()
    {
        IsOnBottom = true;

        Vector3 p = transform.position;
        p.y = targetBottomY;
        transform.position = p;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    public void AttachFish(Transform fish) => AttachedFish = fish;
    public void DetachFish() => AttachedFish = null;
    public Transform GetAttachedFish() => AttachedFish;

    public void MoveRig(Vector3 delta)
    {
        if (rb != null && !rb.isKinematic)
            rb.MovePosition(rb.position + delta);
        else if (rb != null)
            rb.position += delta;
        else
            transform.position += delta;
    }

    public void MoveWithFish(Vector3 delta) { }
}