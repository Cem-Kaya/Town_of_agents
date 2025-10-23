using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting.FullSerializer.Internal;
using UnityEngine;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour
{
    [Header("Root (enable/disable this to show the whole UI)")]
    [SerializeField] private GameObject rootPanel;              // e.g., NewChatCanvas/ChatRoot

    [Header("Hierarchy wiring (match your names)")]
    [SerializeField] private ScrollRect bodyScrollRect;         // NewChatCanvas/ChatRoot/Body (ScrollRect)
    [SerializeField] private RectTransform content;             // Body/Viewport/Content
    [SerializeField] private TMP_InputField playerInputField;   // Footer/PlayerInputField
    [SerializeField] private Button sendBtn;                    // Footer/SendBtn
    [SerializeField] private Button closeBtn;                   // Header/CloseBtn
    [SerializeField] private Image npcPortrait;                 // Header/NPCPortrait
    [SerializeField] private TMP_Text npcName;                  // Header/NPCName
    [SerializeField] private GameObject talkPrompt;             // “Press F to talk”

    [Header("Prefabs (your asset names)")]
    [SerializeField] private MessageBubble NPC_MessageBubble_Left;     // left bubble (NPC)
    [SerializeField] private MessageBubble Player_MessageBubble_Right; // right bubble (Player)

    [Header("Layout")]
    [Range(0.4f, 0.98f)] public float maxBubbleWidthPercent = 0.85f;

    public static ChatUIController Instance { get; private set; }
    public bool IsOpen { get; private set; }

    [Header("LLM")]
    public bool EnableStreamingChat = true;

    private ConversationManager conversationManager;
    private bool isLLMServiceAvailable => conversationManager != null;

    private NPCInteractable currentNPC;
    private Player player;

    // streaming state
    private bool isStreaming;
    private string streamingText;
    private MessageBubble currentNpcBubble;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Wire buttons
        if (sendBtn) sendBtn.onClick.AddListener(OnSendClicked);
        if (playerInputField) playerInputField.onSubmit.AddListener(_ => OnSendClicked());
        if (closeBtn) closeBtn.onClick.AddListener(Close);

        // Init LLM
        conversationManager = GetConversationManager();
        if (conversationManager != null)
        {
            try { Debug.Log($"CULPRIT: {conversationManager.WhoIsCulprit()}"); }
            catch (System.Exception ex) { Debug.LogWarning($"WhoIsCulprit() failed: {ex.Message}"); }
        }

        HidePrompt();
        Close(); // ensures UI is hidden and state reset
    }

    private void OnEnable()
    {
        Canvas.ForceUpdateCanvases();
        SnapToBottom();
        if (IsOpen && playerInputField) playerInputField.ActivateInputField();
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ===== Prompt =====
    public void ShowPrompt() { if (talkPrompt) talkPrompt.SetActive(true); }
    public void HidePrompt() { if (talkPrompt) talkPrompt.SetActive(false); }

    // ===== Open / Close =====
    public void Open(NPCInteractable npc, Player p)
    {
        if (rootPanel && !rootPanel.activeSelf) rootPanel.SetActive(true);

        currentNPC = npc;
        player = p;
        IsOpen = true;
        
        SetHeader(ExtractPortrait(npc), npc ? npc.displayName : "Unknown");

        // freeze movement/AI
        if (player) player.SetInputEnabled(false);
        if (npc && npc.npc) npc.npc.SetAIEnabled(false);

        // clear old bubbles
        if (content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        // reset streaming state
        isStreaming = false;
        streamingText = "";
        currentNpcBubble = null;

        HidePrompt();
        Canvas.ForceUpdateCanvases();
        SnapToBottom();
        playerInputField?.ActivateInputField();
    }

    public void Close()
    {
        // unfreeze
        if (player) player.SetInputEnabled(true);
        if (currentNPC && currentNPC.npc) currentNPC.npc.SetAIEnabled(true);

        currentNPC = null;
        player = null;
        IsOpen = false;

        if (rootPanel) rootPanel.SetActive(false);
        HidePrompt();
    }

    // ===== Header =====
    public void SetHeader(Sprite portrait, string displayName)
    {
        SetPortrait(portrait);
        if (npcName) npcName.text = displayName;
    }    

    // ===== Add bubbles =====
    public MessageBubble AddNPCMessageBubble(string text) => AddBubble(text, fromPlayer: false);
    public MessageBubble AddPlayerMessageBubble(string text) => AddBubble(text, fromPlayer: true);

    private MessageBubble AddBubble(string text, bool fromPlayer)
    {
        var prefab = fromPlayer ? Player_MessageBubble_Right : NPC_MessageBubble_Left;
        if (!prefab || !content) return null;

        var bubble = Instantiate(prefab, content);
        bubble.SetText(text ?? string.Empty);

        float viewportWidth = (bodyScrollRect && bodyScrollRect.viewport)
            ? bodyScrollRect.viewport.rect.width
            : ((RectTransform)transform).rect.width;

        bubble.ClampWidth(viewportWidth * maxBubbleWidthPercent);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        SnapToBottom();
        return bubble;
    }

    private void SnapToBottom()
    {
        if (bodyScrollRect) bodyScrollRect.verticalNormalizedPosition = 0f;
    }

    // ===== Send handling =====
    private void OnSendClicked()
    {
        if (!IsOpen) return;
        if (!playerInputField) return;

        string playerSays = playerInputField.text?.Trim();
        if (string.IsNullOrWhiteSpace(playerSays)) return;

        AddPlayerMessageBubble(playerSays);
        playerInputField.text = "";
        playerInputField.ActivateInputField();

        if (!isLLMServiceAvailable || currentNPC == null)
        {
            AddNPCMessageBubble("…thinking… (LLM unavailable or NPC not set)");
            return;
        }

        if (EnableStreamingChat)
            ChatStreamSafe(playerSays);
        else
            ChatSafe(playerSays);
    }

    // ===== Chat (safe wrappers) =====
    private void ChatSafe(string playerSays)
    {
        if (!GuardConversation("ChatSafe")) return;
        try { StartCoroutine(Chat(playerSays)); }
        catch (System.Exception ex) { Debug.LogError($"Error calling ChatSafe(): {ex.Message}"); }
    }

    private void ChatStreamSafe(string playerSays)
    {
        if (!GuardConversation("ChatStreamSafe")) return;
        try { _ = ChatAsync(playerSays); }          // fire-and-forget
        catch (System.Exception ex) { Debug.LogError($"Error calling ChatStreamSafe(): {ex.Message}"); }
    }

    private bool GuardConversation(string fromWhere)
    {
        if (!isLLMServiceAvailable)
        {
            Debug.LogError($"{fromWhere}: ConversationManager is null.");
            AddNPCMessageBubble("Sorry, the conversation service isn’t available.");
            return false;
        }
        if (currentNPC == null)
        {
            Debug.LogError($"{fromWhere}: currentNPC is null (Open() not called or NPC lost).");
            AddNPCMessageBubble("Sorry, I’m not sure who I’m talking to.");
            return false;
        }
        return true;
    }

    // ===== Chat (streaming) =====
    private async Task ChatAsync(string playerSaid)
    {
        isStreaming = true;
        streamingText = "";
        StartCoroutine(UpdateStreamingText());

        try
        {
            conversationManager.SwitchPlayerTo(currentNPC.displayName);
            var stream = conversationManager.TalkToCurrentPlayerStreaming(playerSaid);
            await foreach (var chunk in stream)
            {
                if (chunk is string)
                    streamingText += (string)chunk;
                else if (chunk is ChatResponse cr)
                    streamingText = cr.Message;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error calling ChatAsync(): {ex.Message}");
            streamingText = "Sorry, I'm having trouble thinking right now...";
        }

        isStreaming = false;
    }

    private IEnumerator UpdateStreamingText()
    {
        if (currentNpcBubble == null)
        {
            currentNpcBubble = AddNPCMessageBubble("");
            yield return null; // give layout a frame
        }

        while (isStreaming)
        {
            currentNpcBubble.SetText(streamingText);
            yield return null;
        }

        currentNpcBubble.SetText(streamingText);
        SnapToBottom();
        currentNpcBubble = null;
    }

    // ===== Chat (non-streaming) =====
    private IEnumerator Chat(string playerSaid)
    {
        string npcResponse = null;
        bool responseReceived = false;

        Task.Run(() =>
        {
            try
            {
                conversationManager.SwitchPlayerTo(currentNPC.displayName);
                npcResponse = conversationManager.TalkToCurrentPlayer(playerSaid).Message;
                responseReceived = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error calling Chat(string playerSaid): {ex.Message}");
                npcResponse = "Sorry, I'm having trouble thinking right now...";
                responseReceived = true;
            }
        });

        while (!responseReceived)
            yield return new WaitForSeconds(0.05f);

        if (!string.IsNullOrWhiteSpace(npcResponse))
            AddNPCMessageBubble(npcResponse);
    }

    // ===== ConversationManager boot =====
    private ConversationManager GetConversationManager()
    {
        var allNPCs = FindObjectsByType<NPCInteractable>(FindObjectsSortMode.None);
        int model = 1;
        //Streaming chat is available only for nano.
        EnableStreamingChat = EnableStreamingChat && model == 1;

        try
        {
            Debug.Log($"StreamingAssets path: {Application.streamingAssetsPath}");
            Debug.Log("All NPCs:");
            foreach (var npc in allNPCs)
                Debug.Log($"NPC: {npc.GetOccupation()}, {npc.displayName}");

            var cm = new ConversationManager(allNPCs, Application.streamingAssetsPath, suspectIntelligenceLevel: model);
            cm.Start();
            return cm;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unable to initialize the conversation manager: {ex.Message}", this);
            Debug.LogException(ex, this);
        }

        return null;
    }

    // Try to get a portrait sprite from the NPC (field or SpriteRenderer)
    private Sprite ExtractPortrait(NPCInteractable npc)
    {
        if (!npc) return null;

        // 1) explicit field on NPCInteractable
        if (npc.portrait) return npc.portrait;

        // 2) fallback: grab the first SpriteRenderer we can find
        var sr = npc.GetComponentInChildren<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }

    // Apply to the UI Image (hide if null)
    private void SetPortrait(Sprite s)
    {
        if (!npcPortrait) return;

        npcPortrait.sprite = s;
        npcPortrait.preserveAspect = true;
        npcPortrait.enabled = s != null;   // hide image if no sprite assigned
    }


}
