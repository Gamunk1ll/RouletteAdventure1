using UnityEngine;

public class ShopWorldOffer : MonoBehaviour
{
    private Shop shop;
    private int slotIndex;

    public void Setup(Shop owner, int index)
    {
        shop = owner;
        slotIndex = index;
    }

    private void OnMouseDown()
    {
        if (shop == null)
            return;

        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.Shop)
            return;

        shop.TryBuy(slotIndex);
    }
}
