using UnityEngine;

public class DraggableRouletteSector : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;

    // Данные сектора (для логики игры)
    public SectorData sectorData;
    public int originalSlotIndex;

    // Визуальные данные (копируем при перетаскивании)
    private Material sectorMaterial;
    private Color sectorColor;

    [Header("Настройки")]
    public float dragHeight = 2f;
    public float smoothSpeed = 10f;

    private RouletteInitializer rouletteInitializer;

    private void Start()
    {
        mainCamera = Camera.main;
        rouletteInitializer = FindObjectOfType<RouletteInitializer>();

        // Запоминаем визуал сектора при старте
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        {
            sectorMaterial = rend.material;
            sectorColor = rend.material.color;
        }
    }

    private void OnMouseDown()
    {
        StartDrag();
    }

    private void StartDrag()
    {
        isDragging = true;
        transform.parent = null;

        Debug.Log($"Перетаскивание сектора {sectorData?.Type} из слота {originalSlotIndex}");
    }

    private void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.up * dragHeight);

        if (plane.Raycast(ray, out float distance))
        {
            transform.position = Vector3.Lerp(transform.position, ray.GetPoint(distance), smoothSpeed * Time.deltaTime);
        }

        HighlightInventorySlots();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        InventorySlot targetSlot = GetInventorySlotUnderMouse();

        if (targetSlot != null && targetSlot.CanAcceptSector())
        {
            TransferToInventory(targetSlot);
        }
        else
        {
            ReturnToRoulette();
        }

        ResetInventoryHighlight();
    }

    private void TransferToInventory(InventorySlot slot)
    {
        // 1. Удаляем из рулетки (логика)
        if (rouletteInitializer != null)
        {
            rouletteInitializer.RemoveSectorFromSlot(originalSlotIndex);
        }

        // 2. Создаём простой визуал в инвентаре (куб с тем же материалом)
        slot.PlaceSectorSimple(sectorMaterial, sectorColor, sectorData, -1);

        // 3. Добавляем в инвентарь (логика игры)
        if (PlayerInventory.Instance != null && sectorData != null)
        {
            PlayerInventory.Instance.AddSector(sectorData, sectorData.buyPrice);
        }

        // 4. Удаляем оригинал с рулетки
        Destroy(gameObject);

        Debug.Log($"Сектор перенесён в инвентарь (слот {slot.slotIndex})");
    }

    private void ReturnToRoulette()
    {
        // Если у сектора был родитель (рулетка) — возвращаем
        if (rouletteInitializer != null)
        {
            transform.parent = rouletteInitializer.transform;
        }
        // Позицию можно не возвращать точно — рулетка сама отрисует слоты
        Debug.Log("Сектор возвращён в рулетку");
    }

    private void HighlightInventorySlots()
    {
        InventorySlot[] slots = FindObjectsOfType<InventorySlot>();
        foreach (var slot in slots)
        {
            if (slot.CanAcceptSector())
                slot.OnDragEnter();
        }
    }

    private void ResetInventoryHighlight()
    {
        InventorySlot[] slots = FindObjectsOfType<InventorySlot>();
        foreach (var slot in slots)
        {
            slot.OnDragExit();
        }
    }

    private InventorySlot GetInventorySlotUnderMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            InventorySlot slot = hit.collider.GetComponent<InventorySlot>();
            return slot;
        }
        return null;
    }

    private void Update()
    {
        if (isDragging && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            ReturnToRoulette();
            isDragging = false;
            ResetInventoryHighlight();
        }
    }
}