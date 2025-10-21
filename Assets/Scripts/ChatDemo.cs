using System.Collections;
using UnityEngine;

public class ChatDemo : MonoBehaviour
{
    [SerializeField] private ChatUIController chat;
    [SerializeField] private string demoName = "Farmer Hans";
    [SerializeField] private bool openOnStart = true;

    private void Start()
    {
        if (!chat) chat = ChatUIController.Instance;
        if (!chat)
        {
            Debug.LogError("[ChatDemo] ChatUIController not found.");
            return;
        }
        StartCoroutine(SeedDemo());
    }

    private IEnumerator SeedDemo()
    {
        // wait a frame so ChatUIController.Awake() finishes (it calls Close())
        yield return null;

        if (openOnStart)
        {
            // Opens UI without freezing anything (player/npc are null so no freeze).
            chat.Open(null, null);
        }

        // set header + seed lines
        chat.SetHeader(null, demoName);
        chat.AddNPCMessageBubble("Welcome! The market opens at dawn.");
        chat.AddNPCMessageBubble("Type and press Enter, or click Send.");
    }
}
