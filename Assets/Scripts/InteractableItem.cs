using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractableItem : MonoBehaviour
{
    [Header("Who can interact")]
    public string playerTag = "Player";

    [Header("Requirements")]
    [Tooltip("All of these must be in the inventory to interact.")]
    public List<string> requiredItems = new List<string>();   // e.g. "Key", "Shovel"

    [Header("Prompt UI")]
    public GameObject promptRoot;          // e.g., a panel near the bottom of screen
    public TMP_Text promptText;            // text component inside promptRoot
    [TextArea]
    public string promptWhenOK = "Press F to interact";
    [TextArea]
    public string promptWhenMissingTemplate = "Missing: {items}";

    [Header("Actions")]
    public UnityEvent onInteract;          // hook door open, cutscene, etc.
    public UnityEvent onInteractMissing;   // optional feedback (error sfx, shake)

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    bool playerInRange;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable() => HidePrompt();
    void OnDisable() => HidePrompt();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = true;
        UpdatePrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = false;
        HidePrompt();
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (RequirementsMet())
            {
                onInteract?.Invoke();
                // optional: HidePrompt(); // if you want it to disappear after success
            }
            else
            {
                onInteractMissing?.Invoke();
                // refresh message (in case inventory changed)
                UpdatePrompt();
            }
        }
    }

    bool RequirementsMet()
    {
        if (InventoryManager.Instance == null) return false;
        return InventoryManager.Instance.HasAllItems(requiredItems);
    }

    void UpdatePrompt()
    {
        if (!promptRoot || !promptText) return;

        if (RequirementsMet())
        {
            promptText.text = promptWhenOK;
        }
        else
        {
            // build "Missing: Key, Rope"
            var sb = new StringBuilder();
            var first = true;
            foreach (var req in requiredItems)
            {
                if (string.IsNullOrWhiteSpace(req)) continue;
                if (!InventoryManager.Instance.HasItem(req))
                {
                    if (!first) sb.Append(", ");
                    sb.Append(req);
                    first = false;
                }
            }
            var missing = sb.Length == 0 ? "…" : sb.ToString();
            promptText.text = promptWhenMissingTemplate.Replace("{items}", missing);
        }

        promptRoot.SetActive(true);
    }

    void HidePrompt()
    {
        if (promptRoot) promptRoot.SetActive(false);
    }
}
