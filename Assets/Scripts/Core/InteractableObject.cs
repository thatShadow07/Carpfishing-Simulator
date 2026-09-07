using UnityEngine;
using UnityEngine.Events;

// Versão genérica e simples de um objeto interagível - serve para testar
// o sistema (o cubo) e para casos simples que só precisam de disparar um
// evento (abrir uma porta, tocar um som). Sistemas mais complexos (cana,
// loja, rig) vão ter a sua própria classe a implementar IInteractable diretamente.
public class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pressiona E para interagir";
    [SerializeField] private UnityEvent onInteract;

    public string InteractionPrompt => interactionPrompt;

    public void Interact(GameObject interactor)
    {
        Debug.Log($"{interactor.name} interagiu com {gameObject.name}");
        onInteract?.Invoke();
    }
}