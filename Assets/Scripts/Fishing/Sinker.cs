using UnityEngine;

// O chumbo é sempre um corpo físico normal. Assenta no fundo por gravidade e
// atrito - não há nenhum joint a prendê-lo lá. A ligação ao peixe TAMBÉM não
// usa nenhum joint do Unity (SpringJoint/FixedJoint) - um chumbo de ~90g é
// leve demais para qualquer mola do PhysX não o disparar ao mínimo esticão.
// Em vez disso, corrigimos a posição à mão, com uma velocidade máxima -
// impossível de "explodir" porque não há força nenhuma envolvida.
[RequireComponent(typeof(Rigidbody))]
public class Sinker : MonoBehaviour
{
    public static Sinker Current { get; private set; }

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(0.01f)] private float massKg = 0.09f;
    [SerializeField, Min(0f)] private float sinkAcceleration = 7f;
    [SerializeField, Min(0f)] private float waterDrag = 1.5f;

    [Header("Hook Connection")]
    [SerializeField, Min(0.01f)] private float rigLength = 0.35f;
    [SerializeField, Min(0.1f)] private float hookFollowSpeed = 2.5f; // m/s máximo de correção

    public bool IsInWater { get; private set; }
    public bool IsOnBottom { get; private set; }
    public bool IsBeingRetrieved { get; private set; }
    public Rigidbody Rigidbody => rb;
    public float MassKg => massKg;
    public float RigLength => rigLength;
    public Transform AttachedFish { get; private set; }
    public WaterDepth CurrentWater => currentWater;

    private WaterDepth currentWater;

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

        if (AttachedFish != null)
        {
            FollowAttachedFish();
            return;
        }

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

        if (IsBeingRetrieved)
            return;

        rb.AddForce(Vector3.down * sinkAcceleration, ForceMode.Acceleration);
    }

    private void FollowAttachedFish()
    {
        // Sem gravidade artificial nem joints - só uma correção de posição
        // suave, capada por hookFollowSpeed. Nunca pode "disparar".
        rb.useGravity = false;
        rb.linearDamping = waterDrag;

        Vector3 toSinker = rb.position - AttachedFish.position;
        float distance = toSinker.magnitude;

        if (distance > rigLength)
        {
            Vector3 desiredPosition = AttachedFish.position + toSinker.normalized * rigLength;
            rb.position = Vector3.MoveTowards(rb.position, desiredPosition, hookFollowSpeed * Time.fixedDeltaTime);
        }

        rb.linearVelocity = Vector3.zero;
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
            IsOnBottom = true;
    }

    private void EnterWater(WaterDepth depth)
    {
        if (depth == null || currentWater == depth)
            return;

        currentWater = depth;
        IsInWater = true;
        IsOnBottom = false;
        IsBeingRetrieved = false;
        rb.useGravity = false;
        rb.linearDamping = waterDrag;
    }

    public void BeginRetrieval()
    {
        if (AttachedFish != null || rb == null)
            return;

        IsOnBottom = false;
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

        IsOnBottom = false;
        IsBeingRetrieved = false;
        AttachedFish = fish;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void DetachFish()
    {
        AttachedFish = null;
    }

    public Transform GetAttachedFish() => AttachedFish;
}