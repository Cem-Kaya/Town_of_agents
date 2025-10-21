using UnityEngine;

public class ChatDemo : MonoBehaviour
{
    [SerializeField] private ChatUIController chat;

    private void Start()
    {
        chat.SetHeader(null, "Farmer Hans");
        chat.AddNPC("Welcome! The market opens at dawn.");
        chat.AddNPC("Type and press Enter, or click Send.");
    }
}
