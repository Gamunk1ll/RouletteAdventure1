using System.Collections.Generic;
using UnityEngine;

public class RouletteInitializer : MonoBehaviour
{
    [Header("Circle-îáúåêòû èç Blender (ïî ïîðÿäêó ñëîòîâ)")]
    public List<MeshRenderer> slotRenderers = new();

    [Header("Ïðåôàáû ñåêòîðîâ")]
    public List<GameObject> sectorPrefabs = new();

    [Header("Ñòàðòîâàÿ êîíôèãóðàöèÿ (îäèí èíäåêñ = îäèí ñåêòîð)")]
    public List<int> startingConfiguration = new();

    [Header("Ëîãè÷åñêèå ñëîòû")]
    public List<Slot> allSlots = new();

    [Header("Ñïàâí ïðåôàáîâ íà ðóëåòêå")]
    public Transform wheelSpinTransform;   
    public Transform spawnedSectorsParent;   
    public float sectorRotationOffset = 0f;  
    public float sectorScaleMultiplier = 1f;

    [Header("Ìàòåðèàëû (äëÿ ïóñòûõ ñëîòîâ)")]
    public Material emptyMaterial;
    public Material attackMaterial;
    public Material moneyMaterial;
    public Material shieldMaterial;
    public Material healMaterial;

    public int[] slotAssignment;

    private readonly Dictionary<int, GameObject> spawnedSectorObjects = new();

    private void Start()
    {
        BuildFromConfiguration(startingConfiguration);
        SetupSlotColliders();
    }



    public void BuildFromConfiguration(List<int> config)
    {
        ClearAllSpawnedSectors();

        slotAssignment = new int[slotRenderers.Count];
        for (int i = 0; i < slotAssignment.Length; i++)
            slotAssignment[i] = -1;

        foreach (var slot in allSlots)
            if (slot != null) slot.sector = null;

        int currentSlot = 0;
        foreach (int prefabIndex in config)
        {
            if (currentSlot >= slotRenderers.Count) break;
            if (prefabIndex < 0 || prefabIndex >= sectorPrefabs.Count)
            {
                currentSlot++;
                continue;
            }

            int size = GetSectorSize(prefabIndex);
            if (currentSlot + size > slotRenderers.Count) break;

            PlaceSector(currentSlot, prefabIndex, size);
            currentSlot += size;
        }

        RefreshEmptySlots();
    }

    private void PlaceSector(int startSlot, int prefabIndex, int size)
    {
        if (prefabIndex < 0 || prefabIndex >= sectorPrefabs.Count) return;
        if (startSlot + size > slotRenderers.Count) return;

        if (!TryCalculateSectorPose(startSlot, size, out Vector3 worldPos, out Quaternion worldRotation))
            return;
   private bool TryCalculateSectorPose(int startSlot, int size, out Vector3 worldPos, out Quaternion worldRotation)
    {
        Transform center = wheelSpinTransform != null ? wheelSpinTransform : transform;
        worldPos = Vector3.zero;
        int count = 0;
        for (int i = startSlot; i < startSlot + size; i++)
        {
            MeshRenderer slotRenderer = slotRenderers[i];
            if (slotRenderer == null)
                continue;
            worldPos += slotRenderer.transform.position;
            count++;
        }

        if (count == 0)
        {
            worldRotation = center.rotation;
            return false;
        }

        worldPos /= count;
        Vector3 radial = Vector3.ProjectOnPlane(worldPos - center.position, center.up);
        if (radial.sqrMagnitude < 0.0001f)
        {
            worldRotation = center.rotation;
            return true;
        }
        Vector3 sectorForward = radial.normalized;
        worldRotation = Quaternion.LookRotation(sectorForward, center.up) * Quaternion.Euler(0f, sectorRotationOffset, 0f);
        return true;
    }
        BaseSector sector = ResolveSector(obj);
        for (int i = startSlot; i < startSlot + size; i++)
        {
            slotAssignment[i] = prefabIndex;
            if (i < allSlots.Count && allSlots[i] != null)
            {
                allSlots[i].sector = sector;
                allSlots[i].index = i;
            }
            // Ñêðûâàåì Circle-ìåø ïîä ïðåôàáîì
            if (slotRenderers[i] != null)
                slotRenderers[i].enabled = false;
        }
        Debug.Log($"Sector {prefabIndex} startSlot={startSlot} worldPos={worldPos} | Circle bounds={slotRenderers[startSlot].bounds.center}");
        spawnedSectorObjects[startSlot] = obj;
    }

   private Quaternion CalculateSectorRotation(Vector3 worldPos)
{
    Transform center = wheelSpinTransform != null ? wheelSpinTransform : transform;

    // Íàïðàâëåíèå îò öåíòðà ê ñåêòîðó
    Vector3 toSector = worldPos - center.position;

    // Íîðìàëü ïëîñêîñòè ðóëåòêè = ëîêàëüíàÿ îñü Y îáúåêòà WheelSpin
    Vector3 wheelNormal = center.up;

    // Ïðîåöèðóåì íàïðàâëåíèå íà ïëîñêîñòü ðóëåòêè
    Vector3 projected = Vector3.ProjectOnPlane(toSector, wheelNormal).normalized;

    if (projected.sqrMagnitude < 0.0001f)
        return center.rotation;

    // Ñåêòîð ñìîòðèò óçêîé ÷àñòüþ Ê öåíòðó — forward = îò öåíòðà ê ñåêòîðó
    // up = íîðìàëü ïëîñêîñòè ðóëåòêè
    Quaternion rot = Quaternion.LookRotation(projected, wheelNormal);

    // Ïðèìåíÿåì offset
    return rot * Quaternion.Euler(0f, sectorRotationOffset, 0f);
}


    public (int start, int end) GetSectorRange(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotAssignment.Length)
            return (-1, -1);

        int prefabIndex = slotAssignment[slotIndex];
        if (prefabIndex < 0) return (-1, -1);

        int size = GetSectorSize(prefabIndex);

        int start = slotIndex;
        while (start > 0
               && slotAssignment[start - 1] == prefabIndex
               && (slotIndex - (start - 1)) < size)
            start--;

        int end = start + size - 1;
        if (end >= slotAssignment.Length)
            end = slotAssignment.Length - 1;

        return (start, end);
    }

    public void RemoveWholeSector(int slotIndex)
    {
        var (start, end) = GetSectorRange(slotIndex);
        if (start < 0) return;
        if (spawnedSectorObjects.TryGetValue(start, out GameObject obj))
        {
            if (obj != null) Destroy(obj);
            spawnedSectorObjects.Remove(start);
        }

        for (int i = start; i <= end; i++)
        {
            slotAssignment[i] = -1;
            if (i < allSlots.Count && allSlots[i] != null)
                allSlots[i].sector = null;
            if (i < slotRenderers.Count && slotRenderers[i] != null)
            {
                slotRenderers[i].enabled = true;
                slotRenderers[i].material = emptyMaterial;
            }
        }
    }

    public bool PlaceWholeSector(int startSlot, int prefabIndex)
    {
        int size = GetSectorSize(prefabIndex);
        if (startSlot + size > slotAssignment.Length) return false;

        for (int i = startSlot; i < startSlot + size; i++)
            if (slotAssignment[i] != -1) return false;

        PlaceSector(startSlot, prefabIndex, size);
        return true;
    }

    public void SwapWholeSectors(int slotA, int slotB)
    {
        int prefabA = GetPrefabIndexAtSlot(slotA);
        int prefabB = GetPrefabIndexAtSlot(slotB);

        var (startA, endA) = GetSectorRange(slotA);
        var (startB, endB) = GetSectorRange(slotB);

        if (startA < 0 || startB < 0) return;

        if (Mathf.Max(startA, startB) < Mathf.Min(endA + 1, endB + 1))
        {
            Debug.LogWarning("Ñåêòîðà ïåðåñåêàþòñÿ!");
            return;
        }

        RemoveWholeSector(slotA);
        RemoveWholeSector(slotB);

        bool placedA = PlaceWholeSector(startA, prefabB);
        bool placedB = PlaceWholeSector(startB, prefabA);

        if (!placedA || !placedB)
        {
            if (!placedA) PlaceWholeSector(startA, prefabA);
            if (!placedB) PlaceWholeSector(startB, prefabB);
            Debug.LogWarning("Íå óäàëîñü ïîìåíÿòü ñåêòîðà ìåñòàìè");
        }
    }

    private void SetupSlotColliders()
    {
        for (int i = 0; i < slotRenderers.Count; i++)
        {
            MeshRenderer r = slotRenderers[i];
            if (r == null) continue;

            if (r.GetComponent<Collider>() == null)
            {
                MeshCollider col = r.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = r.GetComponent<MeshFilter>()?.sharedMesh;
            }

            RouletteSlotHandler handler = r.GetComponent<RouletteSlotHandler>();
            if (handler == null)
                handler = r.gameObject.AddComponent<RouletteSlotHandler>();

            handler.slotIndex = i;
            handler.initializer = this;
        }
    }


    public void RefreshVisuals()
    {
        RefreshEmptySlots();
    }

    private void RefreshEmptySlots()
    {
        for (int i = 0; i < slotRenderers.Count; i++)
        {
            if (slotRenderers[i] == null) continue;
            int prefabIndex = i < slotAssignment.Length ? slotAssignment[i] : -1;

            if (prefabIndex < 0)
            {
                slotRenderers[i].enabled = true;
                slotRenderers[i].material = emptyMaterial;
            }
        }
    }



    public void RemoveSectorFromSlot(int slotIndex) => RemoveWholeSector(slotIndex);

    public void PlaceSectorAtSlot(int slotIndex, int prefabIndex)
    {
        PlaceWholeSector(slotIndex, prefabIndex);
    }

    public int GetPrefabIndexAtSlot(int slotIndex)
    {
        if (slotAssignment == null || slotIndex < 0 || slotIndex >= slotAssignment.Length) return -1;
        return slotAssignment[slotIndex];
    }

    public SectorData GetSectorDataAtSlot(int slotIndex)
    {
        int prefabIndex = GetPrefabIndexAtSlot(slotIndex);
        if (prefabIndex < 0) return null;
        return ResolveSector(sectorPrefabs[prefabIndex])?.data;
    }

    public Material GetMaterialForType(SectorType type)
    {
        switch (type)
        {
            case SectorType.Attack: return attackMaterial;
            case SectorType.Money: return moneyMaterial;
            case SectorType.Shield: return shieldMaterial;
            case SectorType.Heal: return healMaterial;
            default: return emptyMaterial;
        }
    }

    public void HighlightSlot(int slotIndex, bool on) { }



    private void ClearAllSpawnedSectors()
    {
        foreach (var kv in spawnedSectorObjects)
            if (kv.Value != null) Destroy(kv.Value);
        spawnedSectorObjects.Clear();
        foreach (var r in slotRenderers)
            if (r != null) r.enabled = true;
    }

    private int GetSectorSize(int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= sectorPrefabs.Count) return 1;
        BaseSector s = ResolveSector(sectorPrefabs[prefabIndex]);
        return s?.data != null ? Mathf.Max(1, s.data.size) : 1;
    }

    private static BaseSector ResolveSector(GameObject obj)
    {
        if (obj == null) return null;
        return obj.GetComponent<BaseSector>() ?? obj.GetComponentInChildren<BaseSector>(true);
    }
}