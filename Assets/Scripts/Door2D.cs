using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door2D : MonoBehaviour
{
    [Header("Access")]
    public string requiredKeyId = "red_key";
    public bool consumeKey = true;

    [Tooltip("If true, just bumping the door will try to open it when you have the key.")]
    public bool openOnTouch = true;

    [Tooltip("If > 0, destroy this GameObject after opening (seconds).")]
    public float destroyAfterOpen = 0f;

    [Header("UI Prompt (Press-E mode)")]
    public GameObject prompt; // optional world-space prompt; used only when openOnTouch = false

    [Header("Note text when key is used")]
    public string doorDisplayName = "Door";
    public string keyDisplayNameOverride = "";            // if empty, uses requiredKeyId
    [TextArea] public string noteBodyTemplate = "The {KEY} broke while opening the {DOOR}.";

    [Header("SFX")]
    public AudioClip openSfx;

    // --- internals ---
    Collider2D triggerCol;        // trigger on THIS object (for proximity / bump)
    Collider2D blockingCollider;  // non-trigger on THIS object (the thing that blocks movement)
    bool playerInside;

    void Awake()
    {
        // Find both colliders ON THIS GAMEOBJECT.
        foreach (var c in GetComponents<Collider2D>())
        {
            if (c.isTrigger) triggerCol = c;
            else blockingCollider = c;
        }

        if (!triggerCol)
            Debug.LogWarning($"Door2D '{name}': No trigger collider found. Add a second collider with IsTrigger=ON.", this);
        if (!blockingCollider)
            Debug.LogWarning($"Door2D '{name}': No blocking collider found. Add a non-trigger collider.", this);

        // ✅ no escaping needed inside interpolation:
        Debug.Log($"Door2D '{name}' setup: trigger={(triggerCol ? triggerCol.GetType().Name : "<none>")} " +
                  $"blocking={(blockingCollider ? blockingCollider.GetType().Name : "<none>")} " +
                  $"openOnTouch={openOnTouch}", this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        Debug.Log($"Door2D '{name}': OnTriggerEnter2D by {other.name}", this);

        if (openOnTouch) TryOpen();
        else UpdatePrompt(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        Debug.Log($"Door2D '{name}': OnTriggerExit2D by {other.name}", this);
        UpdatePrompt(false);
    }

    void Update()
    {
        if (!playerInside || openOnTouch) return;
        if (Input.GetKeyDown(KeyCode.E)) TryOpen();
    }

    void UpdatePrompt(bool show)
    {
        if (!prompt) return;
        bool canOpen = show && Inventory.I && Inventory.I.HasKey(requiredKeyId);
        prompt.SetActive(canOpen);
    }

    public void TryOpen()
    {
        bool hasKey = Inventory.I && Inventory.I.HasKey(requiredKeyId);
        Debug.Log($"Door2D '{name}': TryOpen() hasKey={hasKey} key='{requiredKeyId}'", this);

        if (!hasKey)
        {
            UpdatePrompt(true);
            return;
        }

        string keyName = string.IsNullOrWhiteSpace(keyDisplayNameOverride)
            ? requiredKeyId
            : keyDisplayNameOverride;

        if (consumeKey) Inventory.I.TryUseKey(requiredKeyId);

        string noteBody = noteBodyTemplate
            .Replace("{KEY}", keyName)
            .Replace("{DOOR}", doorDisplayName);

        Inventory.I.TryAddNote($"Used {keyName}", noteBody);

        if (blockingCollider) Destroy(blockingCollider);
        if (prompt) prompt.SetActive(false);
        if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position, 0.9f);

        Debug.Log($"Door2D '{name}': OPENED (key used: {keyName})", this);

        if (destroyAfterOpen > 0f) Destroy(gameObject, destroyAfterOpen);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // If this fires but OnTriggerEnter2D doesn't, your trigger isn’t being overlapped.
        Debug.LogWarning(
            $"Door2D '{name}': Collision with {collision.collider.name}. " +
            $"Ensure your TRIGGER collider is slightly larger/offset so the player can overlap it.",
            this);
    }
}
