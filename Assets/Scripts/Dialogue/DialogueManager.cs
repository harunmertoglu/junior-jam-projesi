using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Ekranın altındaki diyalog kutusu. Sahnede yoksa ilk diyalogda kendini ve kutusunu oluşturur,
// sahneye elle bir şey koymak gerekmez. Zıplama tuşu ya da sol tık: yazı yazılıyorsa bitirir, bittiyse sonraki satır.
public class DialogueManager : MonoBehaviour
{
    // true = diyalog açıldı, false = kapandı. PlayerMovement ve PlayerDash dinler, karakteri kilitler
    public static event Action<bool> OnDialogueChanged;

    public static DialogueManager Instance { get; private set; }

    [Header("Typewriter")]
    // Saniyede kaç harf yazılır
    [SerializeField] float charactersPerSecond = 40f;

    [Header("Box")]
    [SerializeField] int fontSize = 32;
    [SerializeField] Color boxColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] Color speakerColor = new Color(1f, 0.8f, 0.4f);

    GameObject dialoguePanel;
    Text speakerText;
    Text lineText;

    DialogueData.DialogueLine[] currentLines;
    int currentLineIndex;
    string currentSpeaker;
    Action onFinished;
    Coroutine typeCoroutine;
    bool isTyping;
    bool isOpen;

    public bool IsOpen()
    {
        return isOpen;
    }

    public static DialogueManager GetOrCreate()
    {
        if (Instance == null)
        {
            new GameObject("DialogueManager").AddComponent<DialogueManager>();
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CreateDialogueBox();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnEnable()
    {
        InputManager.OnJump += JumpInputs;
    }

    void OnDisable()
    {
        InputManager.OnJump -= JumpInputs;
    }

    void Update()
    {
        if (isOpen && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Advance();
        }
    }

    void JumpInputs(bool value)
    {
        if (isOpen && value)
        {
            Advance();
        }
    }

    public void StartDialogue(DialogueData dialogue, Action onFinished = null)
    {
        if (isOpen || dialogue == null || dialogue.GetLines() == null || dialogue.GetLines().Length == 0)
        {
            return;
        }

        currentLines = dialogue.GetLines();
        currentLineIndex = 0;
        currentSpeaker = "";
        this.onFinished = onFinished;
        isOpen = true;

        dialoguePanel.SetActive(true);
        OnDialogueChanged?.Invoke(true);
        ShowLine();
    }

    void Advance()
    {
        // Yazı daha bitmediyse önce satırın tamamını göster
        if (isTyping)
        {
            StopCoroutine(typeCoroutine);
            lineText.text = currentLines[currentLineIndex].text;
            isTyping = false;
            return;
        }

        currentLineIndex++;
        if (currentLineIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    void ShowLine()
    {
        DialogueData.DialogueLine line = currentLines[currentLineIndex];
        if (!string.IsNullOrEmpty(line.speakerName))
        {
            currentSpeaker = line.speakerName;
        }
        speakerText.text = currentSpeaker;

        typeCoroutine = StartCoroutine(TypeLine(line.text));
    }

    IEnumerator TypeLine(string text)
    {
        isTyping = true;
        lineText.text = "";

        float visibleCharacters = 0f;
        while (visibleCharacters < text.Length)
        {
            visibleCharacters += charactersPerSecond * Time.deltaTime;
            lineText.text = text.Substring(0, Mathf.Min(text.Length, (int)visibleCharacters));
            yield return null;
        }

        isTyping = false;
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        StartCoroutine(UnlockNextFrame());
    }

    // Diyaloğu kapatan zıplama basışı karaktere de gidip onu zıplatmasın diye kilit bir kare sonra açılır
    IEnumerator UnlockNextFrame()
    {
        yield return null;

        isOpen = false;
        OnDialogueChanged?.Invoke(false);

        Action finished = onFinished;
        onFinished = null;
        finished?.Invoke();
    }

    void CreateDialogueBox()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("DialogueCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Diğer menülerin üstünde dursun
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Alt kenarda, ekranın %80'i genişliğinde kutu
        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = dialoguePanel.AddComponent<Image>();
        panelImage.color = boxColor;
        RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.04f);
        panelRect.anchorMax = new Vector2(0.9f, 0.26f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        speakerText = CreateText("Speaker", dialoguePanel.transform, font, fontSize, FontStyle.Bold, speakerColor);
        RectTransform speakerRect = speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0.5f, 1f);
        speakerRect.offsetMin = new Vector2(30f, -60f);
        speakerRect.offsetMax = new Vector2(-30f, -15f);

        lineText = CreateText("Line", dialoguePanel.transform, font, fontSize, FontStyle.Normal, Color.white);
        RectTransform lineRect = lineText.rectTransform;
        lineRect.anchorMin = Vector2.zero;
        lineRect.anchorMax = Vector2.one;
        lineRect.offsetMin = new Vector2(30f, 20f);
        lineRect.offsetMax = new Vector2(-30f, -65f);

        dialoguePanel.SetActive(false);
    }

    Text CreateText(string name, Transform parent, Font font, int size, FontStyle style, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
