using UnityEngine;

// Placeholder do chumbo/rig lançado. Distingue água de qualquer outra
// superfície através da tag "Water". Vai evoluir para o FishingRig
// (linha, anzol, isco) quando esse sistema existir - Fase 3.
public class Sinker : MonoBehaviour
{
    private bool hasLanded;
    private bool isSinking;
    private float waterBottomHeight;
    private float sinkingSpeed;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private float sinkSpeed = 2f;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (!isSinking) return;

        Vector3 position = transform.position;
        position.y = Mathf.MoveTowards(position.y, waterBottomHeight, sinkingSpeed * Time.deltaTime);
        transform.position = position;

        if (Mathf.Approximately(position.y, waterBottomHeight))
        {
            isSinking = false;
            hasLanded = true;
            Debug.Log($"Rig assentou no fundo. Profundidade: {GetSettledDepth():F1} m.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        if (collision.gameObject.CompareTag("Water"))
        {
            LandInWater(collision.gameObject);
        }
        else
        {
            hasLanded = true;
            LandOnGround(collision);
        }
    }

    private void LandInWater(GameObject waterObject)
    {
        WaterDepth waterDepth = waterObject.GetComponent<WaterDepth>();

        if (waterDepth == null)
        {
            Debug.Log("Rig entrou na água, mas este objeto ainda não tem WaterDepth.");
            return;
        }

        waterBottomHeight = waterDepth.BottomHeight;
        sinkingSpeed = Mathf.Max(0.01f, sinkSpeed);

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        isSinking = true;

        Debug.Log($"Rig entrou na água. A afundar até {waterDepth.GetDepthAt(transform.position):F1} m de profundidade.");
    }

    private float GetSettledDepth()
    {
        return Mathf.Max(0f, -waterBottomHeight);
    }

    private void LandOnGround(Collision collision)
    {
        Debug.Log($"O lançamento caiu em terra ({collision.gameObject.name}), não na água.");
    }
}
