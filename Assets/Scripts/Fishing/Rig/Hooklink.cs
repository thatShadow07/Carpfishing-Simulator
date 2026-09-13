using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Hooklink : MonoBehaviour
{
    [Header("Connections")]
    [SerializeField] private Transform hookPoint;
    [SerializeField] private Transform leadPoint;

    [Header("Length")]
    [SerializeField, Min(0.01f)] private float length = 0.20f;

    private LineRenderer lineRenderer;

    public float Length => length;
    public Transform HookPoint => hookPoint;
    public Transform LeadPoint => leadPoint;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    private void LateUpdate()
    {
        if (hookPoint == null || leadPoint == null)
            return;

        lineRenderer.SetPosition(0, hookPoint.position);
        lineRenderer.SetPosition(1, leadPoint.position);
    }

    public void SetConnections(Transform hook, Transform lead)
    {
        hookPoint = hook;
        leadPoint = lead;
    }
}