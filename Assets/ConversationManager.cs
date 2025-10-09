using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class ConversationManager
{
    private readonly string playerModel;
    //private string model = "gpt-5-nano-2025-08-07";//"gpt-5-mini";
    private List<NPCInteractable> playerNames;

    public string CulpritInstructions { get; set; } =
@"You are a player in a detective game. Your name is {name}. 
    You kidnapped a chicken yesterday at 11:32PM and a detective is investigating. He is asking questions about the kidnapper.    
    There are {playerNr} other players in the game. Their names are {players}. 
    Your personality: {player_personality}
    These are your rules:
    - Do not expose that you killed the chicken.
    - Answer detective's questions to mislead and deceive him. 
    - Don't give any of your electronic devices to detective, if the logs in that device gives you away.";

    public string PlayerInstructions { get; set; } =
    @"You are a player in a detective game. Your name is {name}. A chicken was kidnapped yesterday at 11:32PM and
    a detective is investigating. He is asking questions about the kidnapper.
    There are {playerNr} other players in the game. Any of them can be the kidnapper. Their names are {players}. 
    Your personality: {player_personality}
    These are your rules:
    - Do not expose that you killed the chicken.
    - Answer detective's questions to mislead and deceive him. 
    - You may give your electronic devices to detective for verification, if he insists.";

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

        this.playerNames = new List<NPCInteractable>(npcs);
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

    public void Start()
    {
        string apiKey = LLMUtils.GetOpenAIApiKey();
        players = new List<MpcLlmController>();

        int culpritIndex = new Random(DateTime.Now.Millisecond).Next(0, playerNames.Count - 1);

        for (int i = 0; i < playerNames.Count; i++)
        {
            NPCInteractable playerInfo = playerNames[i];
            string name = playerInfo.displayName;
            var otherPlayers = playerNames.Where(a => a.displayName != name).ToArray();
            var agent = new MpcLlmController(apiKey, playerModel, name);

            //Set the LLM instructions (former system prompt) depending on player type (culprit or innocent).
            agent.Instructions = i == culpritIndex ? playerInfo.LLMPromptCulprit : playerInfo.LLMPromptRegular;
            agent.Instructions = agent.Instructions
            .Replace("{name}", name)
            .Replace("{playerNr}", otherPlayers.Length.ToString())
            .Replace("{players}", string.Join(",", otherPlayers.Select(a => a.displayName)));
            players.Add(agent);
        }

        CurrentPlayer = players[0];
    }

    public string TalkToCurrentPlayer(string phrase) => CurrentPlayer.SendPrompt(DetectiveName, phrase, false);
}