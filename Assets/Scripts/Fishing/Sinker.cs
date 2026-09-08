using UnityEngine;

// Placeholder do chumbo/rig lançado. Ainda não distingue água de chão -
// isso só faz sentido quando houver lago. Vai evoluir para o FishingRig
// planeado no ARCHITECTURE.md quando tivermos água, anzol e isco a sério.
public class Sinker : MonoBehaviour
{
    private bool hasLanded;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        hasLanded = true;
        Debug.Log($"Chumbo pousou em: {collision.gameObject.name}");
    }
}