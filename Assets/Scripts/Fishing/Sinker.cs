using UnityEngine;

// Placeholder do chumbo/rig lançado. Distingue água de qualquer outra
// superfície através da tag "Water". Vai evoluir para o FishingRig
// (linha, anzol, isco) quando esse sistema existir - Fase 3.
public class Sinker : MonoBehaviour
{
    private bool hasLanded;

    [SerializeField] private Rigidbody rb;

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
        WaterDepth waterDepth = waterObject.GetComponent<WaterDepth>();

        if (waterDepth != null)
        {
            float depth = waterDepth.GetDepthAt(transform.position);
            Debug.Log($"Rig entrou na água. Profundidade neste ponto: {depth:F1} m.");
        }
        else
        {
            Debug.Log("Rig entrou na água, mas este objeto ainda não tem WaterDepth.");
        }

        // Por agora, "assenta" simplesmente parando a física.
        // O afundamento até ao fundo será implementado no próximo passo.
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void LandOnGround(Collision collision)
    {
        Debug.Log($"O lançamento caiu em terra ({collision.gameObject.name}), não na água.");
    }
}
