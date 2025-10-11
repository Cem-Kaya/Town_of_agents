using System;
using UnityEngine;

public class ChatHistoryItem
{
    public ChatHistoryItem()
    {
        Timestamp = DateTime.Now;
    }

    public ChatHistoryItem(string who, string message)
    {
        Who = who;
        Message = message;
    }

    /// <summary>
    /// Timestamp of the message.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The name of the agent, who sent the message.
    /// </summary>
    public string Who { get; set; }

    /// <summary>
    /// The message text.
    /// </summary>
    public string Message { get; set; }

    public override string ToString() => $"[{Timestamp:F}]\t[{Who}]\t{Message}";
}
