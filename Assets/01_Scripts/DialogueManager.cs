using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Referencias")]
    public CanvasGroup dialogueCanvasGroup;
    public Text speakerNameText;
    public Text dialogueText;

    [Header("Configuración de Escritura")]
    public float typingSpeed = 0.05f;

    [Header("Configuración de Fade")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;
    public float textTransitionDuration = 0.2f;

    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private bool isTransitioning = false;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!dialogueActive || isTransitioning) return;

        // Presionar E para avanzar o completar el texto
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                // Completar el texto instantáneamente
                CompleteText();
            }
            else
            {
                // Avanzar a la siguiente línea con transición
                StartCoroutine(TransitionToNextLine());
            }
        }

        // Presionar ESC para cerrar el diálogo
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(EndDialogueWithFade());
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.dialogueLines.Length == 0) return;

        currentDialogue = dialogue;
        currentLineIndex = 0;
        dialogueActive = true;

        StartCoroutine(ShowDialogueWithFade());
    }

    private IEnumerator ShowDialogueWithFade()
    {
        isTransitioning = true;

        // Limpiar el texto antes de mostrar
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.gameObject.SetActive(true);

            // Fade in del panel
            yield return StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, fadeInDuration));
        }

        // Mostrar la primera línea
        ShowCurrentLine();

        isTransitioning = false;
    }

    private void ShowCurrentLine()
    {
        if (currentLineIndex >= currentDialogue.dialogueLines.Length)
        {
            StartCoroutine(EndDialogueWithFade());
            return;
        }

        DialogueLine line = currentDialogue.dialogueLines[currentLineIndex];

        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        if (dialogueText != null)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(line.text));
        }
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteText()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        DialogueLine line = currentDialogue.dialogueLines[currentLineIndex];
        dialogueText.text = line.text;
        isTyping = false;
    }

    private IEnumerator TransitionToNextLine()
    {
        isTransitioning = true;

        // Fade out del texto actual
        if (dialogueText != null)
        {
            yield return StartCoroutine(FadeText(dialogueText, 1f, 0f, textTransitionDuration));
        }

        currentLineIndex++;

        // Si hay más líneas, mostrar la siguiente
        if (currentLineIndex < currentDialogue.dialogueLines.Length)
        {
            ShowCurrentLine();

            // Fade in del nuevo texto
            if (dialogueText != null)
            {
                yield return StartCoroutine(FadeText(dialogueText, 0f, 1f, textTransitionDuration));
            }
        }
        else
        {
            // Si no hay más líneas, cerrar el diálogo
            yield return StartCoroutine(EndDialogueWithFade());
        }

        isTransitioning = false;
    }

    private IEnumerator EndDialogueWithFade()
    {
        isTransitioning = true;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // Fade out del panel completo
        if (dialogueCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, fadeOutDuration));
            dialogueCanvasGroup.gameObject.SetActive(false);
        }

        // Limpiar los textos después del fade out
        if (dialogueText != null)
        {
            dialogueText.text = "";
            Color c = dialogueText.color;
            c.a = 1f;
            dialogueText.color = c;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
        }

        // Resetear estado
        dialogueActive = false;
        currentDialogue = null;
        currentLineIndex = 0;
        isTransitioning = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    private IEnumerator FadeText(Text text, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            text.color = color;
            yield return null;
        }

        color.a = endAlpha;
        text.color = color;
    }

    public bool IsDialogueActive()
    {
        return dialogueActive;
    }
}