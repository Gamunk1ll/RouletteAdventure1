using UnityEngine;

public class RouletteSlotHandler : MonoBehaviour
{
    public int slotIndex;
    public RouletteInitializer initializer;

    private Camera mainCamera;
    private bool isDragging = false;
    private GameObject dragVisual;
    private int sectorStartSlot;
    private int sectorEndSlot;
    private int draggedPrefabIndex = -1; // запоминаем что тащим

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance == null || GameManager.Instance.state != BattleState.Shop) return;

        int prefabIndex = initializer.GetPrefabIndexAtSlot(slotIndex);
        if (prefabIndex < 0) return;

        isDragging = true;
        draggedPrefabIndex = prefabIndex;

        var (start, end) = initializer.GetSectorRange(slotIndex);
        sectorStartSlot = start;
        sectorEndSlot = end;

        dragVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dragVisual.transform.localScale = new Vector3(0.3f * (end - start + 1), 0.15f, 0.3f);
        Destroy(dragVisual.GetComponent<Collider>());

        SectorData data = initializer.GetSectorDataAtSlot(slotIndex);
        Renderer rend = dragVisual.GetComponent<Renderer>();
        if (rend != null && data != null)
            rend.material = initializer.GetMaterialForType(data.Type);
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

        // Попали в слот инвентаря?
        InventorySlot invSlot = GetUnderMouse<InventorySlot>();
        if (invSlot != null && invSlot.CanAcceptSector())
        {
            SectorData data = initializer.GetSectorDataAtSlot(slotIndex);
            if (data != null)
            {
                initializer.RemoveWholeSector(slotIndex);
                invSlot.PlaceSectorSimple(
                    initializer.GetMaterialForType(data.Type),
                    Color.white, data, draggedPrefabIndex
                );
                draggedPrefabIndex = -1;
                return;
            }
        }

        // Попали в другой слот рулетки?
        RouletteSlotHandler other = GetUnderMouse<RouletteSlotHandler>();
        if (other != null && other.slotIndex != slotIndex)
        {
            initializer.SwapWholeSectors(slotIndex, other.slotIndex);
            draggedPrefabIndex = -1;
            return;
        }

        // Промахнулись — возвращаем сектор на место
        // Сектор никуда не делся (мы его не удаляли до подтверждения),
        // поэтому просто обновляем визуал
        initializer.RefreshVisuals();
        draggedPrefabIndex = -1;
    }

    private T GetUnderMouse<T>() where T : Component
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            return hit.collider.GetComponent<T>();
        return null;
    }
}