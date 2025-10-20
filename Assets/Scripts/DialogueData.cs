using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;  // Nombre del que habla (puede ser el jugador u otro)
    [TextArea(3, 10)]
    public string text;         // El texto del diálogo
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] dialogueLines;
}