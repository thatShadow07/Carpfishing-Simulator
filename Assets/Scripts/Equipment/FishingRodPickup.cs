// Assets/Scripts/Equipment/FishingRodPickup.cs
using UnityEngine;

// Representa a cana pousada no cenário, pronta a ser apanhada.
// Ao interagir, esconde-se e ativa a versão "equipada" já existente no ItemHolder do jogador.
public class FishingRodPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Pressiona E para pegar na cana";
    [SerializeField] private GameObject equippedRodInstance; // a cana já colocada no ItemHolder, desativada por defeito

    public string InteractionPrompt => interactionPrompt;

    public void Interact(GameObject interactor)
    {
        if (equippedRodInstance != null)
        {
            equippedRodInstance.SetActive(true);
        }

        gameObject.SetActive(false);
    }
}