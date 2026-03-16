using System;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    [Serializable]
    public class ShopSlot
    {
        public Button buyButton;
        public Text titleText;
        public Text priceText;
        public Image iconImage;

        [HideInInspector] public SectorData assignedSector;
    }

    [Header("Data")]
    public SectorData[] possibleItems;
    public ShopSlot[] slots;

    [Header("UI")]
    public Text moneyText;
    public Text hintText;
    public Button rerollButton;
    public int rerollPrice = 10;

    private Player player;

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

        RollItems();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void RollItems()
    {
        if (possibleItems == null || possibleItems.Length == 0)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            SectorData item = possibleItems[UnityEngine.Random.Range(0, possibleItems.Length)];
            BindItemToSlot(slots[i], item);
        }

        UpdateButtonsState();
    }

    public void RerollShop()
    {
        if (player == null)
            return;

        if (player.money < rerollPrice)
        {
            SetHint($"Not enough money to reroll ({rerollPrice}$)");
            return;
        }

        player.AddMoney(-rerollPrice);
        RollItems();
    }

    private void TryBuy(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        var slot = slots[slotIndex];
        if (slot.assignedSector == null || player == null)
            return;

        int price = Mathf.Max(0, slot.assignedSector.buyPrice);
        if (player.money < price)
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

        bool added = inventory.AddSector(slot.assignedSector);
        if (!added)
        {
            SetHint("Inventory is full");
            return;
        }

        player.AddMoney(-price);
        SetHint($"Bought: {slot.assignedSector.name}");

        BindItemToSlot(slot, null);
        UpdateButtonsState();
    }

    private void BindItemToSlot(ShopSlot slot, SectorData item)
    {
        slot.assignedSector = item;

        if (item == null)
        {
            if (slot.titleText != null) slot.titleText.text = "SOLD";
            if (slot.priceText != null) slot.priceText.text = "-";
            if (slot.iconImage != null) slot.iconImage.sprite = null;
            return;
        }

        if (slot.titleText != null) slot.titleText.text = item.name;
        if (slot.priceText != null) slot.priceText.text = $"${Mathf.Max(0, item.buyPrice)}";
        if (slot.iconImage != null) slot.iconImage.sprite = item.icon;
    }

    private void UpdateButtonsState()
    {
        int currentMoney = player != null ? player.money : 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].buyButton == null)
                continue;

            bool canBuy = slots[i].assignedSector != null && currentMoney >= Mathf.Max(0, slots[i].assignedSector.buyPrice);
            slots[i].buyButton.interactable = canBuy;
        }

        if (rerollButton != null)
            rerollButton.interactable = player != null && currentMoney >= rerollPrice;
    }

    private void UpdateMoneyText()
    {
        if (moneyText == null)
            return;

        int value = player != null ? player.money : 0;
        moneyText.text = $"${value}";
    }

    private void SetHint(string text)
    {
        if (hintText != null)
            hintText.text = text;

        Debug.Log($"[Shop] {text}");
    }
}
