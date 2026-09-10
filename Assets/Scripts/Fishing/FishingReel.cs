using UnityEngine;
using UnityEngine.InputSystem;

public class FishingReel : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private CastingSystem castingSystem;
    [SerializeField] private Transform castOrigin;

    [Header("Recolha")]
    [SerializeField] private float reelSpeed = 4f;
    [SerializeField] private float catchDistance = 0.3f;
    [SerializeField] private float liftDistance = 2f;

    private Rigidbody currentSinker;

    public void SetSinker(Rigidbody sinker) => currentSinker = sinker;

    private void Update()
    {
        if (currentSinker == null || castOrigin == null) return;
        Sinker sinker = currentSinker.GetComponent<Sinker>();
        if (sinker != null && sinker.GetAttachedFish() != null) return;
        if (Keyboard.current == null || !Keyboard.current.rKey.isPressed) return;

        currentSinker.isKinematic = true;
        Vector3 current = currentSinker.position;
        Vector3 target = castOrigin.position;
        float horizontalDistance = Vector3.Distance(new Vector3(current.x, 0f, current.z), new Vector3(target.x, 0f, target.z));
        Vector3 desiredPoint = horizontalDistance > liftDistance ? new Vector3(target.x, current.y, target.z) : target;
        Vector3 newPosition = Vector3.MoveTowards(current, desiredPoint, reelSpeed * Time.deltaTime);
        currentSinker.MovePosition(newPosition);

        if (Vector3.Distance(newPosition, target) <= catchDistance) FinishReel();
    }

    private void FinishReel()
    {
        Sinker sinker = currentSinker.GetComponent< Sinker>();
        Transform attachedFish = sinker != null ? sinker.GetAttachedFish() : null;
        if (attachedFish != null)
        {
            FishFightController fight = attachedFish.GetComponent<FishFightController>();
            if (fight != null) fight.EndFight(FishFightResult.Landed);
        }

        Destroy(currentSinker.gameObject);
        currentSinker = null;
        if (castingSystem != null) castingSystem.ResetCast();
        Debug.Log("Rig recolhido.");
    }
}
