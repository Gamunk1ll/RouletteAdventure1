using UnityEngine;

public class ShopWorldOffer : MonoBehaviour
{
    private Shop shop;
    private int offerIndex;
    private SectorData sectorData;
    private int price;

    private Camera mainCamera;
    private bool isDragging = false;
    private GameObject dragVisual;
    private Vector3 originalPosition;
    private Renderer[] renderers;

    public void Setup(Shop owner, int index, SectorData sector, int buyPrice)
    {
        shop = owner;
        offerIndex = index;
        sectorData = sector;
        price = buyPrice;
        originalPosition = transform.position;
        mainCamera = Camera.main;
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance?.state != BattleState.Shop) return;

        isDragging = true;

        // Создаём копию для перетаскивания
        dragVisual = Instantiate(gameObject, transform.position, transform.rotation);
        Destroy(dragVisual.GetComponent<ShopWorldOffer>());
        foreach (var col in dragVisual.GetComponents<Collider>())
            Destroy(col);

        // Скрываем оригинал
        SetRenderersEnabled(false);
    }

    private void OnMouseDrag()
    {
        if (!isDragging || dragVisual == null || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, originalPosition.y, 0f));
        if (plane.Raycast(ray, out float dist))
            dragVisual.transform.position = ray.GetPoint(dist);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (dragVisual != null) { Destroy(dragVisual); dragVisual = null; }

        // Попали в слот инвентаря?
        InventorySlot invSlot = GetUnderMouse<InventorySlot>();
        if (invSlot != null && invSlot.CanAcceptSector())
        {
            bool bought = shop.TryBuy(offerIndex);
            if (bought)
            {
                RouletteInitializer roulette = FindObjectOfType<RouletteInitializer>();
                Material mat = roulette?.GetMaterialForType(sectorData.Type);
                int prefabIndex = FindPrefabIndex(roulette, sectorData);

                // Кладём в инвентарь — один слот, один предмет
                invSlot.PlaceSectorSimple(mat, Color.white, sectorData, prefabIndex);
                Destroy(gameObject);
                return;
            }
        }

        // Промахнулись или не хватило денег — возвращаем на место
        SetRenderersEnabled(true);
    }

    private void SetRenderersEnabled(bool on)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
            if (r != null) r.enabled = on;
    }

    private T GetUnderMouse<T>() where T : Component
    {
        if (mainCamera == null) return null;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            return hit.collider.GetComponent<T>();
        return null;
    }

    private static int FindPrefabIndex(RouletteInitializer roulette, SectorData data)
    {
        if (roulette == null || data == null) return -1;
        for (int i = 0; i < roulette.sectorPrefabs.Count; i++)
        {
            if (roulette.sectorPrefabs[i] == null) continue;
            var sector = roulette.sectorPrefabs[i].GetComponent<BaseSector>()
                      ?? roulette.sectorPrefabs[i].GetComponentInChildren<BaseSector>(true);
            if (sector?.data == data) return i;
        }
        return -1;
    }
}