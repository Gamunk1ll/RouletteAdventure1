using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    public int slotIndex;
    public bool isOccupied = false;
    public GameObject currentSector;
    public SectorData sectorData;

    [Header("Визуализация")]
    public Material emptyMaterial;
    public Material highlightedMaterial;
    public Material occupiedMaterial;

    [Header("Префаб для визуала")]
    public GameObject simpleVisualPrefab;

    private Renderer slotRenderer;
    private Material originalMaterial;

    private void Start()
    {
        slotRenderer = GetComponent<Renderer>();
        if (slotRenderer != null)
        {
            originalMaterial = slotRenderer.material;
            if (emptyMaterial != null)
                slotRenderer.material = emptyMaterial;
        }
    }

    public void PlaceSectorSimple(Material mat, Color color, SectorData data, int prefabIndex)
    {
        if (isOccupied)
        {
            Debug.LogWarning($"Слот {slotIndex} уже занят!");
            return;
        }

        GameObject visual;
        if (simpleVisualPrefab != null)
        {
            visual = Instantiate(simpleVisualPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity, transform);
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.up * 0.5f;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 0.8f;
        }

        Renderer rend = visual.GetComponent<Renderer>();
        if (rend != null)
            rend.material = mat != null ? mat : rend.material;
        if (rend != null && mat == null)
            rend.material.color = color;

        // Коллайдер нужен для Raycast
        if (visual.GetComponent<Collider>() == null)
            visual.AddComponent<BoxCollider>();

        DraggableInventorySector draggable = visual.GetComponent<DraggableInventorySector>();
        if (draggable == null)
            draggable = visual.AddComponent<DraggableInventorySector>();
        draggable.Initialize(slotIndex, data, this, prefabIndex);

        currentSector = visual;
        sectorData = data;
        isOccupied = true;

        if (slotRenderer != null)
            slotRenderer.material = occupiedMaterial != null ? occupiedMaterial : originalMaterial;
    }

    public GameObject RemoveSector()
    {
        if (currentSector == null) return null;

        Destroy(currentSector);
        currentSector = null;
        sectorData = null;
        isOccupied = false;

        if (slotRenderer != null)
            slotRenderer.material = emptyMaterial != null ? emptyMaterial : originalMaterial;

        return null;
    }

    public void OnDragEnter()
    {
        if (slotRenderer != null && highlightedMaterial != null && !isOccupied)
            slotRenderer.material = highlightedMaterial;
    }

    public void OnDragExit()
    {
        if (slotRenderer == null) return;
        slotRenderer.material = isOccupied
            ? (occupiedMaterial != null ? occupiedMaterial : originalMaterial)
            : (emptyMaterial != null ? emptyMaterial : originalMaterial);
    }

    // Проверяем и визуально и логически
    public bool CanAcceptSector()
    {
        if (isOccupied) return false;
        // Дополнительная проверка через PlayerInventory
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.IsFull())
            return false;
        return true;
    }

    public void SetInteractable(bool interactable)
    {
        if (slotRenderer == null) return;
        Color c = slotRenderer.material.color;
        c.a = interactable ? 1f : 0.5f;
        slotRenderer.material.color = c;
    }
}