using UnityEngine;
using UnityEngine.InputSystem;

// Recolhe o rig lançado até à ponta da cana. Se o rig estiver ligado a uma
// carpa, a carpa acompanha o movimento do rig através da ligação do Sinker.
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

    public void SetSinker(Rigidbody sinker)
    {
        currentSinker = sinker;
    }

    private void Update()
    {
        if (currentSinker == null || castOrigin == null) return;
        if (Keyboard.current == null || !Keyboard.current.rKey.isPressed) return;

        currentSinker.isKinematic = true;

        Vector3 current = currentSinker.position;
        Vector3 target = castOrigin.position;

        float horizontalDistance = Vector3.Distance(
            new Vector3(current.x, 0f, current.z),
            new Vector3(target.x, 0f, target.z));

        Vector3 desiredPoint = horizontalDistance > liftDistance
            ? new Vector3(target.x, current.y, target.z)
            : target;

        Vector3 newPosition = Vector3.MoveTowards(
            current,
            desiredPoint,
            reelSpeed * Time.deltaTime);

        Vector3 delta = newPosition - current;
        currentSinker.MovePosition(newPosition);

        // Se houver uma carpa presa ao rig, ela é puxada juntamente com ele.
        Sinker sinker = currentSinker.GetComponent<Sinker>();
        if (sinker != null && delta.sqrMagnitude > 0.000001f)
        {
            sinker.MoveWithFish(delta);
        }

        if (Vector3.Distance(newPosition, target) <= catchDistance)
        {
            FinishReel();
        }
    }

    private void FinishReel()
    {
        Sinker sinker = currentSinker.GetComponent<Sinker>();
        Transform attachedFish = sinker != null ? sinker.GetAttachedFish() : null;

        if (attachedFish != null)
        {
            FishFightController fight = attachedFish.GetComponent<FishFightController>();
            if (fight != null)
            {
                fight.EndFight();
            }
        }

        Destroy(currentSinker.gameObject);
        currentSinker = null;

        if (castingSystem != null)
        {
            castingSystem.ResetCast();
        }

        Debug.Log("Rig recolhido.");
    }
}