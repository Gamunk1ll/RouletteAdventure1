using UnityEngine;
using UnityEngine.UI;

public class SectorSelector : MonoBehaviour
{
    public static SectorSelector Instance;
    private int selectedInventoryIndex = -1;
    private int selectedSlotIndex = -1;
    public GameObject rouletteEditorPanel;
    public Button editModeButton;
    public Button confirmButton;
    public Button cancelButton;
    public SectorPlacement sectorPlacement;
    public PlayerInventory inventory;

    private bool isEditMode = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (editModeButton != null)
            editModeButton.onClick.AddListener(ToggleEditMode);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmChanges);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelChanges);

        if (rouletteEditorPanel != null)
            rouletteEditorPanel.SetActive(false);
    }
    public void ToggleEditMode()
    {
        isEditMode = !isEditMode;

        if (rouletteEditorPanel != null)
            rouletteEditorPanel.SetActive(isEditMode);

        Debug.Log($"Режим редактирования: {isEditMode}");
    }
    public void SelectFromInventory(int index)
    {
        if (!isEditMode) return;

        selectedInventoryIndex = index;
        Debug.Log($"Выбран сектор из инвентаря: {index}");
        if (selectedSlotIndex >= 0)
        {
            ReplaceSector();
        }
    }
    public void SelectSlot(int slotIndex)
    {
        if (!isEditMode) return;

        selectedSlotIndex = slotIndex;
        Debug.Log($"Выбран слот рулетки: {slotIndex}");
        if (selectedInventoryIndex >= 0)
        {
            ReplaceSector();
        }
    }
    void ReplaceSector()
    {
        if (selectedInventoryIndex < 0 || selectedSlotIndex < 0)
            return;

        SectorData newSector = inventory.GetSector(selectedInventoryIndex);
        if (newSector == null) return;

        Slot[] slots = sectorPlacement.slots.ToArray();
        BaseSector oldSector = slots[selectedSlotIndex].sector;

        slots[selectedSlotIndex].sector = null;

        SectorData tempSector = newSector;
        inventory.RemoveSector(selectedInventoryIndex);

        if (oldSector != null && oldSector.data != null)
        {
            inventory.AddSector(oldSector.data);
        }
        sectorPlacement.PlaceSectorsDefault();
        selectedInventoryIndex = -1;
        selectedSlotIndex = -1;

        Debug.Log("Сектор заменен!");
    }
    public void SellSelectedSector()
    {
        if (selectedInventoryIndex >= 0)
        {
            inventory.SellSector(selectedInventoryIndex);
            selectedInventoryIndex = -1;
        }
    }

    void ConfirmChanges()
    {
        Debug.Log("Изменения подтверждены");
        ToggleEditMode();
    }

    void CancelChanges()
    {
        Debug.Log("Изменения отменены");
        selectedInventoryIndex = -1;
        selectedSlotIndex = -1;
        ToggleEditMode();
    }

    public bool IsEditMode()
    {
        return isEditMode;
    }
}