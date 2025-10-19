public class PromptInfo
{
    public PromptInfo(string promptText, string role, bool isCulprit)
    {
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new System.ArgumentException($"'{nameof(promptText)}' cannot be null or whitespace.", nameof(promptText));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new System.ArgumentException($"'{nameof(role)}' cannot be null or whitespace.", nameof(role));
        }

        PlayerRole = role;
        IsCulprit = isCulprit;
        PromptText = promptText;
    }
    
    public string PromptText { get; set; }
    public string PlayerRole { get; set; }
    public bool IsCulprit { get; set; }
    public string Key => $"{nameof(PlayerRole)}:{PlayerRole}-{nameof(IsCulprit)}:{IsCulprit}";
    public override string ToString() => Key;

    public void SetVariable(string variableName, string value)
    {
        PromptText = PromptText.Replace("{" + variableName + "}", value);
    }

    public void SetName(string name) => SetVariable("name", name);
    public void SetTownCollectiveMemory(string value) => SetVariable("TOWN COLLECTIVE MEMORY", value);
    public void SetPossessions(string value) => SetVariable("possessions", value);
    public void SetDefaultRules(string value) => SetVariable("general_rules", value);
}