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

    [Header("Spawn settings")]
    [Tooltip("Additional Z rotation in degrees applied to each spawned sector.")]
    public float sectorRotationOffset;
    [Tooltip("Multiplier for spawned sector scale relative to source prefab scale.")]
    public float sectorScaleMultiplier = 1f;
    [Tooltip("Extra offset from wheel center for all sectors.")]
    public Vector3 sectorPositionOffset;

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

        Vector3 sectorPosition = CalculateSectorPosition(startSlot, endSlot) + sectorPositionOffset;
        Quaternion sectorRotation = CalculateSectorRotation(startSlotObj, endSlotObj);

        GameObject sectorObj = Instantiate(prefab, sectorPosition, sectorRotation, transform);

        FitSectorScaleToSlots(sectorObj.transform, startSlotObj, endSlotObj, size);

        BaseSector sector = ResolveSectorComponent(sectorObj);
        if (sector == null)
        {
            Debug.LogError($"RouletteInitializer: spawned object '{sectorObj.name}' does not contain BaseSector component.");
            Destroy(sectorObj);
            return null;
        }

        Renderer sectorRenderer = sector.GetComponentInChildren<Renderer>(true);

        for (int i = startSlot; i <= endSlot; i++)
        {
            allSlots[i].sector = sector;
            allSlots[i].index = i;
            allSlots[i].visual = sectorRenderer;
        }

        // Keep reference to the spawned root when BaseSector is on child object.
        activeSectors.Add(sectorObj.GetComponent<BaseSector>() ?? sector);
        Debug.Log($"Spawned {sector.data.Type} (size {size}) into slots {startSlot}-{endSlot}");
        return sector;
    }

    private Vector3 CalculateSectorPosition(int startSlot, int endSlot)
    {
        Vector3 accumulated = Vector3.zero;
        int count = 0;

        for (int i = startSlot; i <= endSlot; i++)
        {
            Slot slot = allSlots[i];
            if (slot == null)
            {
                continue;
            }

            accumulated += slot.transform.position;
            count++;
        }

        if (count == 0)
        {
            return transform.position;
        }

        return accumulated / count;
    }

    private Quaternion CalculateSectorRotation(Slot startSlotObj, Slot endSlotObj)
    {
        Quaternion sectorRotation = Quaternion.Slerp(
            startSlotObj.transform.rotation,
            endSlotObj.transform.rotation,
            0.5f
        );

        return sectorRotation * Quaternion.Euler(0f, 0f, sectorRotationOffset);
    }

    private void FitSectorScaleToSlots(Transform sectorTransform, Slot startSlotObj, Slot endSlotObj, int size)
    {
        if (sectorTransform == null)
        {
            return;
        }

        Vector3 scale = sectorTransform.localScale;
        float radialScale = Mathf.Max(0.01f, sectorScaleMultiplier);

        Renderer renderer = sectorTransform.GetComponentInChildren<Renderer>(true);
        if (renderer == null)
        {
            sectorTransform.localScale = scale * radialScale;
            return;
        }

        Bounds bounds = renderer.bounds;
        float visualWidth = Mathf.Max(bounds.size.x, 0.001f);
        float targetWidth = Vector3.Distance(startSlotObj.transform.position, endSlotObj.transform.position);
        targetWidth = Mathf.Max(targetWidth, 0.2f * Mathf.Max(1, size));

        float widthScale = targetWidth / visualWidth;
        sectorTransform.localScale = new Vector3(
            scale.x * widthScale * radialScale,
            scale.y * radialScale,
            scale.z * radialScale
        );
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
