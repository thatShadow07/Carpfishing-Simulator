using UnityEngine;
using UnityEngine.InputSystem;

// Recolhe o rig lançado de volta até à ponta da cana. Enquanto a tecla R
// estiver premida, puxa o Sinker atual em direção ao CastOrigin - primeiro
// na horizontal (como se arrastasse pelo fundo), só subindo mesmo perto do
// fim. Ao chegar perto o suficiente, termina o lançamento e avisa o
// CastingSystem.
public class FishingReel : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private CastingSystem castingSystem;
    [SerializeField] private Transform castOrigin;

    [Header("Recolha")]
    [SerializeField] private float reelSpeed = 4f; // metros por segundo
    [SerializeField] private float catchDistance = 0.3f;
    [SerializeField] private float liftDistance = 2f; // distância horizontal a partir da qual começa a subir

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

        Vector3 current = currentSinker.position;
        Vector3 target = castOrigin.position;

        float horizontalDistance = Vector3.Distance(
            new Vector3(current.x, 0f, current.z),
            new Vector3(target.x, 0f, target.z));

        // Longe: arrasta na horizontal, mantendo a profundidade atual (fundo real)
        // Perto: sobe mesmo em direção à ponta da cana
        Vector3 desiredPoint = horizontalDistance > liftDistance
            ? new Vector3(target.x, current.y, target.z)
            : target;

        Vector3 newPosition = Vector3.MoveTowards(current, desiredPoint, reelSpeed * Time.deltaTime);
        currentSinker.MovePosition(newPosition);

        if (Vector3.Distance(newPosition, target) <= catchDistance)
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