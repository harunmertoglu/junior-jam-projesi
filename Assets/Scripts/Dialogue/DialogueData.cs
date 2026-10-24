using System;
using UnityEngine;

// Bir konuşmanın satırları. Project penceresinde sağ tık > Create > Dialogue > Dialogue Data
// ile oluşturulur, satırlar Inspector'dan yazılır.
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public class DialogueLine
    {
        // Boş bırakılırsa bir önceki satırın konuşanı kullanılır
        public string speakerName;
        [TextArea(2, 5)] public string text;
    }

    [SerializeField] DialogueLine[] lines;

    public DialogueLine[] GetLines()
    {
        return lines;
    }
}
