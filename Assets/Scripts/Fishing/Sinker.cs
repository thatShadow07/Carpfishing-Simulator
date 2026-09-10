using UnityEngine;

public class Sinker : MonoBehaviour
{
    public static Sinker Current { get; private set; }

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(0.01f)] private float massKg = 0.09f;
    [SerializeField, Min(0f)] private float sinkSpeed = 1.5f;

    [Header("Rig")]
    [SerializeField, Min(0.01f)] private float rigLength = 0.35f;

    public bool IsInWater { get; private set; }
    public bool IsOnBottom { get; private set; }
    public Rigidbody Rigidbody => rb;
    public float MassKg => massKg;
    public float RigLength => rigLength;
    public Transform AttachedFish { get; private set; }

    private WaterDepth currentWater;
    private float targetBottomY;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = massKg;
            rb.useGravity = true;
            rb.isKinematic = false;
        }
    }

    private void OnEnable() => Current = this;

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water"))
        {
            EnterWater(collision.gameObject);
            return;
        }

        if (!IsInWater) return;
        WaterDepth depth = currentWater != null ? currentWater : collision.gameObject.GetComponent<WaterDepth>();
        if (depth != null)
        {
            targetBottomY = depth.GetBottomHeightAt(transform.position);
            if (transform.position.y <= targetBottomY + 0.15f) SetOnBottom();
        }
    }

    private void EnterWater(GameObject waterObject)
    {
        IsInWater = true;
        currentWater = waterObject.GetComponent<WaterDepth>();
        if (currentWater != null) targetBottomY = currentWater.GetBottomHeightAt(transform.position);
        IsOnBottom = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log("Rig entrou na água.");
    }

    private void FixedUpdate()
    {
        if (!IsInWater || IsOnBottom || currentWater == null) return;
        targetBottomY = currentWater.GetBottomHeightAt(transform.position);
        if (transform.position.y <= targetBottomY + 0.05f) SetOnBottom();
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
    public void MoveRig(Vector3 delta) => transform.position += delta;
    public void MoveWithFish(Vector3 delta) { }
}
