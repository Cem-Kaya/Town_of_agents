// NotePickup.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NotePickup : MonoBehaviour
{
    public string title = "Note";
    [TextArea(2, 6)] public string body = "Some scribbles...";
    public AudioClip pickupSfx;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!Inventory.I) { Debug.LogError($"NotePickup '{name}': NO Inventory.I in scene."); return; }

        bool ok = Inventory.I.TryAddNote(title, body);
        Debug.Log($"NotePickup '{name}': TryAddNote => {ok}");
        if (ok)
        {
            if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, 0.8f);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"NotePickup '{name}': add failed (notes full?).");
        }
    }
}
