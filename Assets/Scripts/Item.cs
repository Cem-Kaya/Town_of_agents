using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Item : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] public string itemName;
    [SerializeField] public bool isEvidence;
    [TextArea][SerializeField] public string itemDesc;
    [SerializeField] public Sprite sprite;

    [Header("Pickup Settings")]
    [Tooltip("Seconds before the item can be picked up.")]
    [SerializeField] private float pickupDelay = 5f;

    private bool canBePickedUp = false;
    private InventoryManager inventoryManager;

    void Start()
    {
        inventoryManager = GameObject.Find("InventoryCanvas").GetComponent<InventoryManager>();
        StartCoroutine(EnablePickupAfterDelay());
    }

    IEnumerator EnablePickupAfterDelay()
    {
        canBePickedUp = false;

        // optional: add a slight visual cue (dim the sprite while waiting)
        var sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            Color c = sr.color;
            c.a = 0.5f;
            sr.color = c;
        }

        yield return new WaitForSeconds(pickupDelay);

        canBePickedUp = true;

        // restore full opacity when it becomes pickable
        if (sr)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canBePickedUp) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (inventoryManager)
            inventoryManager.AddItem(this, sprite);

        Destroy(gameObject);
    }
}

