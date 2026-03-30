using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        public SectorData sector;
        public int boughtPrice;
    }

    public static PlayerInventory Instance;
    public List<InventoryItem> inventory = new List<InventoryItem>();
    public List<SectorData> ownedBalls = new List<SectorData>();
    public int MaxInventorySize = 4;
    public Transform inventoryContainer;
    public GameObject inventorySlotPrefab;
    [Range(0f, 1f)] public float sellMultiplier = 0.75f;

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
    public bool AddSector(SectorData sector, int boughtPrice) 
    {
        if (sector == null)
            return false;

        if (sector.shopItemKind == ShopItemKind.Ball)
            return false;

        if (inventory.Count >= MaxInventorySize) 
        {
            Debug.Log("inventory full");
            return false;
        }

        inventory.Add(new InventoryItem
        {
            sector = sector,
            boughtPrice = Mathf.Max(0, boughtPrice)
        });
        UpdateInventoryUI();
        return true;
    }
    public bool AddBall(SectorData ballItem)
    {
        if (ballItem == null || ballItem.shopItemKind != ShopItemKind.Ball)
            return false;

        ownedBalls.Add(ballItem);
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
            return inventory[index].sector;
        return null;
    }
    public int GetSellPrice(int index)
    {
        if (index < 0 || index >= inventory.Count)
            return 0;

        int baseSell = Mathf.RoundToInt(inventory[index].boughtPrice * sellMultiplier);
        if (baseSell <= 0 && inventory[index].sector != null)
            baseSell = Mathf.Max(0, inventory[index].sector.sellPrice);

        return baseSell;
    }
    public int SellSector(int index) 
    {
        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.Shop)
            return 0;

        if (index >= 0 && index < inventory.Count) 
        {
            int price = GetSellPrice(index);
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
            if (image !=null && inventory[i].sector != null && inventory[i].sector.icon !=null)
            {
                image.sprite = inventory[i].sector.icon;
            }
            Text priceText = slot.GetComponentInChildren<Text>();
            if (priceText != null) 
            {
                priceText.text = $"${GetSellPrice(i)}";
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
        if (GameManager.Instance != null && GameManager.Instance.state == BattleState.Shop)
        {
            SellSector(index);
            return;
        }

        SectorSelector selector = FindObjectOfType<SectorSelector>();
        if (selector != null)
        {
            selector.SelectFromInventory(index);
        }
    }
    public void ClearInventory()
    {
        inventory.Clear();
        ownedBalls.Clear();
        UpdateInventoryUI();
    }
}
