using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Configuración de Diálogo")]
    public DialogueData dialogue;

    [Header("Visual (Opcional)")]
    public GameObject interactionPrompt; // Un sprite o UI que diga "Presiona E"

    private bool playerInRange = false;

    private void Update()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(playerInRange && !DialogueManager.Instance.IsDialogueActive());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    public bool TryInteract()
    {
        if (playerInRange && dialogue != null && dialogue.dialogueLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(dialogue);
            return true;
        }
        return false;
    }
}