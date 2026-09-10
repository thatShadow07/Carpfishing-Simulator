using UnityEngine;

/// <summary>
/// Defines the water surface and finds the actual lake-bed height below a point.
/// The water surface itself is never treated as the bottom.
/// </summary>
public class WaterDepth : MonoBehaviour
{
    [Header("Water Level")]
    [SerializeField] private float surfaceHeight = 0f;

    [Header("Lake Bottom")]
    [SerializeField] private LayerMask lakeBedLayer;
    [SerializeField] private float fallbackBottomHeight = -5f;
    [SerializeField] private float raycastDistance = 500f;

    public float SurfaceHeight => surfaceHeight;

    public float GetBottomHeightAt(Vector3 worldPosition)
    {
        Vector3 origin = new Vector3(worldPosition.x, surfaceHeight + 0.05f, worldPosition.z);

        if (lakeBedLayer.value != 0)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, lakeBedLayer, QueryTriggerInteraction.Ignore))
                return hit.point.y;
        }
        else
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, raycastDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearestBottom = float.NegativeInfinity;
            bool foundBottom = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.GetComponentInParent<WaterDepth>() != null) continue;
                if (hit.collider.CompareTag("Water")) continue;

                nearestBottom = hit.point.y;
                foundBottom = true;
                break;
            }

            if (foundBottom)
                return nearestBottom;
        }

        return fallbackBottomHeight;
    }

    public float GetDepthAt(Vector3 worldPosition)
    {
        return Mathf.Max(0f, surfaceHeight - GetBottomHeightAt(worldPosition));
    }

    public float GetSubmersionDepth(Vector3 worldPosition)
    {
        return Mathf.Max(0f, surfaceHeight - worldPosition.y);
    }
}