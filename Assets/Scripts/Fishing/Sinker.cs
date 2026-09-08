using UnityEngine;

// Placeholder do chumbo/rig lançado. Ao tocar na água, para de cair por
// física normal e desce lentamente até ao fundo real do lago (via WaterDepth).
public class Sinker : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float sinkSpeed = 1.5f; // metros por segundo

    private bool hasLanded;
    private bool isSinking;
    private float targetBottomY;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;
        hasLanded = true;

        if (collision.gameObject.CompareTag("Water"))
        {
            LandInWater(collision.gameObject);
        }
        else
        {
            LandOnGround(collision);
        }
    }

    private void LandInWater(GameObject waterObject)
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        WaterDepth waterDepth = waterObject.GetComponent<WaterDepth>();

        if (waterDepth != null)
        {
            float depth = waterDepth.GetDepthAt(transform.position);
            targetBottomY = waterDepth.GetBottomHeightAt(transform.position);
            isSinking = true;

            Debug.Log($"Rig entrou na água. Profundidade neste ponto: {depth:F1} m.");
        }
        else
        {
            Debug.Log("Rig entrou na água, mas este objeto ainda não tem WaterDepth.");
        }
    }

    private void Update()
    {
        if (!isSinking) return;

        Vector3 position = transform.position;

        if (position.y > targetBottomY)
        {
            position.y = Mathf.MoveTowards(position.y, targetBottomY, sinkSpeed * Time.deltaTime);
            transform.position = position;
        }
        else
        {
            isSinking = false; // chegou ao fundo
        }
    }

    private void LandOnGround(Collision collision)
    {
        Debug.Log($"O lançamento caiu em terra ({collision.gameObject.name}), não na água.");
    }
}