using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField]
    private string itemName;

    [TextArea]
    [SerializeField]
    private string itemDesc;

    [SerializeField]
    private Sprite sprite; //item image

    private InventoryManager inventoryManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       inventoryManager = GameObject.Find("InventoryCanvas").GetComponent<InventoryManager>();

    }

    private void OnCollisionEnter2D(Collision2D collision) 
    { 
        if (collision.gameObject.tag == "Player")
        {
            inventoryManager.AddItem(itemName, itemDesc, sprite);
            Destroy(gameObject);
        }
    
    }
}
