using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    // ITEM DATA
    public string itemName;
    public string itemDesc;
    public Sprite itemSprite;
    public bool isFull;
    //public Sprite emptySprite;

    //ITEM SLOT
    [SerializeField]
    private Image itemImage;

    public GameObject selectedShader;
    public bool thisItemSelected;

    [SerializeField]  private InventoryManager inventoryManager;


    //ITEM DESCRIPTION SLOT
    public Image itemDescriptionImage;
    public TMP_Text ItemDescriptionNameText;
    public TMP_Text ItemDescriptionDescText;



    private void Start()
    {
        if (!inventoryManager)
            inventoryManager = FindFirstObjectByType<InventoryManager>();
    }


    public void AddItem(string itemName, string itemDesc, Sprite itemSprite)
    {
        this.itemName = itemName;
        this.itemDesc = itemDesc;
        this.itemSprite = itemSprite;
        isFull = true;

        if (itemImage)
        {
            itemImage.sprite = itemSprite;
            itemImage.enabled = (itemSprite != null);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightlick();
        }
    }
    public void OnLeftClick()
    {
        inventoryManager.DeselectAllSlots();
        selectedShader.SetActive(true);
        thisItemSelected = true;
        ItemDescriptionNameText.text = itemName;
        ItemDescriptionDescText.text = itemDesc;
        itemDescriptionImage.sprite = itemSprite;
        if(itemDescriptionImage.sprite == null)
        {
            itemDescriptionImage.enabled = false;
        }
        else
        {
            itemDescriptionImage.enabled = true;
        }
    }
    public void OnRightlick()
    {

    }
    public void Clear()
    {
        itemName = "";
        itemDesc = "";
        itemSprite = null;
        isFull = false;

        if (itemImage)
        {
            itemImage.sprite = null;
            itemImage.enabled = false;
        }

        if (selectedShader) selectedShader.SetActive(false);
        thisItemSelected = false;

        // optional: also clear description panel if this slot is currently selected
        if (ItemDescriptionNameText && thisItemSelected) ItemDescriptionNameText.text = "";
        if (ItemDescriptionDescText && thisItemSelected) ItemDescriptionDescText.text = "";
        if (itemDescriptionImage && thisItemSelected) itemDescriptionImage.enabled = false;
    }

    private void SetDescriptionUI()
    {
        if (ItemDescriptionNameText) ItemDescriptionNameText.text = itemName;
        if (ItemDescriptionDescText) ItemDescriptionDescText.text = itemDesc;

        if (itemDescriptionImage)
        {
            itemDescriptionImage.sprite = itemSprite;
            itemDescriptionImage.enabled = (itemSprite != null);
        }
    }
}
