using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public GameObject InventoryMenu;
    private bool menuActivated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
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

    public void AddItem(string itemName, string itemDesc, Sprite itemSprite)
    {
        Debug.Log("itemName = " + itemName + ", itemDesc = " + itemDesc + ", itemSprite = " + itemSprite);

    }

}
