using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FishingHook : MonoBehaviour
{
    [Header("Bait")]
    [SerializeField] private Transform bait;

    [Header("Hook State")]
    public bool IsHooked { get; private set; }
    public Transform HookedFish { get; private set; }

    [Header("Rig")]
    [SerializeField, Min(0.01f)] private float hooklinkLength = 0.20f;

    public Transform Bait => bait;
    public float HooklinkLength => hooklinkLength;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // O Hook não se move sozinho.
        // A física da luta será adicionada quando estiver ligado ao Hooklink.
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void SetBait(Transform newBait)
    {
        bait = newBait;
    }

    public void HookFish(Transform fish)
    {
        if (fish == null || IsHooked)
            return;

        IsHooked = true;
        HookedFish = fish;

        Debug.Log($"FishingHook: peixe fisgado -> {fish.name}");
    }

    public void UnhookFish()
    {
        IsHooked = false;
        HookedFish = null;

        Debug.Log("FishingHook: peixe desengatado.");
    }

    public bool HasBait()
    {
        return bait != null;
    }
}