using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Deteção")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionRange = 3f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI promptText;

    private IInteractable currentInteractable;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        DetectInteractable();
        HandleInteractionInput();
    }

    private void DetectInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange)
            && hit.collider.TryGetComponent(out IInteractable interactable))
        {
            ShowPrompt(interactable);
        }
        else
        {
            HidePrompt();
        }
    }

    private void ShowPrompt(IInteractable interactable)
    {
        currentInteractable = interactable;

        if (promptText != null)
        {
            promptText.text = interactable.InteractionPrompt;
            promptText.gameObject.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        currentInteractable = null;

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void HandleInteractionInput()
    {
        if (currentInteractable != null
            && Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame)
        {
            currentInteractable.Interact(gameObject);
        }
    }
}