using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PimDeWitte.UnityMainThreadDispatcher;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCInteractable : MonoBehaviour
{
    [Header("UI / Dialogue")]
    public Sprite portrait;   // drag a headshot here (optional)

    [Header("LLM Configuration")]
    [Tooltip("NPC's display name in the game.")]
    public string displayName = "NPC";

    [Tooltip("Unique role per NPC. Used to match the NPC prompt.")]
    public string Occupation = "Farmer";

    [Tooltip("NPC's possessions in the game.")]
    public string Posessions = "shovel, ledger, dirty boots, smartphone, laptop";

    public PromptInfo Prompt { get; set; }

    [Header("Movement / Grid")]
    public Grid grid;
    public NPCWalkerGrid npc;

    private Collider2D trig;
    private bool playerInRange;
    private Player cachedPlayer;

    // Track if *this* NPC opened the chat, so we only auto-close our own session on exit.
    private bool openedChatThisSession;

    public string GetOccupation() => Occupation.Trim().ToLower();

    void Awake()
    {
        trig = GetComponent<Collider2D>();
        trig.isTrigger = true;

        if (!grid) grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
        if (!npc) npc = GetComponent<NPCWalkerGrid>();

        // If it's a BoxCollider2D and tiny, expand to ~3x3 cells
        if (trig is BoxCollider2D box && box.size.sqrMagnitude < 0.01f && grid)
            box.size = new Vector2(grid.cellSize.x * 3f, grid.cellSize.y * 3f);
    }

    void OnDisable()
    {
        // Safety: clear range state and hide prompt if we get disabled while the player is inside
        playerInRange = false;
        var ui = ChatUIController.Instance;
        if (ui != null) ui.HidePrompt();
        openedChatThisSession = false;
        cachedPlayer = null;
    }

    void Update()
    {
        var ui = ChatUIController.Instance;

        if (playerInRange && ui != null && !ui.IsOpen)
        {
            ui.ShowPrompt();

            if (Input.GetKeyDown(KeyCode.F))
                StartChat(ui);
        }
        else
        {
            // No-op; exit hides prompt in OnTriggerExit2D.
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        cachedPlayer = other.GetComponent<Player>();

        var ui = ChatUIController.Instance;
        if (ui != null && !ui.IsOpen)
            ui.ShowPrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        var ui = ChatUIController.Instance;

        if (ui != null)
        {
            ui.HidePrompt();

            // Only close automatically if this NPC opened the chat.
            if (openedChatThisSession && ui.IsOpen)
                ui.Close();
        }

        openedChatThisSession = false;
        cachedPlayer = null;
    }

    private void StartChat(ChatUIController ui)
    {
        if (ui == null) return;
        if (cachedPlayer == null) return;

        ui.HidePrompt();
        ui.Open(this, cachedPlayer);
        openedChatThisSession = true;

        // Optional greeting to prove UI path works even if LLM is down
        ui.AddNPCMessageBubble("Hi there!");
    }

    // ===== Optional NPC->NPC movement helpers =====

    private NPCInteractable[] otherPlayers;

    private NPCInteractable FindOtherNpc(string nameOrOccupation)
    {
        nameOrOccupation = (nameOrOccupation ?? string.Empty).ToLower().Trim();
        if (otherPlayers == null || otherPlayers.Length == 0)
            otherPlayers = FindObjectsByType<NPCInteractable>(FindObjectsSortMode.None);

        foreach (var n in otherPlayers)
        {
            if (n == null) continue;
            // FIX: use Contains (capital C)
            if (nameOrOccupation.Contains(n.displayName.ToLower().Trim()) ||
                nameOrOccupation.Contains(n.Occupation.ToLower().Trim()))
            {
                return n;
            }
        }
        return null;
    }

    private void MoveTo(string otherNpcName)
    {
        if (string.IsNullOrWhiteSpace(otherNpcName))
            throw new ArgumentException("The parameter 'npc_name' is required.", nameof(otherNpcName));

        var other = FindOtherNpc(otherNpcName);
        if (other == null)
            throw new ArgumentException($"There is no player by that name: {otherNpcName}", nameof(otherNpcName));

        if (grid == null || npc == null)
            throw new InvalidOperationException("Grid or NPCWalkerGrid is not assigned.");

        bool moving = true;
        Exception err = null;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            try
            {
                Vector3Int targetCell = grid.WorldToCell(other.transform.position);
                Vector3Int adjacentCell = new Vector3Int(targetCell.x - 1, targetCell.y, targetCell.z);
                npc.GoToCell(adjacentCell);

                var ui = ChatUIController.Instance;
                if (ui != null)
                {
                    if (openedChatThisSession && ui.IsOpen)
                        ui.Close();
                }

                moving = false;
            }
            catch (Exception ex)
            {
                err = ex;
                moving = false;
            }
        });

        while (moving) Thread.Sleep(1);

        if (err != null) throw err;
    }

    // ===== LLM action wrappers =====
    private static bool ContainsEither(string a, string b) =>
        !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
        (a.Contains(b, StringComparison.OrdinalIgnoreCase) ||
        b.Contains(a, StringComparison.OrdinalIgnoreCase));

    private void UpdateInventoryAfterHandover(string itemName)
    {
        bool working = true;
        Exception err = null;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            try
            {
                //Check if there is such an object in the game.        
                Item[] currentItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                //If there is such an object, add it to the inventory of the player, if not already there.
                var item = currentItems.FirstOrDefault(i => ContainsEither(itemName, i.itemName));
                bool isAbsent = !InventoryManager.Instance.itemSlot.Any(i => i.itemName == item.itemName && i.isFull);
                if (isAbsent)
                {
                    InventoryManager.Instance.AddItem(item, item.sprite);
                    Debug.Log($"YOU ACQUIRED: {item.itemName}");
                }

                working = false;
            }
            catch (Exception ex)
            {
                err = ex;
                working = false;
            }
        });

        while (working) Thread.Sleep(1);
        if (err != null) throw err;
    }

    private ActionResponse HandoverSafe(string callName, Dictionary<string, object> parameters)
    {
        var response = new ActionResponse(callName) { Parameters = parameters };

        string itemName = LLMTools.TryGetValueAsString(parameters, "item_name");
        if (string.IsNullOrWhiteSpace(itemName))
        {
            response.IsSuccessful = false;
            response.Output = "The string valued parameter 'item_name' is required.";
            return response;
        }

        try
        {
            UpdateInventoryAfterHandover(itemName);
            Debug.Log($"YOU ACQUITRED {itemName}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unable to add given item to inventory: {ex.Message}");
        }

        response.IsSuccessful = true;
        response.Output = $"{displayName} handed over the {itemName}";
        return response;
    }

    private ActionResponse RejectHandoverSafe(string callName, Dictionary<string, object> parameters)
    {
        var response = new ActionResponse(callName) { Parameters = parameters };

        string itemName = LLMTools.TryGetValueAsString(parameters, "item_name");
        if (string.IsNullOrWhiteSpace(itemName))
        {
            response.IsSuccessful = false;
            response.Output = "The string valued parameter 'item_name' is required.";
            return response;
        }
        string reason = LLMTools.TryGetValueAsString(parameters, "reason");
        if (string.IsNullOrWhiteSpace(reason))
        {
            response.IsSuccessful = false;
            response.Output = "The string valued parameter 'reason' is required.";
            return response;
        }

        response.IsSuccessful = true;
        response.Output = $"{displayName} refused to hand over the {itemName}. Reason: {reason}";
        return response;
    }

    private ActionResponse MoveToSafe(string callName, Dictionary<string, object> parameters)
    {
        var response = new ActionResponse(callName) { Parameters = parameters };

        string otherNpcName = LLMTools.TryGetValueAsString(parameters, "npc_name");
        if (string.IsNullOrWhiteSpace(otherNpcName))
        {
            response.IsSuccessful = false;
            response.Output = "The string valued parameter 'npc_name' is required.";
            return response;
        }

        try
        {
            MoveTo(otherNpcName);
            response.IsSuccessful = true;
            response.Output = $"You are now standing next to {otherNpcName}. Don't talk to him yet.";
        }
        catch (Exception ex)
        {
            response.IsSuccessful = false;
            response.Error = ex;
            response.Output = $"You cannot visit '{otherNpcName}' right now.";
        }

        return response;
    }

    private void Arrest(string suspectNpcName)
    {
        if (string.IsNullOrWhiteSpace(suspectNpcName))
            throw new ArgumentException("The parameter 'suspectNpcName' is required.", nameof(suspectNpcName));

        var other = FindOtherNpc(suspectNpcName);
        if (other == null)
            throw new ArgumentException($"There is no player by that name: {suspectNpcName}", nameof(suspectNpcName));

        bool isCinematicRunning = true;
        Exception err = null;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            try
            {
                //@Cem-Kaya TODO: Launch the cinematic.
                Debug.Log("FINAL CINEMATIC TRIGGER");
                isCinematicRunning = false;
            }
            catch (Exception ex)
            {
                err = ex;
                isCinematicRunning = false;
            }
        });

        while (isCinematicRunning) Thread.Sleep(1);

        if (err != null) throw err;
    }

    private ActionResponse ArrestSafe(string callName, Dictionary<string, object> parameters)
    {
        var response = new ActionResponse(callName) { Parameters = parameters };

        string otherNpcName = LLMTools.TryGetValueAsString(parameters, "suspect_npc_name");
        if (string.IsNullOrWhiteSpace(otherNpcName))
        {
            response.IsSuccessful = false;
            response.Output = "The string valued parameter 'suspect_npc_name' is required.";
            return response;
        }

        try
        {
            //Go to NPC, if you do not want to navigate, just comment out the line below.
            MoveTo(otherNpcName);
            Arrest(otherNpcName);
            response.IsSuccessful = true;
            response.Output = $"You are now standing next to {otherNpcName} to arrest him. Don't talk to him yet.";
        }
        catch (Exception ex)
        {
            response.IsSuccessful = false;
            response.Error = ex;
            response.Output = $"You cannot arrest '{otherNpcName}' right now.";
        }

        return response;
    }

    public ActionResponse PerformAction(string actionName, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(actionName))
            throw new ArgumentNullException(nameof(actionName));

        string goToFuncName = "go_to_npc";
        string handoverFunctionName = "handover_item_to_detective";
        string refuseHandoverFunctionName = "refuse_handover_item_to_detective";
        string arrestFunctionName = "arrest_suspect";

        actionName = actionName.ToLowerInvariant();

        var response = new ActionResponse(actionName);
        bool supported =
            actionName == goToFuncName ||
            actionName == handoverFunctionName ||
            actionName == refuseHandoverFunctionName;

        if (!supported)
        {
            response.IsSuccessful = false;
            response.Error = new ArgumentException($"Unsupported action: '{actionName}'", nameof(actionName));
            response.Output = response.Error.Message;
            return response;
        }

        if (actionName == goToFuncName)
            response = MoveToSafe(goToFuncName, parameters);
        else if (actionName == handoverFunctionName)
            response = HandoverSafe(handoverFunctionName, parameters);
        else if (actionName == refuseHandoverFunctionName)
            response = RejectHandoverSafe(refuseHandoverFunctionName, parameters);
        else if (actionName == arrestFunctionName)
            response = ArrestSafe(arrestFunctionName, parameters);

        LogActionResponse(response);
        return response;
    }

    private void LogActionResponse(ActionResponse response, bool errorOnly = false)
    {
        if (errorOnly && response.IsSuccessful) return;
        Debug.Log(response.ToLogString(false));
    }
}
