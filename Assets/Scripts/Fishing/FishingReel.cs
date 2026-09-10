using UnityEngine;
using UnityEngine.InputSystem;

// Phase 2: the reel changes available line length. The FishingLine joint then
// transmits the resulting load to the physical rig.
public class FishingReel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CastingSystem castingSystem;
    [SerializeField] private Transform castOrigin;
    [SerializeField] private FishingLine fishingLine;

    [Header("Line Recovery")]
    [SerializeField, Min(0.05f)] private float recoverySpeed = 0.75f;
    [SerializeField, Min(0.01f)] private float catchDistance = 0.3f;
    [SerializeField, Min(0.1f)] private float minimumTravelBeforeFinish = 1f;

    private Rigidbody currentSinker;
    private bool sinkerHasLeftOrigin;

    public void SetFishingLine(FishingLine line)
    {
        fishingLine = line;
    }

    public void SetSinker(Rigidbody sinker)
    {
        currentSinker = sinker;
        sinkerHasLeftOrigin = false;
    }

    private void Update()
    {
        if (currentSinker == null || castOrigin == null || Keyboard.current == null)
            return;

        Sinker sinker = currentSinker.GetComponent<Sinker>();
        if (sinker != null && sinker.GetAttachedFish() != null)
            return;

        float distanceToOrigin = Vector3.Distance(currentSinker.position, castOrigin.position);
        if (distanceToOrigin >= minimumTravelBeforeFinish)
            sinkerHasLeftOrigin = true;

        if (Keyboard.current.rKey.isPressed && fishingLine != null && !fishingLine.IsBroken)
        {
            sinker?.BeginRetrieval();
            fishingLine.ReelIn(recoverySpeed * Time.deltaTime);
        }

        if (sinkerHasLeftOrigin && distanceToOrigin <= catchDistance)
            FinishReel();
    }

    private void FinishReel()
    {
        Destroy(currentSinker.gameObject);
        currentSinker = null;
        sinkerHasLeftOrigin = false;

        if (castingSystem != null)
            castingSystem.ResetCast();

        Debug.Log("Rig recolhido.");
    }
}
