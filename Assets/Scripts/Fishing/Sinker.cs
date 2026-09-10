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
    [SerializeField, Min(0f)] private float surfaceDetectionMargin = 0.15f;
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
    private Collider currentWaterCollider;
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

    private void OnTriggerStay(Collider other)
    {
        WaterDepth depth = other.GetComponent<WaterDepth>();
        if (depth == null) depth = other.GetComponentInParent<WaterDepth>();
        if (depth != null && !IsInWater) EnterWater(depth, other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        WaterDepth depth = collision.gameObject.GetComponent<WaterDepth>();
        if (depth == null) depth = collision.gameObject.GetComponentInParent<WaterDepth>();

        if (depth != null)
        {
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
        currentWaterCollider = waterCollider != null ? waterCollider : depth.GetComponent<Collider>();
        if (currentWaterCollider == null)
            currentWaterCollider = depth.GetComponentInParent<Collider>();

        IsInWater = true;
        IsOnBottom = false;
        targetBottomY = currentWater.GetBottomHeightAt(transform.position);

        if (sinkerCollider != null && currentWaterCollider != null)
            Physics.IgnoreCollision(sinkerCollider, currentWaterCollider, true);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterDrag;
        }

        Debug.Log("Rig entrou na água.");
    }

    private void TryDetectWater()
    {
        if (IsInWater || IsOnBottom) return;

        WaterDepth depth = FindFirstObjectByType<WaterDepth>();
        if (depth == null) return;

        float surface = depth.SurfaceHeight;
        if (transform.position.y > surface + surfaceDetectionMargin) return;

        Collider waterCollider = depth.GetComponent<Collider>();
        if (waterCollider == null)
            waterCollider = depth.GetComponentInChildren<Collider>();

        if (waterCollider != null)
        {
            Bounds bounds = waterCollider.bounds;
            Vector3 closest = bounds.ClosestPoint(transform.position);
            bool insideHorizontalArea = Mathf.Abs(closest.x - transform.position.x) < 0.01f ||
                                        Mathf.Abs(closest.z - transform.position.z) < 0.01f;
            if (!insideHorizontalArea) return;
        }

        EnterWater(depth, waterCollider);
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        TryDetectWater();

        if (!IsInWater)
        {
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