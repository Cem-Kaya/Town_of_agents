using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public GameObject InventoryMenu;
    private bool menuActivated;
    public ItemSlot[] itemSlot;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetButtonDown("Inventory") && menuActivated)
        {
            InventoryMenu.SetActive(false);
            menuActivated = false;
        }
        else if (Input.GetButtonDown("Inventory") && !menuActivated)
        {
            InventoryMenu.SetActive(true);
            menuActivated = true;
        }
    }

    public void AddItem(Item item, Sprite itemSprite)
    {
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (!itemSlot[i].isFull)
            {
                itemSlot[i].AddItem(item, itemSprite);
                return;
            }
        }
    }

    public void DeselectAllSlots()
    {
        for (int i = 0; i < itemSlot.Length; i++)
        {
            itemSlot[i].selectedShader.SetActive(false);
            itemSlot[i].thisItemSelected = false;
        }
    }

    // === NEW: lookups ===
    public bool HasItem(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i].isFull && string.Equals(itemSlot[i].itemName, name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public bool HasAllItems(IList<string> names)
    {
        if (names == null || names.Count == 0) return true;
        for (int n = 0; n < names.Count; n++)
            if (!HasItem(names[n])) return false;
        return true;
    }
}
