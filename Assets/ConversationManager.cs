using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class ConversationManager
{
    private readonly string playerModel;
    //private string model = "gpt-5-nano-2025-08-07";//"gpt-5-mini";
    private List<NPCInteractable> npcs;    
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

        this.npcs = new List<NPCInteractable>(npcs);
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
    
    private void LoadPrompts(string culpritName)
    {
        string folder = "/Users/m/Documents/masterAI_offline/AICG2025/town_of_agents_llm/TownOfAgentsLLMDev/StreamingAssets";
        var prompts = LLMUtils.LoadPrompts(folder);
        string townMemory = LLMUtils.LoadTownCollectiveMemory(folder);        
        string generalRules = LLMUtils.LoadGeneralRules(folder);      

        foreach (var npc in npcs)
        {
            bool isCulprit = culpritName == npc.displayName;

            PromptInfo prompt = prompts.Where(p => p.IsCulprit == isCulprit).FirstOrDefault(p => p.PlayerRole == npc.Occupation);
            if (prompt == null)
                throw new Exception($"Prompt file for NPC: {npc.displayName} with occupation: {npc.Occupation} and IsCulprit:{isCulprit} cannot be found.");

            prompt.SetName(npc.displayName);
            prompt.SetPossessions(npc.Posessions);
            prompt.SetTownCollectiveMemory(townMemory);
            prompt.SetDefaultRules(generalRules);
            npc.Prompt = prompt;
        }
    }

    public void Start()
    {
        string apiKey = LLMUtils.GetOpenAIApiKey();
        culpritIndex = new Random(DateTime.Now.Millisecond).Next(0, npcs.Count - 1);
        int mayorIndex = npcs.IndexOf(npcs.First(n => n.Occupation.ToLower() == "mayor"));
        while (culpritIndex == mayorIndex)
            culpritIndex = new Random(DateTime.Now.Millisecond).Next(0, npcs.Count - 1);

        string culpritName = npcs[culpritIndex].displayName;
        LoadPrompts(culpritName);
        players = new List<MpcLlmController>();        

        for (int i = 0; i < npcs.Count; i++)
        {
            NPCInteractable playerInfo = npcs[i];
            string name = playerInfo.displayName;
            var otherPlayers = npcs.Where(a => a.displayName != name).ToArray();
            var agent = new MpcLlmController(apiKey, playerModel, playerInfo);
            
            agent.Instructions = playerInfo.Prompt.PromptText;
            players.Add(agent);
        }

        CurrentPlayer = players[0];
    }

    public string WhoIsCulprit() => npcs.First(p=>p.Prompt.IsCulprit).displayName;

    public ChatResponse TalkToCurrentPlayer(string phrase) => CurrentPlayer.SendPrompt(DetectiveName, phrase);
}