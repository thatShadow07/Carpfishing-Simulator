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
    [SerializeField] private float minimumTravelBeforeFinish = 1f;

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

        Sinker sinker = currentSinker.GetComponent<Sinker>();
        if (sinker != null && sinker.GetAttachedFish() != null) return;
        if (Keyboard.current == null) return;

        float distanceToOrigin = Vector3.Distance(currentSinker.position, castOrigin.position);

        // O chumbo nasce no castOrigin. Só permitimos finalizar a recolha
        // depois de ele se ter afastado uma distância real do ponto de lançamento.
        if (distanceToOrigin >= minimumTravelBeforeFinish)
            sinkerHasLeftOrigin = true;

        // O reel aplica força. O Rigidbody continua completamente físico.
        if (Keyboard.current.rKey.isPressed)
        {
            Vector3 offset = castOrigin.position - currentSinker.position;
            if (offset.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = offset.normalized;
                currentSinker.AddForce(direction * reelForce, ForceMode.Force);
            }
        }

        // Só recolhemos/destruímos o chumbo depois de ele realmente ter
        // saído do ponto de lançamento e voltar ao castOrigin.
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