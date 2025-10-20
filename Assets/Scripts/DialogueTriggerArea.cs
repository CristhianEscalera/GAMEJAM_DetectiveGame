using UnityEngine;

public class DialogueTriggerArea : MonoBehaviour
{
    [Header("Configuración de Diálogo Automático")]
    public DialogueData dialogue;

    [Header("Comportamiento")]
    [Tooltip("Si está activado, el trigger se ejecuta solo una vez y luego se destruye")]
    public bool destroyAfterUse = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo activar si es el jugador y no se ha activado antes
        if (collision.CompareTag("Player") && !hasTriggered)
        {
            // Verificar que hay un DialogueManager y un diálogo asignado
            if (DialogueManager.Instance != null && dialogue != null && dialogue.dialogueLines.Length > 0)
            {
                hasTriggered = true;
                DialogueManager.Instance.StartDialogue(dialogue);

                // Destruir el objeto después de activarse si está configurado
                if (destroyAfterUse)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}