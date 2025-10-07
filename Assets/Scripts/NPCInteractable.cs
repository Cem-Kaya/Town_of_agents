using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCInteractable : MonoBehaviour
{
    public string displayName = "NPC";
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
