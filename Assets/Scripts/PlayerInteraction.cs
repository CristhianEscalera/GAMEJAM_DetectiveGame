using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private InteractableObject currentInteractable;

    private void Update()
    {
        // Solo permitir interacción si no hay diálogo activo
        if (!DialogueManager.Instance.IsDialogueActive() && Input.GetKeyDown(KeyCode.E))
        {
            if (currentInteractable != null)
            {
                currentInteractable.TryInteract();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        InteractableObject interactable = collision.GetComponent<InteractableObject>();
        if (interactable != null)
        {
            currentInteractable = interactable;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        InteractableObject interactable = collision.GetComponent<InteractableObject>();
        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable = null;
        }
    }
}