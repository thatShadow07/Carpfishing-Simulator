using UnityEngine;

// Desenha a linha entre a ponta da cana e o rig lançado. Não sabe nada
// sobre casting nem física - só liga dois pontos com um LineRenderer.
[RequireComponent(typeof(LineRenderer))]
public class FishingLine : MonoBehaviour
{
    [SerializeField] private Transform lineStart; // a ponta da cana (CastOrigin)

    private LineRenderer lineRenderer;
    private Transform target;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        lineRenderer.enabled = target != null;
    }

    public void Clear()
    {
        target = null;
        lineRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (target == null || lineStart == null) return;

        lineRenderer.SetPosition(0, lineStart.position);
        lineRenderer.SetPosition(1, target.position);
    }
}