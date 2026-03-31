using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    [Serializable]
    public class ShopSlot
    {
        public Button buyButton;
        public TMP_Text titleText;
        public TMP_Text priceText;
        public Image iconImage;
        public Text legacyTitleText;
        public Text legacyPriceText;

        [HideInInspector] public SectorData assignedSector;
    }

    public SectorData[] possibleItems;
    public ShopSlot[] slots;

    [Min(1)] public int offersPerRoll = 6;
    public bool uniqueOffers = false;

    public TMP_Text moneyText;
    public TMP_Text hintText;
    public Button rerollButton;
    public int rerollPrice = 10;
    [Min(0)] public int rerollPriceGrowthPerWave = 2;
    [Min(0)] public int rerollPriceGrowthPerRoll = 3;
    [Min(0f)] public float wavePriceGrowth = 0.12f;
    [Min(0)] public int ballPurchaseAmount = 1;
    public Text legacyMoneyText;
    public Text legacyHintText;

    public Transform[] worldSpawnPoints;
    public Transform worldItemsParent;

    private readonly List<GameObject> spawnedWorldItems = new();
    private Player player;
    private int rerollsThisShopStage;

    private void Awake()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(RerollShop);
            rerollButton.onClick.AddListener(RerollShop);
        }

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            if (slots[i].buyButton == null) continue;
            slots[i].buyButton.onClick.RemoveAllListeners();
            slots[i].buyButton.onClick.AddListener(() => TryBuy(index));
        }
    }

    private void Update()
    {
        if (player == null)
        {
            if (GameManager.Instance != null)
                player = GameManager.Instance.player;
            else
                player = FindObjectOfType<Player>();
        }

        UpdateMoneyText();
        UpdateButtonsState();
    }

    public void Open()
    {
        if (player == null)
            player = GameManager.Instance != null ? GameManager.Instance.player : FindObjectOfType<Player>();

        rerollsThisShopStage = 0;
        RollItems();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        ClearWorldItems();
        gameObject.SetActive(false);
    }

    public void RollItems()
    {
        if (possibleItems == null || possibleItems.Length == 0)
            return;

        int activeOffers = Mathf.Min(offersPerRoll, slots.Length);

        bool useUniqueOffers = uniqueOffers || possibleItems.Length >= activeOffers;
        if (useUniqueOffers)
        {
            List<SectorData> pool = new List<SectorData>(possibleItems);
            Shuffle(pool);

            for (int i = 0; i < activeOffers; i++)
            {
                SectorData item = pool[i % pool.Count];
                BindItemToSlot(slots[i], item);
            }
        }
        else
        {
            for (int i = 0; i < activeOffers; i++)
            {
                SectorData item = possibleItems[UnityEngine.Random.Range(0, possibleItems.Length)];
                BindItemToSlot(slots[i], item);
            }
        }

        for (int i = activeOffers; i < slots.Length; i++)
            BindItemToSlot(slots[i], null);

        RespawnWorldItems();
        UpdateButtonsState();
    }

    public void RerollShop()
    {
        if (player == null)
            return;

        int currentRerollPrice = GetCurrentRerollPrice();
        if (player.GetMoney() < currentRerollPrice)
        {
            SetHint($"Not enough money to reroll ({currentRerollPrice}$)");
            return;
        }

        player.AddMoney(-currentRerollPrice);
        rerollsThisShopStage++;
        RollItems();
    }

    public void TryBuy(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        var slot = slots[slotIndex];
        if (slot.assignedSector == null || player == null)
            return;

        int price = GetBuyPrice(slot.assignedSector);
        if (player.GetMoney() < price)
        {
            SetHint($"Need {price}$");
            return;
        }

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            SetHint("PlayerInventory not found");
            return;
        }

        bool added;
        if (slot.assignedSector.shopItemKind == ShopItemKind.Ball)
        {
            added = inventory.AddBall(slot.assignedSector);
            BallManager ballManager = FindObjectOfType<BallManager>();
            if (added && ballManager != null)
                ballManager.AddBalls(Mathf.Max(1, ballPurchaseAmount));
        }
        else
        {
            added = inventory.AddSector(slot.assignedSector, price);
        }

        if (!added)
        {
            SetHint(slot.assignedSector.shopItemKind == ShopItemKind.Ball
                ? "Failed to add ball"
                : "Inventory is full. Sell sector/turret first");
            return;
        }

        player.AddMoney(-price);
        SetHint($"Bought: {slot.assignedSector.name}");

        BindItemToSlot(slot, null);
        ClearWorldItem(slotIndex);
        UpdateButtonsState();
    }

    private void BindItemToSlot(ShopSlot slot, SectorData item)
    {
        slot.assignedSector = item;

        if (item == null)
        {
            SetLabel(slot.titleText, slot.legacyTitleText, "SOLD");
            SetLabel(slot.priceText, slot.legacyPriceText, "-");
            if (slot.iconImage != null) slot.iconImage.sprite = null;
            return;
        }

        SetLabel(slot.titleText, slot.legacyTitleText, item.name);
        SetLabel(slot.priceText, slot.legacyPriceText, $"${GetBuyPrice(item)}");
        if (slot.iconImage != null) slot.iconImage.sprite = item.icon;
    }

    private void RespawnWorldItems()
    {
        ClearWorldItems();

        if (worldSpawnPoints == null || worldSpawnPoints.Length == 0)
            return;

        int activeOffers = Mathf.Min(offersPerRoll, slots.Length);

        for (int i = 0; i < activeOffers; i++)
        {
            spawnedWorldItems.Add(null);

            if (i >= worldSpawnPoints.Length)
                continue;

            Transform spawnPoint = worldSpawnPoints[i];
            if (spawnPoint == null || slots[i].assignedSector == null)
                continue;

            GameObject visualPrefab = slots[i].assignedSector.visualPrefab;
            if (visualPrefab == null)
                continue;

            Transform parent = worldItemsParent != null ? worldItemsParent : spawnPoint;
            GameObject itemView = Instantiate(visualPrefab, spawnPoint.position, spawnPoint.rotation, parent);

            if (itemView.GetComponent<Collider>() == null)
                itemView.AddComponent<BoxCollider>();

            ShopWorldOffer offer = itemView.GetComponent<ShopWorldOffer>();
            if (offer == null)
                offer = itemView.AddComponent<ShopWorldOffer>();

            offer.Setup(this, i, slots[i].assignedSector, GetBuyPrice(slots[i].assignedSector));
            spawnedWorldItems[i] = itemView;
        }
    }

    private void ClearWorldItems()
    {
        for (int i = 0; i < spawnedWorldItems.Count; i++)
        {
            if (spawnedWorldItems[i] != null)
                Destroy(spawnedWorldItems[i]);
        }

        spawnedWorldItems.Clear();
    }

    private void ClearWorldItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= spawnedWorldItems.Count)
            return;

        if (spawnedWorldItems[slotIndex] != null)
            Destroy(spawnedWorldItems[slotIndex]);

        spawnedWorldItems[slotIndex] = null;
    }

    private void UpdateButtonsState()
    {
        int currentMoney = player != null ? player.GetMoney() : 0;
        PlayerInventory inventory = PlayerInventory.Instance;

        int activeOffers = Mathf.Min(offersPerRoll, slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].buyButton == null)
                continue;

            bool hasItem = i < activeOffers && slots[i].assignedSector != null;
            bool enoughMoney = hasItem && currentMoney >= GetBuyPrice(slots[i].assignedSector);
            bool hasSlotSpace = true;

            if (hasItem &&
                slots[i].assignedSector.shopItemKind != ShopItemKind.Ball &&
                inventory != null)
            {
                hasSlotSpace = inventory.inventory.Count < inventory.MaxInventorySize;
            }

            bool canBuy = hasItem && enoughMoney && hasSlotSpace;

            slots[i].buyButton.interactable = canBuy;
        }

        if (rerollButton != null)
            rerollButton.interactable = player != null && currentMoney >= GetCurrentRerollPrice();
    }

    private void UpdateMoneyText()
    {
        int value = player != null ? player.GetMoney() : 0;
        string text = $"${value}";

        SetLabel(moneyText, legacyMoneyText, text);
    }

    private void SetHint(string text)
    {
        SetLabel(hintText, legacyHintText, text);
        Debug.Log($"[Shop] {text}");
    }

    private int GetBuyPrice(SectorData item)
    {
        if (item == null)
            return 0;

        int wave = 1;
        if (GameManager.Instance != null)
            wave = Mathf.Max(1, GameManager.Instance.currentWave);

        float multiplier = 1f + (wave - 1) * wavePriceGrowth;
        return Mathf.Max(0, Mathf.RoundToInt(item.buyPrice * multiplier));
    }

    private int GetCurrentRerollPrice()
    {
        int wave = 1;
        if (GameManager.Instance != null)
            wave = Mathf.Max(1, GameManager.Instance.currentWave);

        return rerollPrice + (wave - 1) * rerollPriceGrowthPerWave + rerollsThisShopStage * rerollPriceGrowthPerRoll;
    }

    private static void SetLabel(TMP_Text tmpText, Text legacyText, string value)
    {
        if (tmpText != null)
            tmpText.text = value;

        if (legacyText != null)
            legacyText.text = value;
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
