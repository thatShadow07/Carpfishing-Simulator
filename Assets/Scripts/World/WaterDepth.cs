using UnityEngine;

/// <summary>
/// Defines the depth of a water body at its current prototype stage.
/// For now the lake has a flat surface and a flat bottom.
/// Later this can be extended to support depth variation by position.
/// </summary>
public class WaterDepth : MonoBehaviour
{
    [Header("Water Level")]
    [SerializeField] private float surfaceHeight = 0f;

    [Header("Lake Bottom")]
    [SerializeField] private float bottomHeight = -5f;

    public float SurfaceHeight => surfaceHeight;
    public float BottomHeight => bottomHeight;

    /// <summary>
    /// Returns the water depth at a world position.
    /// Currently every point has the same depth because the prototype lake has a flat bottom.
    /// </summary>
    public float GetDepthAt(Vector3 worldPosition)
    {
        return Mathf.Max(0f, surfaceHeight - bottomHeight);
    }

    /// <summary>
    /// Returns how far below the water surface a point currently is.
    /// </summary>
    public float GetSubmersionDepth(Vector3 worldPosition)
    {
        return Mathf.Max(0f, surfaceHeight - worldPosition.y);
    }
}
