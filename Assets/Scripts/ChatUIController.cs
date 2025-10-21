using System.Collections;
using System.Threading.Tasks;
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
    [SerializeField] private GameObject talkPrompt;

    [Header("Layout")]
    [Range(0.4f, 0.95f)] public float maxBubbleWidthPercent = 0.72f;
    internal static ChatUIController Instance { get; private set;}    

    #region LLM initialization and components
    public bool EnableStreamingChat = false;
    private ConversationManager conversationManager;
    private bool isLLMServiceAvailable => conversationManager != null;

    private ConversationManager GetConversationManager()
    {
        //Instantiate the ConversationManager. You can use conversation manager to interact with
        //all NPCs in the game via configured LLM service. We need all NPCs in the scene and their system prompts to initialize it.
        //Random one of them will be the Culprit.
        NPCInteractable[] allNPCs = FindObjectsByType<NPCInteractable>(FindObjectsSortMode.None);

        try
        {
            Debug.Log($"StreamingAssets path: {Application.streamingAssetsPath}");
            Debug.Log("All NPCs:");
            foreach (var npc in allNPCs)
                Debug.Log($"NPC: {npc.GetOccupation()}, {npc.displayName}");

            ConversationManager conversationManager = new ConversationManager(allNPCs, Application.streamingAssetsPath, suspectIntelligenceLevel: 1);
            //This retrieves the LLM parameters such as API key. Raises error if API key not found.
            //It initializes the LLM agent instructions for each NPC player.
            conversationManager.Start();
            return conversationManager;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unable to initialize the conversation manager: {ex.Message}", this);
            Debug.LogException(ex, this);
        }

        return null;
    }

    #endregion

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

        if (sendBtn) sendBtn.onClick.AddListener(OnSendClicked);
        if (playerInputField) playerInputField.onSubmit.AddListener(_ => OnSendClicked());
        if (closeBtn) closeBtn.onClick.AddListener(() => gameObject.SetActive(false));

        conversationManager = GetConversationManager();
        Debug.Log($"CULPRIT: {conversationManager.WhoIsCulprit()}");

        HidePrompt();
        Close();
    }

    private void OnEnable()
    {
        Canvas.ForceUpdateCanvases();
        SnapToBottom();
        if (playerInputField) playerInputField.ActivateInputField();
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void ShowPrompt() { if (talkPrompt) talkPrompt.SetActive(true); }
    public void HidePrompt() { if (talkPrompt) talkPrompt.SetActive(false); }

    private NPCInteractable currentNPC;
    private Player player;
    public void Open(NPCInteractable npc, Player p)
    {
        currentNPC = npc;
        player = p;

        SetHeader(null, npc.displayName);

        // freeze movement/AI
        if (player) player.SetInputEnabled(false);
        if (npc && npc.npc) npc.npc.SetAIEnabled(false);

        // clear old
        foreach (Transform child in content) Destroy(child.gameObject);
        IsOpen = true;
        HidePrompt();
    }

    public void Close()
    {   
        // unfreeze
        if (player) player.SetInputEnabled(true);
        if (currentNPC && currentNPC.npc) currentNPC.npc.SetAIEnabled(true);
        currentNPC = null;
        player = null;
        IsOpen = false;
    }

    // -------- API you�ll call from your dialogue system --------

    public void SetHeader(Sprite portrait, string displayName)
    {
        if (npcPortrait) npcPortrait.sprite = portrait;
        if (npcName) npcName.text = displayName;
    }

    public MessageBubble AddNPCMessageBubble(string text) => AddBubble(text, fromPlayer: false);
    public MessageBubble AddPlayerMessageBubble(string text) => AddBubble(text, fromPlayer: true);

    // -----------------------------------------------------------

    private MessageBubble AddBubble(string text, bool fromPlayer)
    {
        var prefab = fromPlayer ? Player_MessageBubble_Right : NPC_MessageBubble_Left;
        if (!prefab || !content) return null;

        var bubble = Instantiate(prefab, content);
        bubble.SetText(text);

        float viewportWidth = (bodyScrollRect && bodyScrollRect.viewport)
            ? bodyScrollRect.viewport.rect.width
            : ((RectTransform)transform).rect.width;

        bubble.ClampWidth(viewportWidth * maxBubbleWidthPercent);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        SnapToBottom();
        return bubble;
    }

    private void OnSendClicked()
    {
        if (!playerInputField) return;

        string playerSays = playerInputField.text?.Trim();
        if (string.IsNullOrWhiteSpace(playerSays)) return;

        AddPlayerMessageBubble(playerSays);
        playerInputField.text = "";
        playerInputField.ActivateInputField();

        // demo echo � replace with your conversation logic
        //AddNPCMessageBubble("Got it. Anything else?");
        if (EnableStreamingChat)
            ChatStreamSafe(playerSays);
        else
            ChatSafe(playerSays);
    }

    private void SnapToBottom()
    {
        if (bodyScrollRect) bodyScrollRect.verticalNormalizedPosition = 0f;
    }

    #region chat handling

    private void ChatSafe(string playerSays)
    {
        try
        {
            StartCoroutine(Chat(playerSays));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error calling ChatSafe(): {ex.Message}");            
        }
    }

    private void ChatStreamSafe(string playerSays)
    {
        try
        {
            //This is an async method, call-and-forget strategy applied for simplicity.
            _ = ChatAsync(playerSays);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error calling ChatStreamSafe(): {ex.Message}");            
        }
    }
    
    private bool isStreaming;
    private string streamingText;    

    private async Task ChatAsync(string playerSaid)
    {
        isStreaming = true;
        streamingText = "";
        // Start coroutine to update UI
        StartCoroutine(UpdateStreamingText());
            
        try
        {
            conversationManager.SwitchPlayerTo(currentNPC.displayName);
            var stream = conversationManager.TalkToCurrentPlayerStreaming(playerSaid);
            await foreach (var chunk in stream)
            {
                streamingText += chunk;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error calling ChatAsync(): {ex.Message}");
            streamingText = "Sorry, I'm having trouble thinking right now...";
        }

        isStreaming = false;
    } 

    private MessageBubble currentNpcBubble;
    private IEnumerator UpdateStreamingText()
    {
        if (currentNpcBubble == null)
        {
            currentNpcBubble = AddNPCMessageBubble(streamingText);
            yield return null;
        }

        while (isStreaming)
        {
            currentNpcBubble.SetText(streamingText);
            yield return null; // Wait one frame
        }

        // Final update
        currentNpcBubble.SetText(streamingText);
        SnapToBottom();
        currentNpcBubble = null;
    }
    
    IEnumerator Chat(string playerSaid)
    {
        // TODO: May be some indicator for thinking, while the user is waiting for the response?

        string npcResponse = null;
        bool responseReceived = false;
        Task.Run(() =>
        {
            try
            {
                // Switch to the current NPC in the conversation manager
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

        // Wait for the response
        while (!responseReceived)
            yield return new WaitForSeconds(0.1f);

        // Add the NPC's response to the chat
        if (!string.IsNullOrWhiteSpace(npcResponse))
            AddNPCMessageBubble(npcResponse);
    }

    #endregion
}
