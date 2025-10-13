using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCInteractable : MonoBehaviour
{
    public string displayName = "NPC";

    [Header("LLM Configuration")]
    [TextArea(10, 999)]
    [Tooltip("Instructions to LLM service for this player. Applies when the player is not culprit.")]
    public string LLMPromptRegular = @"You are a player in a detective game. Your name is {name}. A chicken was kidnapped yesterday at 11:32PM and
    a detective is investigating. He is asking questions about the kidnapper.
    There are {playerNr} other players in the game. Any of them can be the kidnapper. Their names are {players}. 
    Your personality: {player_personality}
    These are your rules:
    - Do not expose that you killed the chicken.
    - Answer detective's questions to mislead and deceive him. 
    - You may give your electronic devices to detective for verification, if he insists.";

    [TextArea(10, 999)]
    [Tooltip("Instructions to LLM service for this player. Applies when the player is culprit.")]
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

        if (!grid) grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
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

    NPCInteractable[] otherPlayers;
    void StartChat()
    {
        if (!cachedPlayer) return;

        otherPlayers = FindObjectsByType<NPCInteractable>(FindObjectsSortMode.None);
        ChatUI.Instance.Open(this, cachedPlayer);

        // Optional greeting:
        ChatUI.Instance.AddNPCMessage($"Hello! I'm {displayName}.");
    }

    void MoveTo(string otherNpcName)
    {
        if (string.IsNullOrWhiteSpace(otherNpcName))
            throw new ArgumentException("The parameter 'player_name' is required.", nameof(otherNpcName));

        var other = otherPlayers?.FirstOrDefault(n => n.displayName == otherNpcName);
        if (other == null)
            throw new ArgumentException($"There is no player by that name: {otherNpcName}", nameof(otherNpcName));

        bool moving = true;
        Exception err = default;

        // Execute on main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            try
            {
                Vector3Int targetCell = grid.WorldToCell(other.transform.position);
                Vector3Int adjacentCell = new Vector3Int(targetCell.x - 1, targetCell.y, targetCell.z);
                npc.GoToCell(adjacentCell);

                ChatUI.Instance.HidePrompt();
                if (ChatUI.Instance.IsOpen)
                    ChatUI.Instance.Close();
                    
                moving = false;
            }
            catch (Exception ex)
            {
                err = ex;
                moving = false;
            }
        });

        // Wait for main thread execution to complete
        while (moving)
        {
            Thread.Sleep(1);
        }

        if (err != null)
            throw err;
    }

    private ActionResponse MoveToSafe(string callName, Dictionary<string, object> parameters)
    {
        ActionResponse response = new ActionResponse(callName);
        response.Parameters = parameters;

        if (parameters == null || !parameters.ContainsKey("player_name"))
        {
            response.IsSuccessful = false;
            response.Output = "The string valued parameter 'player_name' is required.";
            return response;
        }

        string otherNpcName = parameters["player_name"]?.ToString();
        if (otherNpcName == null)
        {
            response.IsSuccessful = false;
            response.Output = "The string valued parameter 'player_name' is required.";
            return response;
        }

        try
        {
            MoveTo(otherNpcName);
            response.IsSuccessful = true;
            response.Output = $"You are now standing next to {otherNpcName}";
        }
        catch (Exception ex)
        {
            response.IsSuccessful = false;
            response.Error = ex;
            response.Output = $"You cannot visit the '{otherNpcName}' at the moment because of a personal excuse.";
        }

        return response;
    }
    public ActionResponse PerformAction(string actionName, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(actionName))
            throw new ArgumentNullException(nameof(actionName));

        actionName = actionName.ToLower();
        string visitFuncName = "visit_other_player";

        ActionResponse response = new ActionResponse(actionName);

        bool supports = string.Equals(actionName, visitFuncName);
        if (!supports)
        {
            response.IsSuccessful = false;
            response.Error = new ArgumentException($"Unsupported action: '{actionName}'", nameof(actionName));
            response.Output = response.Error.Message;
            return response;
        }

        if (string.Equals(actionName, visitFuncName))
        {
            response = MoveToSafe(visitFuncName, parameters);
            LogActionResponse(response);
        }

        return response;
    }

    private void LogActionResponse(ActionResponse response, bool errorOnly = false)
    {
        if (errorOnly && response.IsSuccessful)
            return;

        Debug.Log(response.ToLogString(false));
    }
}
