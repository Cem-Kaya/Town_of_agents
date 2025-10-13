public class ChatResponse
{
    public ChatResponse(string from, string to)
    {
        From = from;
        To = to;
    }

    public string Message { get; set; }
    public ActionResponse InvolvedAction { get; set; }

    public string FullJson{ get; set; }
    public string From { get; }
    public string To { get; }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(Message))
            return Message;

        if (InvolvedAction != null)
            return $"Involved Action: {InvolvedAction.Name}";

        return base.ToString();
    }
}
