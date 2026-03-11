using System.Collections.Generic;
using UnityEngine;

public class RouletteInitializer : MonoBehaviour
{
    [Header("All slots")]
    public List<Slot> allSlots = new();

    [Header("Sector prefabs")]
    public List<GameObject> sectorPrefabs = new();

    [Header("Start configuration (prefab indexes)")]
    public List<int> startingConfiguration = new();

    private readonly List<BaseSector> activeSectors = new();
    private List<int> slotToSectorMap = new();

    private void Start()
    {
        InitializeRoulette();
    }

    public void InitializeRoulette()
    {
        ClearRoulette();

        if (allSlots == null || allSlots.Count == 0)
        {
            Debug.LogError("RouletteInitializer: allSlots is empty.");
            return;
        }

        slotToSectorMap = new List<int>(new int[allSlots.Count]);
        for (int i = 0; i < slotToSectorMap.Count; i++)
        {
            slotToSectorMap[i] = -1;
        }

        int currentSlot = 0;

        for (int i = 0; i < startingConfiguration.Count && currentSlot < allSlots.Count; i++)
        {
            int prefabIndex = startingConfiguration[i];

            if (prefabIndex < 0 || prefabIndex >= sectorPrefabs.Count)
            {
                Debug.LogWarning($"RouletteInitializer: prefab index {prefabIndex} is out of range at config position {i}.");
                continue;
            }

            GameObject prefab = sectorPrefabs[prefabIndex];
            BaseSector prefabSector = ResolveSectorComponent(prefab);

            if (prefabSector == null)
            {
                Debug.LogError($"RouletteInitializer: BaseSector component was not found on prefab '{prefab.name}' (index {prefabIndex}).");
                continue;
            }

            if (prefabSector.data == null)
            {
                Debug.LogError($"RouletteInitializer: SectorData is not assigned for prefab '{prefab.name}'.");
                continue;
            }

            int sectorSize = Mathf.Max(1, prefabSector.data.size);
            if (currentSlot + sectorSize > allSlots.Count)
            {
                Debug.LogWarning($"RouletteInitializer: sector '{prefabSector.data.Type}' does not fit from slot {currentSlot} (size {sectorSize}).");
                continue;
            }

            BaseSector spawnedSector = SpawnSector(currentSlot, sectorSize, prefab);
            if (spawnedSector == null)
            {
                continue;
            }

            int sectorIndex = activeSectors.Count - 1;
            for (int s = 0; s < sectorSize; s++)
            {
                slotToSectorMap[currentSlot + s] = sectorIndex;
            }

            currentSlot += sectorSize;
        }

        Debug.Log($"Roulette initialized. Active sectors: {activeSectors.Count}");
    }

    private BaseSector SpawnSector(int startSlot, int size, GameObject prefab)
    {
        int endSlot = startSlot + size - 1;
        Slot startSlotObj = allSlots[startSlot];
        Slot endSlotObj = allSlots[endSlot];

        // Вычисляем центр между первым и последним слотом сектора
        Vector3 centerPos = (startSlotObj.transform.position + endSlotObj.transform.position) / 2f;

        // Спавним сектор БЕЗ родителя
        GameObject sectorObj = Instantiate(prefab, centerPos, startSlotObj.transform.rotation, transform);

        // Значительно увеличиваем масштаб
        float baseScale = 5f;  // Базовый масштаб
        float scaleMultiplier = size * baseScale;

        Vector3 currentScale = sectorObj.transform.localScale;
        sectorObj.transform.localScale = new Vector3(
            currentScale.x * scaleMultiplier,
            currentScale.y * scaleMultiplier,
            currentScale.z * scaleMultiplier
        );

        BaseSector sector = ResolveSectorComponent(sectorObj);
        if (sector == null)
        {
            Debug.LogError($"RouletteInitializer: spawned object '{sectorObj.name}' does not contain BaseSector component.");
            Destroy(sectorObj);
            return null;
        }

        Renderer sectorRenderer = sector.GetComponentInChildren<Renderer>(true);

        // Назначаем сектор всем слотам которые он занимает
        for (int i = startSlot; i <= endSlot; i++)
        {
            allSlots[i].sector = sector;
            allSlots[i].index = i;
            allSlots[i].visual = sectorRenderer;
        }

        activeSectors.Add(sector);
        Debug.Log($"Spawned {sector.data.Type} (size {size}) into slots {startSlot}-{endSlot}");
        return sector;
    }

    private static BaseSector ResolveSectorComponent(GameObject sectorObject)
    {
        if (sectorObject == null)
        {
            return null;
        }

        BaseSector sector = sectorObject.GetComponent<BaseSector>();
        return sector != null ? sector : sectorObject.GetComponentInChildren<BaseSector>(true);
    }

    private void ClearRoulette()
    {
        foreach (BaseSector sector in activeSectors)
        {
            if (sector != null)
            {
                Destroy(sector.gameObject);
            }
        }

        activeSectors.Clear();

        foreach (Slot slot in allSlots)
        {
            if (slot == null)
            {
                continue;
            }

            slot.sector = null;
            slot.visual = null;
        }
    }

    public BaseSector GetSectorInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotToSectorMap.Count)
        {
            return null;
        }

        int sectorIndex = slotToSectorMap[slotIndex];
        if (sectorIndex < 0 || sectorIndex >= activeSectors.Count)
        {
            return null;
        }

        return activeSectors[sectorIndex];
    }

    public List<BaseSector> GetActiveSectors()
    {
        return activeSectors;
    }
}