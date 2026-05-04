using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        public SectorData sector;
        public int boughtPrice;
    }

    public static PlayerInventory Instance;

    [Header("Логика инвентаря")]
    public List<InventoryItem> inventory = new List<InventoryItem>();
    public List<SectorData> ownedBalls = new List<SectorData>();
    public int MaxInventorySize = 5;

    [Header("Настройки продажи")]
    [Range(0f, 1f)] public float sellMultiplier = 0.75f;

    [Header("3D Ссылки")]
    public Transform inventoryParent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Полное добавление с визуалом
    public bool AddSector(SectorData sector, int boughtPrice)
    {
        if (!CanAdd(sector)) return false;
        inventory.Add(new InventoryItem { sector = sector, boughtPrice = Mathf.Max(0, boughtPrice) });
        SyncVisuals();
        return true;
    }

    // Добавление только в список БЕЗ визуала
    // Используется когда визуал уже создан через PlaceSectorSimple (покупка из магазина)
    public bool AddSectorSilent(SectorData sector, int boughtPrice)
    {
        if (!CanAdd(sector)) return false;
        inventory.Add(new InventoryItem { sector = sector, boughtPrice = Mathf.Max(0, boughtPrice) });
        // Не вызываем SyncVisuals — визуал уже создан снаружи
        return true;
    }

    private bool CanAdd(SectorData sector)
    {
        if (sector == null || sector.shopItemKind == ShopItemKind.Ball) return false;
        if (inventory.Count >= MaxInventorySize)
        {
            Debug.Log("Inventory full");
            return false;
        }
        return true;
    }

    public bool AddBall(SectorData ballItem)
    {
        if (ballItem == null || ballItem.shopItemKind != ShopItemKind.Ball) return false;
        ownedBalls.Add(ballItem);
        return true;
    }

    public void RemoveSector(int index)
    {
        if (index >= 0 && index < inventory.Count)
        {
            inventory.RemoveAt(index);
            SyncVisuals();
        }
    }

    // Удаляет по SectorData — используется при переносе из инвентаря на рулетку
    public void RemoveSectorByData(SectorData sector)
    {
        int index = inventory.FindIndex(item => item.sector == sector);
        if (index >= 0)
        {
            inventory.RemoveAt(index);
            // Не вызываем SyncVisuals — визуал уже удалён через RemoveSector на слоте
        }
    }

    public SectorData GetSector(int index)
    {
        return (index >= 0 && index < inventory.Count) ? inventory[index].sector : null;
    }

    public int GetSellPrice(int index)
    {
        if (index < 0 || index >= inventory.Count) return 0;
        int baseSell = Mathf.RoundToInt(inventory[index].boughtPrice * sellMultiplier);
        if (baseSell <= 0 && inventory[index].sector != null)
            baseSell = Mathf.Max(0, inventory[index].sector.sellPrice);
        return baseSell;
    }

    public int SellSector(int index)
    {
        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.Shop) return 0;
        if (index < 0 || index >= inventory.Count) return 0;

        int price = GetSellPrice(index);
        inventory.RemoveAt(index);
        SyncVisuals();

        Player player = FindObjectOfType<Player>();
        player?.AddMoney(price);
        return price;
    }

    public void ClearInventory()
    {
        inventory.Clear();
        ownedBalls.Clear();
        SyncVisuals();
    }

    // Возвращает количество занятых слотов (логически)
    public int GetUsedSlotCount() => inventory.Count;

    public bool IsFull() => inventory.Count >= MaxInventorySize;

    private void SyncVisuals()
    {
        if (inventoryParent == null) return;

        InventorySlot[] slots = inventoryParent.GetComponentsInChildren<InventorySlot>();
        System.Array.Sort(slots, (x, y) => x.slotIndex.CompareTo(y.slotIndex));

        RouletteInitializer roulette = FindObjectOfType<RouletteInitializer>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= inventory.Count)
            {
                if (slots[i].isOccupied) slots[i].RemoveSector();
                continue;
            }

            var item = inventory[i];
            if (slots[i].isOccupied && slots[i].sectorData == item.sector)
                continue;

            if (slots[i].isOccupied)
                slots[i].RemoveSector();

            if (item.sector != null)
            {
                Material mat = roulette?.GetMaterialForType(item.sector.Type);
                slots[i].PlaceSectorSimple(mat, Color.white, item.sector, -1);
            }
        }
    }
}