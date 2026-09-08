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
            LandInWater();
        }
        else
        {
            LandOnGround(collision);
        }
    }

    private void LandInWater()
    {
        Debug.Log("Rig entrou na água.");

        // Por agora, "assenta" simplesmente parando a física.
        // Mais tarde isto será substituído por profundidade real do lago.
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void LandOnGround(Collision collision)
    {
        Debug.Log($"O lançamento caiu em terra ({collision.gameObject.name}), não na água.");
    }
}