// KeyPickup.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour
{
    public string keyId = "red_key";
    public string displayName = "Red Key";
    public AudioClip pickupSfx;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!Inventory.I) { Debug.LogError($"KeyPickup '{name}': NO Inventory.I in scene."); return; }

        bool ok = Inventory.I.TryAddKey(keyId);
        Debug.Log($"KeyPickup '{name}': TryAddKey({keyId}) => {ok}");
        if (ok)
        {
            Inventory.I.TryAddNote($"Found {displayName}", $"You picked up the {displayName}.");
            if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, 0.8f);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"KeyPickup '{name}': add failed (duplicate? inventory full?).");
        }
    }
}
