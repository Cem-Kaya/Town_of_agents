using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCInteractable : MonoBehaviour
{
    public string displayName = "NPC";

    [TextArea(10,999)]
    public string LLMPromptRegular = @"You are a player in a detective game. Your name is {name}. A chicken was kidnapped yesterday at 11:32PM and
    a detective is investigating. He is asking questions about the kidnapper.
    There are {playerNr} other players in the game. Any of them can be the kidnapper. Their names are {players}. 
    Your personality: {player_personality}
    These are your rules:
    - Do not expose that you killed the chicken.
    - Answer detective's questions to mislead and deceive him. 
    - You may give your electronic devices to detective for verification, if he insists.";

    [TextArea(10,999)]
    public string LLMPromptCulprit = @"You are a player in a detective game. Your name is {name}. 
    You kidnapped a chicken yesterday at 11:32PM and a detective is investigating. He is asking questions about the kidnapper.    
    There are {playerNr} other players in the game. Their names are {players}. 
    Your personality: {player_personality}
    These are your rules:
    - Do not expose that you killed the chicken.
    - Answer detective's questions to mislead and deceive him. 
    - Don't give any of your electronic devices to detective, if the logs in that device gives you away.";
    
    public Grid grid;
    public NPCWalkerGrid npc;

    Collider2D trig;
    bool playerInRange;
    Player cachedPlayer;    

    void Awake()
    {
        trig = GetComponent<Collider2D>();
        trig.isTrigger = true;

        if (!grid) grid = Object.FindFirstObjectByType<Grid>();
        if (!npc) npc = GetComponent<NPCWalkerGrid>();

        // If it's a BoxCollider2D and size is tiny, default to 3x3 cells
        if (trig is BoxCollider2D box && box.size.sqrMagnitude < 0.01f && grid)
            box.size = new Vector2(grid.cellSize.x * 3f, grid.cellSize.y * 3f);
    }

    void Update()
    {
        if (playerInRange && !ChatUI.Instance.IsOpen)
        {
            ChatUI.Instance.ShowPrompt();
            if (Input.GetKeyDown(KeyCode.F))
                StartChat();
        }
        else
        {
            ChatUI.Instance.HidePrompt();
        }

        // If chat got closed while we're still in range, nothing else to do
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        cachedPlayer = other.GetComponent<Player>();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        ChatUI.Instance.HidePrompt();

        if (ChatUI.Instance.IsOpen) ChatUI.Instance.Close();
        cachedPlayer = null;
    }

    void StartChat()
    {
        if (!cachedPlayer) return;
        ChatUI.Instance.Open(this, cachedPlayer);

        // Optional greeting:
        ChatUI.Instance.AddNPCMessage($"Hello! I'm {displayName}.");
    }
}
