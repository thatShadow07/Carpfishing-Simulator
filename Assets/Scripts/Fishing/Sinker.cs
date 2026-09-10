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
    [SerializeField] private float bottomStopDistance = 0.03f;

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
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.mass = massKg;
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterDrag;
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
        if (depth != null) EnterWater(depth);
    }

    private void OnCollisionEnter(Collision collision)
    {
        WaterDepth depth = collision.gameObject.GetComponent<WaterDepth>();
        if (depth == null) depth = collision.gameObject.GetComponentInParent<WaterDepth>();
        if (depth != null)
        {
            EnterWater(depth);
            return;
        }

        if (!IsInWater || currentWater == null) return;

        targetBottomY = currentWater.GetBottomHeightAt(transform.position);
        if (transform.position.y <= targetBottomY + bottomStopDistance)
            SetOnBottom();
    }

    private void EnterWater(WaterDepth depth)
    {
        if (depth == null) return;

        currentWater = depth;
        IsInWater = true;
        IsOnBottom = false;
        targetBottomY = currentWater.GetBottomHeightAt(transform.position);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Rig entrou na água.");
    }

    private void FixedUpdate()
    {
        if (!IsInWater || IsOnBottom || currentWater == null || rb == null)
            return;

        targetBottomY = currentWater.GetBottomHeightAt(transform.position);

        if (transform.position.y <= targetBottomY + bottomStopDistance)
        {
            SetOnBottom();
            return;
        }

        // Simula o peso afundando na água sem deixar o chumbo boiar.
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
