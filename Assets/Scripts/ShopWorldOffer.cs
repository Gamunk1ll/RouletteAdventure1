using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopWorldOffer : MonoBehaviour
{
    [Header("Optional 3D Labels")]
    public TMP_Text titleText;
    public TMP_Text priceText;

    [Header("Legacy Labels")]
    public Text legacyTitleText;
    public Text legacyPriceText;

    private Shop shop;
    private int slotIndex;
    private int displayedPrice;

    public void Setup(Shop owner, int index, SectorData sector, int price)
    {
        shop = owner;
        slotIndex = index;
        displayedPrice = Mathf.Max(0, price);
        ApplyLabels(sector);
    }

    private void OnMouseDown()
    {
        if (shop == null)
            return;

        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.Shop)
            return;

        shop.TryBuy(slotIndex);
    }

    private void ApplyLabels(SectorData sector)
    {
        if (sector == null)
            return;

        if (titleText == null || priceText == null)
            TryAutoAssignTmpLabels();

        string title = sector.name;
        string price = $"${displayedPrice}";

        if (titleText != null)
            titleText.text = title;

        if (priceText != null)
            priceText.text = price;

        if (legacyTitleText != null)
            legacyTitleText.text = title;

        if (legacyPriceText != null)
            legacyPriceText.text = price;
    }

    private void TryAutoAssignTmpLabels()
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        if (labels.Length == 1)
        {
            priceText = labels[0];
            return;
        }

        if (labels.Length >= 2)
        {
            titleText = labels[0];
            priceText = labels[1];
        }
    }
}
