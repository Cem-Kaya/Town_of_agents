using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour
{
    [Header("Hierarchy wiring (match your names)")]
    [SerializeField] private ScrollRect bodyScrollRect;         // NewChatCanvas/ChatRoot/Body (ScrollRect)
    [SerializeField] private RectTransform content;             // Body/Viewport/Content
    [SerializeField] private TMP_InputField playerInputField;   // Footer/PlayerInputField
    [SerializeField] private Button sendBtn;                    // Footer/SendBtn
    [SerializeField] private Button closeBtn;                   // Header/CloseBtn
    [SerializeField] private Image npcPortrait;                 // Header/NPCPortrait
    [SerializeField] private TMP_Text npcName;                  // Header/NPCName

    [Header("Prefabs (your asset names)")]
    [SerializeField] private MessageBubble NPC_MessageBubble_Left;     // left bubble (NPC)
    [SerializeField] private MessageBubble Player_MessageBubble_Right; // right bubble (Player)

    [Header("Layout")]
    [Range(0.4f, 0.95f)] public float maxBubbleWidthPercent = 0.72f;

    private void Awake()
    {
        if (sendBtn) sendBtn.onClick.AddListener(OnSendClicked);
        if (playerInputField) playerInputField.onSubmit.AddListener(_ => OnSendClicked());
        if (closeBtn) closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable()
    {
        Canvas.ForceUpdateCanvases();
        SnapToBottom();
        if (playerInputField) playerInputField.ActivateInputField();
    }

    // -------- API you’ll call from your dialogue system --------

    public void SetHeader(Sprite portrait, string displayName)
    {
        if (npcPortrait) npcPortrait.sprite = portrait;
        if (npcName) npcName.text = displayName;
    }

    public void AddNPC(string text) => AddBubble(text, fromPlayer: false);
    public void AddPlayer(string text) => AddBubble(text, fromPlayer: true);

    // -----------------------------------------------------------

    private void AddBubble(string text, bool fromPlayer)
    {
        var prefab = fromPlayer ? Player_MessageBubble_Right : NPC_MessageBubble_Left;
        if (!prefab || !content) return;

        var bubble = Instantiate(prefab, content);
        bubble.SetText(text);

        float viewportWidth = (bodyScrollRect && bodyScrollRect.viewport)
            ? bodyScrollRect.viewport.rect.width
            : ((RectTransform)transform).rect.width;

        bubble.ClampWidth(viewportWidth * maxBubbleWidthPercent);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        SnapToBottom();
    }

    private void OnSendClicked()
    {
        if (!playerInputField) return;

        string t = playerInputField.text;
        if (string.IsNullOrWhiteSpace(t)) return;

        AddPlayer(t.Trim());
        playerInputField.text = "";
        playerInputField.ActivateInputField();

        // demo echo — replace with your conversation logic
        AddNPC("Got it. Anything else?");
    }

    private void SnapToBottom()
    {
        if (bodyScrollRect) bodyScrollRect.verticalNormalizedPosition = 0f;
    }
}
