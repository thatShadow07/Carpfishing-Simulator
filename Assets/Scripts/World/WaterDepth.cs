using UnityEngine;

/// <summary>
/// Define a superfície da água e vai buscar a altura real do fundo do lago
/// abaixo de qualquer ponto, para que zonas diferentes tenham profundidades
/// diferentes. O Plane da água continua a ser só a superfície - o fundo é
/// uma geometria separada (na layer "LakeBed") contra a qual fazemos raycast.
/// </summary>
public class WaterDepth : MonoBehaviour
{
    [Header("Water Level")]
    [SerializeField] private float surfaceHeight = 0f;

    [Header("Lake Bottom")]
    [SerializeField] private LayerMask lakeBedLayer;
    [SerializeField] private float fallbackBottomHeight = -5f; // usado se o raycast não encontrar fundo nenhum

    public float SurfaceHeight => surfaceHeight;

    /// <summary>
    /// Altura (Y) do fundo do lago diretamente abaixo de worldPosition.
    /// </summary>
    public float GetBottomHeightAt(Vector3 worldPosition)
    {
        Vector3 origin = new Vector3(worldPosition.x, surfaceHeight, worldPosition.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, lakeBedLayer))
        {
            return hit.point.y;
        }

        return fallbackBottomHeight;
    }

    /// <summary>
    /// Profundidade da água (superfície até ao fundo) num ponto.
    /// </summary>
    public float GetDepthAt(Vector3 worldPosition)
    {
        float bottomHeight = GetBottomHeightAt(worldPosition);
        return Mathf.Max(0f, surfaceHeight - bottomHeight);
    }

    public float GetSubmersionDepth(Vector3 worldPosition)
    {
        return Mathf.Max(0f, surfaceHeight - worldPosition.y);
    }
}