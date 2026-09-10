using UnityEngine;
using UnityEngine.InputSystem;

public class FishingReel : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private CastingSystem castingSystem;
    [SerializeField] private Transform castOrigin;

    [Header("Recolha")]
    [SerializeField] private float reelForce = 12f;
    [SerializeField] private float catchDistance = 0.3f;

    private Rigidbody currentSinker;

    public void SetSinker(Rigidbody sinker) => currentSinker = sinker;

    private void Update()
    {
        if (currentSinker == null || castOrigin == null) return;

        Sinker sinker = currentSinker.GetComponent<Sinker>();
        if (sinker != null && sinker.GetAttachedFish() != null) return;
        if (Keyboard.current == null) return;

        // Nunca tornamos o chumbo kinematic durante o reel.
        // O Rigidbody continua a reagir à gravidade, água e linha física.
        if (Keyboard.current.rKey.isPressed)
        {
            Vector3 direction = (castOrigin.position - currentSinker.position).normalized;
            currentSinker.AddForce(direction * reelForce, ForceMode.Force);
        }

        if (Vector3.Distance(currentSinker.position, castOrigin.position) <= catchDistance)
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

        Debug.Log("Rig recolhido.");
    }
}