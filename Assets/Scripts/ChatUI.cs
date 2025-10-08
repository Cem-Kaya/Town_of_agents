using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    public TMP_InputField inputField;
    public Button sendButton;
    public bool sendOnEnter = true;

    [Header("Prefabs")]
    public ChatMessageItem messagePrefab_Player; // your MessageItem_Player
    public ChatMessageItem messagePrefab_NPC;    // your MessageItem_NPC

    public bool IsOpen { get; private set; }

    NPCInteractable currentNPC;
    Player player;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

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

    // ===== Sending =====
    void SendFromInput()
    {
        if (!IsOpen || inputField == null) return;
        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        AddPlayerMessage(msg);
        inputField.text = "";
        inputField.ActivateInputField();

        // Fake NPC reply for now
        StartCoroutine(FakeNPCReply(msg));
    }

    IEnumerator FakeNPCReply(string playerSaid)
    {
        yield return new WaitForSeconds(0.25f);
        AddNPCMessage($"you said: {playerSaid}");
    }

    // ===== Add messages =====
    public void AddPlayerMessage(string text) => AddMessage(messagePrefab_Player, text);
    public void AddNPCMessage(string text) => AddMessage(messagePrefab_NPC, text);

    void AddMessage(ChatMessageItem prefab, string text)
    {
        if (!prefab || !content) return;
        var item = Instantiate(prefab, content);
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
