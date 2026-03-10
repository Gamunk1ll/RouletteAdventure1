using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;
    public List<SectorData> inventory = new List<SectorData> ();
    public int MaxInventorySize = 4;
    public Transform inventoryContainer;
    public GameObject inventorySlotPrefab;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy (gameObject);
    }
    void Start()
    {
        UpdateInventoryUI();    
    }
    public bool AddSector(SectorData sector) 
    {
        if (inventory.Count >= MaxInventorySize) 
        {
            Debug.Log("inventory full");
            return false;
        }
        inventory.Add(sector);
        UpdateInventoryUI();
        return true;
    }
    public void RemoveSector(int index) 
    {
        if (index >= 0 && index < inventory.Count) 
        {
            inventory.RemoveAt(index);
            UpdateInventoryUI();
        }
    }
    public SectorData GetSector(int index) 
    {
        if(index >= 0 && index < inventory.Count)
            return inventory[index];
        return null;
    }
    public int SellSector(int index) 
    {
        if (index >= 0 && index < inventory.Count) 
        {
            SectorData sector = inventory[index];
            int price = sector.sellPrice;
            inventory.RemoveAt(index);
            UpdateInventoryUI();
            Player player = FindObjectOfType<Player>();
            if (player!=null)
            {
                player.AddMoney(price);
            }
            return price;
        }
        return 0;
    }
    public void UpdateInventoryUI() 
    {
        if (inventoryContainer == null)
            return;
        foreach(Transform child in inventoryContainer) 
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < inventory.Count; i++) 
        {
            GameObject slot = Instantiate(inventorySlotPrefab, inventoryContainer);
            Image image = slot.GetComponent<Image>();
            if (image !=null && inventory[i].icon !=null)
            {
                image.sprite = inventory[i].icon;
            }
            Text priceText = slot.GetComponentInChildren<Text>();
            if (priceText != null) 
            {
                priceText.text = $"${inventory[i].sellPrice}";
            }
            Button button = slot.GetComponent<Button>();
            if(button != null) 
            {
                int index = i;
                button.onClick.AddListener(() =>OnInventorySlotClick(index));
            }
        }
    }
    void OnInventorySlotClick(int index) 
    {
        SectorSelector selector = FindObjectOfType<SectorSelector>();
        if (selector != null)
        {
            selector.SelectFromInventory(index);
        }
    }
    public void ClearInventory()
    {
        inventory.Clear();
        UpdateInventoryUI();
    }
}
