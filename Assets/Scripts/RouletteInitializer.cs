using System.Collections.Generic;
using UnityEngine;

public class RouletteInitializer : MonoBehaviour
{
    public List<Slot> allSlots = new();
    public List<GameObject> sectorPrefabs = new();
    public List<int> startingConfiguration = new();
    public float sectorRotationOffset;
    public float referenceSectorAngleZ = 180.5f;
    public int referenceStartSlot = 0;
    public int referenceSectorSize = 2;
    public bool rotateClockwise = true;
    public float sectorScaleMultiplier = 1f;
    public bool autoFitSectorWidth = false;
    public float tangentialScaleMultiplier = 1f;
    public bool placeAtWheelCenter = true;
    public Transform wheelCenter;
    public Transform spawnedSectorsParent;
    public Vector3 sectorPositionOffset;
    public bool hideGraySlots = true;

    private readonly List<BaseSector> activeSectors = new();
    private List<int> slotToSectorMap = new();
    private readonly List<Renderer> slotPlaceholderVisuals = new();

    private void Start()
    {
        InitializeRoulette();
    }

    public void InitializeRoulette()
    {
        CaptureSlotPlaceholders();
        ClearRoulette();

        if (allSlots == null || allSlots.Count == 0)
        {
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
                continue;
            }

            GameObject prefab = sectorPrefabs[prefabIndex];
            BaseSector prefabSector = ResolveSectorComponent(prefab);

            if (prefabSector == null)
            {
                continue;
            }

            if (prefabSector.data == null)
            {
                continue;
            }

            int sectorSize = Mathf.Max(1, prefabSector.data.size);
            if (currentSlot + sectorSize > allSlots.Count)
            {
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
    }

    private BaseSector SpawnSector(int startSlot, int size, GameObject prefab)
    {
        int endSlot = startSlot + size - 1;
        Slot startSlotObj = allSlots[startSlot];
        Slot endSlotObj = allSlots[endSlot];

        if (startSlotObj == null || endSlotObj == null)
        {
            return null;
        }

        Transform parent = ResolveSpawnParent(startSlotObj);
        Vector3 sectorPosition = CalculateSectorPosition(startSlot, endSlot) + sectorPositionOffset;
        BaseSector prefabSector = ResolveSectorComponent(prefab);

        GameObject sectorObj = Instantiate(prefab, parent, false);
        sectorObj.transform.localPosition = parent != null
            ? parent.InverseTransformPoint(sectorPosition)
            : sectorPosition;

        BaseSector sector = ResolveSectorComponent(sectorObj);
        if (sector == null)
        {
            Destroy(sectorObj);
            return null;
        }

        List<Renderer> slotRenderers = SpawnSlotVisuals(sectorObj.transform, prefabSector, startSlot, endSlot);
        bool usingPerSlotVisuals = slotRenderers.Count == size;

        if (!usingPerSlotVisuals)
        {
            Quaternion sectorLocalRotation = CalculateSectorLocalRotation(
                startSlot,
                size,
                prefabSector
            );

            sectorObj.transform.localRotation = sectorLocalRotation;
            FitSectorScaleToSlots(sectorObj.transform, startSlot, endSlot);
        }
        else
        {
            sectorObj.transform.localRotation = Quaternion.identity;
            sectorObj.transform.localScale = Vector3.one;
            SetOwnRenderersEnabled(sectorObj, false);
        }

        Renderer sectorRenderer = usingPerSlotVisuals
            ? slotRenderers[0]
            : sector.GetComponentInChildren<Renderer>(true);

        for (int i = startSlot; i <= endSlot; i++)
        {
            allSlots[i].sector = sector;
            allSlots[i].index = i;
            allSlots[i].visual = usingPerSlotVisuals
                ? slotRenderers[i - startSlot]
                : sectorRenderer;
            SetGraySlotVisible(i, false);
        }

        activeSectors.Add(sectorObj.GetComponent<BaseSector>() ?? sector);
        return sector;
    }

    private Transform ResolveSpawnParent(Slot startSlot)
    {
        if (spawnedSectorsParent != null)
        {
            return spawnedSectorsParent;
        }

        if (startSlot != null && startSlot.transform.parent != null)
        {
            return startSlot.transform.parent;
        }

        return transform;
    }

    private Vector3 CalculateSectorPosition(int startSlot, int endSlot)
    {
        if (placeAtWheelCenter)
        {
            return wheelCenter != null ? wheelCenter.position : transform.position;
        }

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

    private Quaternion CalculateSectorLocalRotation(int startSlot, int size, BaseSector prefabSector)
    {
        if (allSlots == null || allSlots.Count == 0)
        {
            return prefabSector != null ? prefabSector.transform.localRotation : Quaternion.identity;
        }

        float anglePerSlot = 360f / allSlots.Count;
        float centerIndex = startSlot + (size - 1) * 0.5f;
        float referenceCenterIndex = referenceStartSlot + (Mathf.Max(1, referenceSectorSize) - 1) * 0.5f;
        float direction = rotateClockwise ? -1f : 1f;
        float visualOffsetZ = prefabSector != null ? prefabSector.visualRotationOffsetZ : 0f;
        Vector3 prefabEuler = prefabSector != null
            ? prefabSector.transform.localEulerAngles
            : Vector3.zero;

        float zAngle =
            referenceSectorAngleZ +
            (centerIndex - referenceCenterIndex) * anglePerSlot * direction +
            sectorRotationOffset +
            visualOffsetZ;

        return Quaternion.Euler(prefabEuler.x, prefabEuler.y, zAngle);
    }

    private List<Renderer> SpawnSlotVisuals(Transform sectorRoot, BaseSector prefabSector, int startSlot, int endSlot)
    {
        List<Renderer> renderers = new();

        if (sectorRoot == null || prefabSector == null || prefabSector.data == null || prefabSector.data.visualPrefab == null)
        {
            return renderers;
        }

        GameObject visualPrefab = prefabSector.data.visualPrefab;
        Vector3 visualScale = visualPrefab.transform.localScale;
        Vector3 visualEuler = visualPrefab.transform.localEulerAngles;

        for (int slotIndex = startSlot; slotIndex <= endSlot; slotIndex++)
        {
            GameObject visualObj = Instantiate(visualPrefab, sectorRoot, false);
            visualObj.name = $"{visualPrefab.name}_Slot{slotIndex}";
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localRotation = CalculateSingleSlotVisualRotation(slotIndex, visualEuler);
            visualObj.transform.localScale = visualScale;

            Renderer renderer = visualObj.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                renderers.Add(renderer);
            }
        }

        return renderers;
    }

    private Quaternion CalculateSingleSlotVisualRotation(int slotIndex, Vector3 baseVisualEuler)
    {
        float anglePerSlot = allSlots.Count > 0 ? 360f / allSlots.Count : 0f;
        float referenceCenterIndex = referenceStartSlot + (Mathf.Max(1, referenceSectorSize) - 1) * 0.5f;
        float direction = rotateClockwise ? -1f : 1f;
        float zAngle =
            referenceSectorAngleZ +
            (slotIndex - referenceCenterIndex) * anglePerSlot * direction +
            sectorRotationOffset;

        return Quaternion.Euler(baseVisualEuler.x, baseVisualEuler.y, zAngle);
    }

    private void SetOwnRenderersEnabled(GameObject target, bool enabled)
    {
        if (target == null)
        {
            return;
        }

        foreach (Renderer renderer in target.GetComponents<Renderer>())
        {
            renderer.enabled = enabled;
        }
    }

    private void FitSectorScaleToSlots(Transform sectorTransform, int startSlot, int endSlot)
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

        if (!autoFitSectorWidth)
        {
            sectorTransform.localScale = scale * radialScale;
            return;
        }

        float visualWidth = GetRendererWorldWidth(renderer, sectorTransform.right);
        float targetWidth = CalculateTargetArcWidth(startSlot, endSlot);
        float tangentialScale = Mathf.Max(0.01f, tangentialScaleMultiplier);

        float widthScale = targetWidth / visualWidth;
        sectorTransform.localScale = new Vector3(
            scale.x * widthScale * radialScale * tangentialScale,
            scale.y * radialScale,
            scale.z * radialScale
        );
    }

    private float GetRendererWorldWidth(Renderer renderer, Vector3 widthAxis)
    {
        Bounds bounds = renderer.bounds;
        Vector3 axis = widthAxis.sqrMagnitude > 0.0001f ? widthAxis.normalized : Vector3.right;

        Vector3 ext = bounds.extents;
        float projectedHalfWidth =
            Mathf.Abs(axis.x) * ext.x +
            Mathf.Abs(axis.y) * ext.y +
            Mathf.Abs(axis.z) * ext.z;

        return Mathf.Max(projectedHalfWidth * 2f, 0.001f);
    }

    private float CalculateTargetArcWidth(int startSlot, int endSlot)
    {
        if (allSlots == null || allSlots.Count == 0)
        {
            return 0.2f;
        }

        float width = 0f;
        for (int i = startSlot; i < endSlot; i++)
        {
            width += Vector3.Distance(allSlots[i].transform.position, allSlots[i + 1].transform.position);
        }

        if (width <= 0.001f)
        {
            width = EstimateSingleSlotWidth(startSlot);
        }

        return Mathf.Max(width, 0.2f);
    }

    private float EstimateSingleSlotWidth(int slotIndex)
    {
        if (allSlots == null || allSlots.Count < 2)
        {
            return 0.2f;
        }

        int previous = Mathf.Max(0, slotIndex - 1);
        int next = Mathf.Min(allSlots.Count - 1, slotIndex + 1);

        float previousDistance = Vector3.Distance(allSlots[slotIndex].transform.position, allSlots[previous].transform.position);
        float nextDistance = Vector3.Distance(allSlots[slotIndex].transform.position, allSlots[next].transform.position);

        float estimated = 0f;
        if (slotIndex > 0)
        {
            estimated += previousDistance;
        }

        if (slotIndex < allSlots.Count - 1)
        {
            estimated += nextDistance;
        }

        if (slotIndex > 0 && slotIndex < allSlots.Count - 1)
        {
            estimated *= 0.5f;
        }

        return Mathf.Max(estimated, 0.2f);
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

    private void CaptureSlotPlaceholders()
    {
        if (slotPlaceholderVisuals.Count == allSlots.Count && slotPlaceholderVisuals.Count > 0)
        {
            return;
        }

        slotPlaceholderVisuals.Clear();
        for (int i = 0; i < allSlots.Count; i++)
        {
            slotPlaceholderVisuals.Add(allSlots[i] != null ? allSlots[i].visual : null);
        }
    }

    private void SetGraySlotVisible(int slotIndex, bool visible)
    {
        if (!hideGraySlots || slotIndex < 0 || slotIndex >= slotPlaceholderVisuals.Count)
        {
            return;
        }

        Renderer placeholder = slotPlaceholderVisuals[slotIndex];
        if (placeholder != null)
        {
            placeholder.enabled = visible;
        }
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

        for (int i = 0; i < allSlots.Count; i++)
        {
            Slot slot = allSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.sector = null;
            slot.visual = i < slotPlaceholderVisuals.Count ? slotPlaceholderVisuals[i] : null;
            SetGraySlotVisible(i, true);
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