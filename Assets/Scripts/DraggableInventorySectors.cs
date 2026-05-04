using UnityEngine;

public class DraggableInventorySector : MonoBehaviour
{
    public SectorData sectorData;
    public int storedPrefabIndex = -1;

    private int inventorySlotIndex;
    private InventorySlot parentSlot;
    private Camera mainCamera;
    private bool isDragging = false;
    private GameObject dragVisual;
    private RouletteInitializer roulette;

    public void Initialize(int index, SectorData data, InventorySlot slot, int prefabIndex)
    {
        inventorySlotIndex = index;
        sectorData = data;
        parentSlot = slot;
        storedPrefabIndex = prefabIndex;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        roulette = FindObjectOfType<RouletteInitializer>();
    }

    private void OnMouseDown()
    {
        if (sectorData == null) return;
        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.Shop) return;

        isDragging = true;

        // —крываем себ€ пока тащим Ч слот визуально освобождаетс€
        GetComponent<Renderer>().enabled = false;
        if (parentSlot != null)
            parentSlot.OnDragExit(); // слот возвращаетс€ к виду "пустого"

        dragVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dragVisual.transform.localScale = Vector3.one * 0.3f;
        Destroy(dragVisual.GetComponent<Collider>());

        Renderer rend = dragVisual.GetComponent<Renderer>();
        if (rend != null && sectorData != null && roulette != null)
            rend.material = roulette.GetMaterialForType(sectorData.Type);
    }

    private void OnMouseDrag()
    {
        if (!isDragging || dragVisual == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float dist))
            dragVisual.transform.position = ray.GetPoint(dist) + Vector3.up * 0.4f;
    }
    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (dragVisual != null) { Destroy(dragVisual); dragVisual = null; }

        RouletteSlotHandler rouletteSlot = GetUnderMouse<RouletteSlotHandler>();
        if (rouletteSlot != null && storedPrefabIndex >= 0 && roulette != null)
        {
            bool placed = roulette.PlaceWholeSector(rouletteSlot.slotIndex, storedPrefabIndex);
            if (placed)
            {
                // ”дал€ем из логики инвентар€
                if (PlayerInventory.Instance != null && sectorData != null)
                    PlayerInventory.Instance.RemoveSectorByData(sectorData);

                parentSlot.RemoveSector();
                return;
            }
        }

        InventorySlot otherInvSlot = GetUnderMouse<InventorySlot>();
        if (otherInvSlot != null && otherInvSlot != parentSlot && otherInvSlot.CanAcceptSector())
        {
            int myPrefab = storedPrefabIndex;
            SectorData myData = sectorData;
            Material mat = roulette?.GetMaterialForType(myData.Type);
            parentSlot.RemoveSector();
            otherInvSlot.PlaceSectorSimple(mat, Color.white, myData, myPrefab);
            return;
        }

        // ѕромахнулись Ч остаЄмс€
        GetComponent<Renderer>()?.gameObject.SetActive(true);
    }

    private T GetUnderMouse<T>() where T : Component
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            return hit.collider.GetComponent<T>();
        return null;
    }
}