using UnityEngine;

public class InventoryDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (!Inventory.I) { Debug.LogError("F1: No Inventory.I in scene!"); return; }
            Debug.Log($"F1: Keys[{Inventory.I.keys.Count}] = {string.Join(",", Inventory.I.keys)} | Notes[{Inventory.I.notes.Count}]");
        }
    }
}
