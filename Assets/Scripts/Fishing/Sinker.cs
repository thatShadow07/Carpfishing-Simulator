using UnityEngine;

public class Sinker : MonoBehaviour
{
    public static Sinker Current { get; private set; }

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(0.01f)] private float massKg = 0.09f;
    [SerializeField, Min(0f)] private float sinkAcceleration = 7f;
    [SerializeField, Min(0f)] private float waterDrag = 0.35f;

    [Header("Water Detection")]
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
    private Collider sinkerCollider;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        sinkerCollider = GetComponent<Collider>();

        if (rb != null)
        {
            rb.mass = massKg;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
        }
    }

    private void OnEnable() => Current = this;

    private void OnDisable()
    {
        if (Current == this) Current = null;
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        WaterDepth depth = other.GetComponent<WaterDepth>();
        if (depth == null) depth = other.GetComponentInParent<WaterDepth>();
        if (depth != null) EnterWater(depth, other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        WaterDepth depth = collision.gameObject.GetComponent<WaterDepth>();
        if (depth == null) depth = collision.gameObject.GetComponentInParent<WaterDepth>();

        if (depth != null)
        {
            // Water must not act as a solid surface.
            if (sinkerCollider != null && collision.collider != null)
                Physics.IgnoreCollision(sinkerCollider, collision.collider, true);

            EnterWater(depth, collision.collider);
            return;
        }

        if (!IsInWater || currentWater == null) return;

        targetBottomY = currentWater.GetBottomHeightAt(transform.position);
        if (transform.position.y <= targetBottomY + bottomStopDistance)
            SetOnBottom();
    }

    private void EnterWater(WaterDepth depth, Collider waterCollider)
    {
        if (depth == null || IsOnBottom) return;

        currentWater = depth;
        IsInWater = true;
        IsOnBottom = false;
        targetBottomY = currentWater.GetBottomHeightAt(transform.position);

        if (sinkerCollider != null && waterCollider != null)
            Physics.IgnoreCollision(sinkerCollider, waterCollider, true);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterDrag;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0f), rb.linearVelocity.z);
        }

        Debug.Log("Rig entrou na água.");
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (!IsInWater)
        {
            // Normal gravity before reaching the water.
            rb.useGravity = true;
            rb.linearDamping = 0f;
            return;
        }

        if (IsOnBottom || currentWater == null) return;

        rb.useGravity = false;
        rb.linearDamping = waterDrag;
        targetBottomY = currentWater.GetBottomHeightAt(transform.position);

        if (transform.position.y <= targetBottomY + bottomStopDistance)
        {
            SetOnBottom();
            return;
        }

        // Downward acceleration represents the lead's effective weight in water.
        rb.AddForce(Vector3.down * sinkAcceleration, ForceMode.Acceleration);
    }

    private void SetOnBottom()
    {
        IsOnBottom = true;

        Vector3 p = transform.position;
        p.y = targetBottomY;
        transform.position = p;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void AttachFish(Transform fish) => AttachedFish = fish;
    public void DetachFish() => AttachedFish = null;
    public Transform GetAttachedFish() => AttachedFish;

    public void MoveRig(Vector3 delta)
    {
        if (rb != null && !rb.isKinematic)
            rb.MovePosition(rb.position + delta);
        else
            transform.position += delta;
    }

    public void MoveWithFish(Vector3 delta) { }
}