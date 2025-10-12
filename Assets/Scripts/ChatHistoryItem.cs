using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
    [JsonProperty]
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// The name of the agent, who sent the message.
    /// </summary>
    [JsonProperty]
    public string Who { get; set; }
    
    /// <summary>
    /// The message text.
    /// </summary>
    [JsonProperty]
    public string Message { get; set; }

    /// <summary>
    /// Set to true, if the message item is a function call.
    /// </summary>
    [JsonProperty]    
    public bool PlayerPerformedActivity { get; set; }

    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    [JsonProperty]
    public string ActivityName { get; set; }

    /// <summary>
    /// Gets or sets the called function parameters.
    /// </summary>
    [JsonProperty]
    public Dictionary<string, string> ActivityParameters { get; set; }

    /// <summary>
    /// Gets or sets the return value of the called function.
    /// </summary>
    [JsonProperty]
    public string ActivityResult { get; set; }  

    public string ToUnityLogString() => $"[{Who}]: {Message}";

    public override string ToString() => $"[{Timestamp:F}]\t[{Who}]:\t{Message}";

    public string SerializeAsJson() => JsonConvert.SerializeObject(this);
}