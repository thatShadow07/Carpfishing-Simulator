using UnityEngine;
using UnityEngine.InputSystem;

public class FishingReel : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private CastingSystem castingSystem;
    [SerializeField] private Transform castOrigin;

    [Header("Recolha Física")]
    [SerializeField, Min(0f)] private float reelForce = 1.5f;
    [SerializeField, Min(0.01f)] private float catchDistance = 0.3f;
    [SerializeField, Min(0.1f)] private float minimumTravelBeforeFinish = 1f;

    private Rigidbody currentSinker;
    private bool sinkerHasLeftOrigin;

    public void SetSinker(Rigidbody sinker)
    {
        currentSinker = sinker;
        sinkerHasLeftOrigin = false;
    }

    private void Update()
    {
        if (currentSinker == null || castOrigin == null) return;
        if (Keyboard.current == null) return;

        Sinker sinker = currentSinker.GetComponent<Sinker>();
        if (sinker != null && sinker.GetAttachedFish() != null) return;

        float distanceToOrigin = Vector3.Distance(currentSinker.position, castOrigin.position);

        if (distanceToOrigin >= minimumTravelBeforeFinish)
            sinkerHasLeftOrigin = true;

        if (Keyboard.current.rKey.isPressed)
        {
            Vector3 offset = castOrigin.position - currentSinker.position;
            if (offset.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = offset.normalized;

                // If the sinker has settled on the lake bed it becomes kinematic
                // for stability. Reeling must release it before applying pull.
                if (sinker != null && sinker.IsOnBottom)
                    sinker.ReleaseFromBottom(direction);

                currentSinker.AddForce(direction * reelForce, ForceMode.Force);
            }
        }

        if (sinkerHasLeftOrigin && distanceToOrigin <= catchDistance)
            FinishReel();
    }

    private void FinishReel()
    {
        Sinker sinker = currentSinker.GetComponent<Sinker>();
        Transform attachedFish = sinker != null ? sinker.GetAttachedFish() : null;

        if (attachedFish != null)
        {
            FishFightController fight = attachedFish.GetComponent<FishFightController>();
            if (fight != null) fight.EndFight(FishFightResult.Landed);
        }

        Destroy(currentSinker.gameObject);
        currentSinker = null;

        if (castingSystem != null)
            castingSystem.ResetCast();

        sinkerHasLeftOrigin = false;
        Debug.Log("Rig recolhido.");
    }
}