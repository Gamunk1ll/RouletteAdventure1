using System;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [Header("Товары")]
    public SectorData[] possibleItems;
    public bool uniqueOffers = true;

    [Header("Точки спавна товаров на сцене")]
    public Transform[] worldSpawnPoints;
    public Transform worldItemsParent;

    [Header("Настройки спавна товаров")]
    public float spawnHeightOffset = 0.3f;
    //  Поле itemRotation удалено — поворот больше не используется

    [Header("Настройки цен")]
    public int rerollPrice = 10;
    [Min(0)] public int rerollPriceGrowthPerWave = 2;
    [Min(0)] public int rerollPriceGrowthPerRoll = 3;
    [Min(0f)] public float wavePriceGrowth = 0.12f;

    [Header("Объекты только для фазы магазина")]
    public GameObject[] shopPhaseObjects;

    private readonly List<SectorData> currentOffers = new();
    private readonly List<GameObject> spawnedItems = new();
    private Player player;
    private int rerollsThisShop;

    public void Open()
    {
        player = GameManager.Instance?.player ?? FindObjectOfType<Player>();
        rerollsThisShop = 0;
        ToggleShopObjects(true);
        RollItems();
    }

    public void Close()
    {
        ClearSpawnedItems();
        ToggleShopObjects(false);
    }

    public void RollItems()
    {
        if (possibleItems == null || possibleItems.Length == 0) return;

        ClearSpawnedItems();
        currentOffers.Clear();

        int count = Mathf.Min(worldSpawnPoints.Length, possibleItems.Length);
        List<SectorData> pool = new List<SectorData>(possibleItems);
        Shuffle(pool);

        for (int i = 0; i < count; i++)
            currentOffers.Add(pool[i % pool.Count]);

        SpawnWorldItems();
    }

    public void Reroll()
    {
        if (player == null) return;
        int price = GetRerollPrice();
        if (player.GetMoney() < price) return;
        player.AddMoney(-price);
        rerollsThisShop++;
        RollItems();
    }

    public bool TryBuy(int offerIndex)
    {
        if (offerIndex < 0 || offerIndex >= currentOffers.Count) return false;

        SectorData item = currentOffers[offerIndex];
        if (item == null || player == null) return false;

        int price = GetBuyPrice(item);
        if (player.GetMoney() < price) return false;

        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null) return false;

        bool added;
        if (item.shopItemKind == ShopItemKind.Ball)
            added = inv.AddBall(item);
        else
            added = inv.AddSectorSilent(item, price);

        if (!added) return false;

        player.AddMoney(-price);
        currentOffers[offerIndex] = null;

        if (offerIndex < spawnedItems.Count && spawnedItems[offerIndex] != null)
        {
            Destroy(spawnedItems[offerIndex]);
            spawnedItems[offerIndex] = null;
        }

        return true;
    }

    private void SpawnWorldItems()
    {
        for (int i = 0; i < currentOffers.Count; i++)
        {
            spawnedItems.Add(null);

            SectorData item = currentOffers[i];
            if (item == null) continue;

            if (i >= worldSpawnPoints.Length || worldSpawnPoints[i] == null) continue;

            GameObject prefab = item.shopVisualPrefab != null
                ? item.shopVisualPrefab
                : item.visualPrefab;
            if (prefab == null) continue;

            // Создаём пустой контейнер
            GameObject obj = new GameObject($"ShopItem_{i}");

            // Сначала назначаем родителя (worldPositionStays = false)
            if (worldItemsParent != null)
                obj.transform.SetParent(worldItemsParent, false);

            // Выставляем только мировую позицию (поворот убран)
            obj.transform.position = worldSpawnPoints[i].position + Vector3.up * spawnHeightOffset;
            //  Строка obj.transform.rotation = Quaternion.Euler(itemRotation); удалена

            // Меш с оригинальным масштабом префаба
            GameObject visual = Instantiate(prefab, obj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = prefab.transform.localScale;

            // Коллайдер под масштаб префаба (~200 единиц)
            BoxCollider col = obj.AddComponent<BoxCollider>();
            col.size = Vector3.one * 200f;

            ShopWorldOffer offer = obj.AddComponent<ShopWorldOffer>();
            offer.Setup(this, i, item, GetBuyPrice(item));

            spawnedItems[i] = obj;
        }
    }

    private void ClearSpawnedItems()
    {
        foreach (var obj in spawnedItems)
            if (obj != null) Destroy(obj);
        spawnedItems.Clear();
    }

    public int GetBuyPrice(SectorData item)
    {
        if (item == null) return 0;
        int wave = Mathf.Max(1, GameManager.Instance?.currentWave ?? 1);
        float mult = 1f + (wave - 1) * wavePriceGrowth;
        return Mathf.Max(0, Mathf.RoundToInt(item.buyPrice * mult));
    }

    public int GetRerollPrice()
    {
        int wave = Mathf.Max(1, GameManager.Instance?.currentWave ?? 1);
        return rerollPrice
            + (wave - 1) * rerollPriceGrowthPerWave
            + rerollsThisShop * rerollPriceGrowthPerRoll;
    }

    private void ToggleShopObjects(bool active)
    {
        if (shopPhaseObjects == null) return;
        foreach (var obj in shopPhaseObjects)
            if (obj != null) obj.SetActive(active);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}