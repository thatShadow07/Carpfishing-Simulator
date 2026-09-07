using UnityEngine;

// Qualquer objeto com que o jogador possa interagir (tecla E) implementa isto.
// Mantém a deteção (PlayerInteraction) separada da lógica de cada objeto -
// a cana, a caixa, o carro... cada um decide o que "Interact()" significa.
public interface IInteractable
{
    string InteractionPrompt { get; } // ex: "Pressiona E para apanhar a cana"
    void Interact(GameObject interactor);
}