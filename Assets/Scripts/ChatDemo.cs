using UnityEngine;

public class ChatDemo : MonoBehaviour
{
    [SerializeField] private ChatUIController chat;

    private void Start()
    {
        chat.SetHeader(null, "Farmer Hans");
        chat.AddNPCMessageBubble("Welcome! The market opens at dawn.");
        chat.AddNPCMessageBubble("Type and press Enter, or click Send.");
    }
}
