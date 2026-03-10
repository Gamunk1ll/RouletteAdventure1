using System.Collections.Generic;
using UnityEngine;

public class SectorPlacement : MonoBehaviour
{
    public List<Slot> slots;
    public List<BaseSector> sectors;

    private void Awake()
    {
        PlaceSectorsDefault();
    }
    public void PlaceSectorsDefault()
    {
        foreach (var slot in slots)
            slot.sector = null;

        int currentIndex = 0;

        foreach (BaseSector sector in sectors)
        {
            if (currentIndex + sector.data.size > slots.Count)
            {
                Debug.LogError(
                    $"Недостаточно слотов для стартовой раскладки. " +
                    $"Сектор {sector.data.Type}, size={sector.data.size}"
                );
                return;
            }

            for (int i = 0; i < sector.data.size; i++)
            {
                slots[currentIndex].sector = sector;
                currentIndex++;
            }
        }

        foreach (var slot in slots);
    }

    public bool TryPlaceSector(BaseSector sector)
    {
        int startIndex = Random.Range(0, slots.Count);

        for (int attempt = 0; attempt < slots.Count; attempt++)
        {
            bool canPlace = true;

            for (int i = 0; i < sector.data.size; i++)
            {
                int index = (startIndex + i) % slots.Count;
                if (slots[index].sector != null)
                {
                    canPlace = false;
                    break;
                }
            }

            if (canPlace)
            {
                for (int i = 0; i < sector.data.size; i++)
                {
                    int index = (startIndex + i) % slots.Count;
                    slots[index].sector = sector;
                }

                return true;
            }

            startIndex = (startIndex + 1) % slots.Count;
        }

        Debug.LogWarning(
            $"Не удалось разместить сектор {sector.data.Type}, size={sector.data.size}"
        );
        return false;
    }
}
