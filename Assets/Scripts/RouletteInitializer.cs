using UnityEngine;
using System.Collections.Generic;

public class RouletteInitializer : MonoBehaviour
{
    [Header("Âñå ñëîòû")]
    public List<Slot> allSlots = new List<Slot>();

    [Header("Ïðåôàáû ñåêòîðîâ")]
    public List<GameObject> sectorPrefabs = new List<GameObject>();

    [Header("Ñòàðòîâàÿ êîíôèãóðàöèÿ (èíäåêñû ïðåôàáîâ)")]
    public List<int> startingConfiguration = new List<int>();

    private List<BaseSector> activeSectors = new List<BaseSector>();
                BaseSector sector = ResolveSectorComponent(prefab);

                else
                {
                    Debug.LogError($"BaseSector component was not found on prefab '{prefab.name}' (index {prefabIndex})");
                }
        BaseSector sector = ResolveSectorComponent(sectorObj);
                Renderer sectorRenderer = sector.GetComponentInChildren<Renderer>();
        else
        {
            Debug.LogError($"Spawned object '{sectorObj.name}' does not contain BaseSector component");
        }
    }

    private BaseSector ResolveSectorComponent(GameObject sectorObject)
    {
        if (sectorObject == null)
        {
            return null;
        }

        BaseSector sector = sectorObject.GetComponent<BaseSector>();
        if (sector != null)
        {
            return sector;
        }

        return sectorObject.GetComponentInChildren<BaseSector>(true);
        InitializeRoulette();
    }

    public void InitializeRoulette()
    {
        // Î÷èñòêà
        ClearRoulette();

        slotToSectorMap = new List<int>(new int[allSlots.Count]);
        int currentSlot = 0;

        for (int i = 0; i < startingConfiguration.Count && currentSlot < allSlots.Count; i++)
        {
            int prefabIndex = startingConfiguration[i];

            if (prefabIndex >= 0 && prefabIndex < sectorPrefabs.Count)
            {
                GameObject prefab = sectorPrefabs[prefabIndex];
                BaseSector sector = prefab.GetComponent<BaseSector>();

                if (sector != null)
                {
                    int sectorSize = Mathf.Max(1, sector.data.size);

                    // Ïðîâåðÿåì ÷òî ñåêòîð ïîìåùàåòñÿ
                    if (currentSlot + sectorSize > allSlots.Count)
                    {
                        Debug.LogWarning($"Ñåêòîð {sector.data.Type} íå ïîìåùàåòñÿ!");
                        continue;
                    }

                    // Ñïàâíèì ñåêòîð
                    SpawnSector(currentSlot, sectorSize, prefab, sector);

                    // Îòìå÷àåì ñëîòû
                    for (int s = 0; s < sectorSize; s++)
                    {
                        slotToSectorMap[currentSlot + s] = activeSectors.Count - 1;
                    }

                    currentSlot += sectorSize;
                }
            }
        }

        Debug.Log($"Ðóëåòêà èíèöèàëèçèðîâàíà! Àêòèâíûõ ñåêòîðîâ: {activeSectors.Count}");
    }

    void SpawnSector(int startSlot, int size, GameObject prefab, BaseSector sectorData)
    {
        // Âû÷èñëÿåì ïîçèöèþ (öåíòð ìåæäó ñëîòàìè)
        int endSlot = startSlot + size - 1;
        Slot startSlotObj = allSlots[startSlot];
        Slot endSlotObj = allSlots[endSlot];

        Vector3 centerPos = (startSlotObj.transform.position + endSlotObj.transform.position) / 2;
        Quaternion rotation = startSlotObj.transform.rotation;

        // Ñïàâí
        GameObject sectorObj = Instantiate(prefab, centerPos, rotation);
        sectorObj.transform.SetParent(transform);


        float scaleMultiplier = size / 1f; 
        sectorObj.transform.localScale = new Vector3(
            sectorObj.transform.localScale.x * scaleMultiplier,
            sectorObj.transform.localScale.y,
            sectorObj.transform.localScale.z * scaleMultiplier
        );

        BaseSector sector = sectorObj.GetComponent<BaseSector>();
        if (sector != null)
        {
            for (int i = startSlot; i < startSlot + size; i++)
            {
                allSlots[i].sector = sector;
                allSlots[i].index = i;

                Renderer sectorRenderer = sector.GetComponent<Renderer>();
                if (sectorRenderer != null)
                {
                    allSlots[i].visual = sectorRenderer;
                }
            }

            activeSectors.Add(sector);
            Debug.Log($"Ñïàâí {sector.data.Type} (ðàçìåð {size}) â ñëîòàõ {startSlot}-{endSlot}");
        }
    }

    void ClearRoulette()
    {
        foreach (var sector in activeSectors)
        {
            if (sector != null)
                Destroy(sector.gameObject);
        }
        activeSectors.Clear();

        foreach (var slot in allSlots)
        {
            slot.sector = null;
        }
    }

    public BaseSector GetSectorInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotToSectorMap.Count)
        {
            int sectorIndex = slotToSectorMap[slotIndex];
            if (sectorIndex >= 0 && sectorIndex < activeSectors.Count)
            {
                return activeSectors[sectorIndex];
            }
        }
        return null;
    }

    public List<BaseSector> GetActiveSectors()
    {
        return activeSectors;
    }
}