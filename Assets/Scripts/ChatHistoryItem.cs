using System;
using Newtonsoft.Json;
public class ChatHistoryItem
{
    public ChatHistoryItem()
    {
        Timestamp = DateTime.Now;
    }

    public ChatHistoryItem(string from, string to, string message)
    {
        From = from;
        To = to;
        Message = message;
    }

    public ChatHistoryItem(ChatResponse response)
    {
        From = response.From;
        To = response.To;
        Message = response.Message;
        ActivityResult = response.InvolvedAction;
    }
    
    /// <summary>
    /// Timestamp of the message.
    /// </summary>
    [JsonProperty]
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// The name of the agent, who sent the message.
    /// </summary>
    [JsonProperty]
    public string From { get; set; }

    [JsonProperty]
    public string To { get; set; }
    
    /// <summary>
    /// The message text.
    /// </summary>
    [JsonProperty]
    public string Message { get; set; }

    /// <summary>
    /// Gets if the message item is a function call.
    /// </summary>
    [JsonProperty]    
    public bool PlayerPerformedActivity { get => ActivityResult != null; }

    /// <summary>
    /// Gets or sets the return value of the called function.
    /// </summary>
    [JsonProperty]
    public ActionResponse ActivityResult { get; set; }  

    public string ToUnityLogString() => $"[{From} -> {To}]: {Message}";

    public override string ToString() => $"[{Timestamp:F}]\t[{From} -> {To}]:\t{Message}";

    public string SerializeAsJson() => JsonConvert.SerializeObject(this);
}