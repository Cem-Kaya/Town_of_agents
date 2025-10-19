using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChatUI : MonoBehaviour
{
    public static ChatUI Instance { get; private set; }

    [Header("Prompt")]
    public GameObject talkPrompt;

    [Header("Panel")]
    public GameObject chatPanel;
    public TMP_Text headerText;
    public Button endButton;

    [Header("ScrollView")]
    public ScrollRect scroll;     // ScrollView's ScrollRect
    public Transform content;    // ScrollView/Viewport/Content

    [Header("Input")]
    public bool DisableLLM_and_Fake_It;
    public TMP_InputField inputField;
    public Button sendButton;
    public bool sendOnEnter = true;

    [Header("Prefabs")]
    public ChatMessageItem messagePrefab_Player; // your MessageItem_Player
    public ChatMessageItem messagePrefab_NPC;    // your MessageItem_NPC

    public bool IsOpen { get; private set; }

    NPCInteractable currentNPC;
    Player player;

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
            ConversationManager conversationManager = new ConversationManager(allNPCs, Application.streamingAssetsPath, suspectIntelligenceLevel: 3);
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

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        conversationManager = GetConversationManager();
        Debug.Log($"CULPRIT: {conversationManager.WhoIsCulprit()}");

        if (endButton) endButton.onClick.AddListener(Close);
        if (sendButton) sendButton.onClick.AddListener(SendFromInput);

        HidePrompt();
        Close();
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (sendOnEnter && inputField && inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            SendFromInput();
    }

    // ===== Prompt =====
    public void ShowPrompt() { if (talkPrompt) talkPrompt.SetActive(true); }
    public void HidePrompt() { if (talkPrompt) talkPrompt.SetActive(false); }

    // ===== Open/Close =====
    public void Open(NPCInteractable npc, Player p)
    {
        currentNPC = npc;
        player = p;

        if (chatPanel) chatPanel.SetActive(true);
        if (headerText) headerText.text = $"Chatting with: {npc.displayName}";

        // freeze movement/AI
        if (player) player.SetInputEnabled(false);
        if (npc && npc.npc) npc.npc.SetAIEnabled(false);

        // clear old
        foreach (Transform child in content) Destroy(child.gameObject);

        IsOpen = true;
        inputField?.ActivateInputField();
        SnapToBottom();
    }

    public void Close()
    {
        if (chatPanel) chatPanel.SetActive(false);
        IsOpen = false;

        // unfreeze
        if (player) player.SetInputEnabled(true);
        if (currentNPC && currentNPC.npc) currentNPC.npc.SetAIEnabled(true);

        currentNPC = null;
        player = null;
    }

    /// <summary>
    /// Gets the chat history of the NPC agent specified by its name. Returns null if there is no NPC agent by that name.
    /// </summary>
    /// <param name="npcName">The unique name of the NPC agent.</param>
    /// <returns>A list of ChatHistoryItem objects. Null if there is no NPC by that name in this conversation manager.</returns>
    public List<ChatHistoryItem> GetHistoryByNpcName(string npcName)
    {
        return conversationManager?.GetHistoryByNpcName(npcName);
    }

    // ===== Sending =====
    void SendFromInput()
    {
        if (!IsOpen || inputField == null) return;
        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        AddPlayerMessage(msg);
        inputField.text = "";
        inputField.ActivateInputField();

        // Use real conversation manager
        if (!DisableLLM_and_Fake_It && isLLMServiceAvailable && currentNPC != null)
        {
            StartCoroutine(GetNPCReply(msg));
        }
        else
        {
            // Fallback to fake reply if LLM service is not available
            StartCoroutine(FakeNPCReply(msg));
        }
    }

    IEnumerator FakeNPCReply(string playerSaid)
    {
        yield return new WaitForSeconds(0.25f);
        AddNPCMessage($"you said: {playerSaid}");
    }

    IEnumerator GetNPCReply(string playerSaid)
    {   
        // TODO: May be some indicator for thinking, while the user is waiting for the response?
        
        string npcResponse = null;
        bool responseReceived = false;
        System.Threading.Tasks.Task.Run(() =>
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
                Debug.LogError($"Error getting NPC response: {ex.Message}");
                npcResponse = "Sorry, I'm having trouble thinking right now...";
                responseReceived = true;
            }
        });
        
        // Wait for the response
        while (!responseReceived)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Add the NPC's response to the chat
        if (!string.IsNullOrEmpty(npcResponse))
        {
            AddNPCMessage(npcResponse);
        }
    }

    // ===== Add messages =====
    public void AddPlayerMessage(string text) => AddMessage(messagePrefab_Player, text, isNPC: false);
    public void AddNPCMessage(string text) => AddMessage(messagePrefab_NPC, text, isNPC: true);

    void AddMessage(ChatMessageItem prefab, string text, bool isNPC)
    {
        // force a talk for NPC immediately to prove audio path works
        if (isNPC && currentNPC)
            currentNPC.GetComponent<NPCTalkAudio>()?.PlayTalk();

        if (!prefab || !content) return;

        var item = Instantiate(prefab, content);

        if (item.text == null)
        {
            item.text = item.GetComponentInChildren<TMPro.TMP_Text>();
            if (item.text == null)
            {
                Debug.LogError($"'{prefab.name}' is missing TMP_Text. (Audio already fired above)");
                return;
            }
        }

        item.Set(text);
        SnapToBottomNextFrame();
    }


    // ===== Scrolling =====
    void SnapToBottom()
    {
        if (scroll) scroll.verticalNormalizedPosition = 0f; // 0 = bottom
    }
    void SnapToBottomNextFrame() { StartCoroutine(CoBottom()); }
    IEnumerator CoBottom() { yield return null; SnapToBottom(); }
}
