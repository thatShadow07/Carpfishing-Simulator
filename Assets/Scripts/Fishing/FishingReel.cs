using UnityEngine;
using UnityEngine.InputSystem;

// Recolhe o rig lançado de volta até à ponta da cana. Enquanto a tecla R
// estiver premida, puxa o Sinker atual em direção ao CastOrigin; ao chegar
// perto o suficiente, termina o lançamento e avisa o CastingSystem.
public class FishingReel : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private CastingSystem castingSystem;
    [SerializeField] private Transform castOrigin;

    [Header("Recolha")]
    [SerializeField] private float reelSpeed = 4f; // metros por segundo
    [SerializeField] private float catchDistance = 0.3f;

    private Rigidbody currentSinker;

    public void SetSinker(Rigidbody sinker)
    {
        currentSinker = sinker;
    }

    private void Update()
    {
        if (currentSinker == null || castOrigin == null) return;
        if (Keyboard.current == null || !Keyboard.current.rKey.isPressed) return;

        currentSinker.isKinematic = true; // controlo manual enquanto recolhemos

        Vector3 newPosition = Vector3.MoveTowards(
            currentSinker.position,
            castOrigin.position,
            reelSpeed * Time.deltaTime);

        currentSinker.MovePosition(newPosition);

        if (Vector3.Distance(newPosition, castOrigin.position) <= catchDistance)
        {
            FinishReel();
        }
    }

    private void FinishReel()
    {
        Destroy(currentSinker.gameObject);
        currentSinker = null;

        if (castingSystem != null)
        {
            castingSystem.ResetCast();
        }

        Debug.Log("Rig recolhido.");
    }
}