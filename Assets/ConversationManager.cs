using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class ConversationManager
{
    private readonly string playerModel;
    //private string model = "gpt-5-nano-2025-08-07";//"gpt-5-mini";
    private List<NPCInteractable> playerNames;    
    private int culpritIndex;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="npcs">A list of strings containing unique names of the AI players.</param>
    /// <param name="suspectIntelligenceLevel">Gets or sets the suspect's intelligence level. Allowed range [1-3]. This determines which model to use.
    /// Currently supported models: [1] gpt-5-nano, [2] gpt-5-mini, [3] gpt-5-nano.
    /// Default: gpt-5-nano </param>
    public ConversationManager(ICollection<NPCInteractable> npcs, int suspectIntelligenceLevel = 1)
    {
        if (npcs == null)
            throw new ArgumentNullException(nameof(npcs));

        playerNames = new List<NPCInteractable>(npcs);
        players = new List<MpcLlmController>();

        if (suspectIntelligenceLevel <= 1)
            playerModel = "gpt-5-nano-2025-08-07";
        else if (suspectIntelligenceLevel == 2)
            playerModel = "gpt-5-mini-2025-08-07";
        else
            playerModel = "gpt-5-2025-08-07";//check if this is true!
    }

    private List<MpcLlmController> players;
    public string DetectiveName { get; set; } = "Detective";
    public ReadOnlyCollection<MpcLlmController> Players => players.AsReadOnly();
    public MpcLlmController FindAgentByName(string name) => players.FirstOrDefault(p => string.Equals(p.Name, name));
    public MpcLlmController CurrentPlayer { get; private set; }
    public void SwitchPlayerTo(string name)
    {
        var player = FindAgentByName(name);
        if (player == null)
            throw new ArgumentException($"There is no player by the name: '{name}'", "name");
        CurrentPlayer = player;
    }

    public void SwitchPlayerTo(int playerIndex) => CurrentPlayer = players[playerIndex];
    
    /// <summary>
    /// Gets the chat history of the NPC agent specified by its name. Returns null if there is no NPC agent by that name.
    /// </summary>
    /// <param name="npcName">The unique name of the NPC agent.</param>
    /// <returns>A list of ChatHistoryItem objects. Null if there is no NPC by that name in this conversation manager.</returns>
    public List<ChatHistoryItem> GetHistoryByNpcName(string npcName)
    {
        var npcController = FindAgentByName(npcName);
        return npcController == null ? null : npcController.History;
    }

    public void Start()
    {
        string apiKey = LLMUtils.GetOpenAIApiKey();
        players = new List<MpcLlmController>();

        culpritIndex = new Random(DateTime.Now.Millisecond).Next(0, playerNames.Count - 1);

        for (int i = 0; i < playerNames.Count; i++)
        {
            NPCInteractable playerInfo = playerNames[i];
            string name = playerInfo.displayName;
            var otherPlayers = playerNames.Where(a => a.displayName != name).ToArray();
            var agent = new MpcLlmController(apiKey, playerModel, playerInfo);

            //Set the culprit flag.
            agent.Instructions = playerInfo.Prompt.Replace("{is_kidnapper}", (i == culpritIndex).ToString());
            players.Add(agent);
        }

        CurrentPlayer = players[0];
    }

    public string WhoIsCulprit() => players[culpritIndex].Name;

    public ChatResponse TalkToCurrentPlayer(string phrase) => CurrentPlayer.SendPrompt(DetectiveName, phrase);
}