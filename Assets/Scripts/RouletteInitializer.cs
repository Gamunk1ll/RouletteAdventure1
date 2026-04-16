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
    public bool useSlotBasedRotation = true;
    public float sectorScaleMultiplier = 1f;
    public bool autoFitSectorWidth = false;
    public float tangentialScaleMultiplier = 1f;
    [Tooltip("If enabled, sectors are positioned by slot anchors so active sectors visually replace gray placeholders.")]
    public bool useSlotAnchorsForSpawn = true;
    public bool placeAtWheelCenter = true;
    public Transform wheelCenter;
    public Transform spawnedSectorsParent;
    public Vector3 sectorPositionOffset;
    public bool hideGraySlots = true;
    [Tooltip("Optional transforms that explicitly define each slot position/rotation. If empty, Slot transforms are used.")]
    public List<Transform> slotSpawnAnchors = new();

    private readonly List<BaseSector> activeSectors = new();
    private List<int> slotToSectorMap = new();
    private readonly List<Renderer> slotPlaceholderVisuals = new();
    private readonly List<Color> slotPlaceholderBaseColors = new();

    private void Start()
    {
        AutoFixLegacyCenterSpawnMode();
        InitializeRoulette();
    }

    private void AutoFixLegacyCenterSpawnMode()
    {
        if (useSlotAnchorsForSpawn || !placeAtWheelCenter)
        {
            return;
        }

        if (allSlots == null || allSlots.Count < 2)
        {
            return;
        }

        bool hasDistinctSlotPositions = false;
        Vector3 firstPos = allSlots[0] != null ? allSlots[0].transform.position : Vector3.zero;
        for (int i = 1; i < allSlots.Count; i++)
        {
            Slot slot = allSlots[i];
            if (slot != null && Vector3.Distance(firstPos, slot.transform.position) > 0.001f)
            {
                hasDistinctSlotPositions = true;
                break;
            }
        }

        if (!hasDistinctSlotPositions)
        {
            return;
        }

        useSlotAnchorsForSpawn = true;
        Debug.LogWarning("RouletteInitializer: detected legacy center-spawn configuration. Auto-enabled useSlotAnchorsForSpawn to prevent stacked sectors.");
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

            if (prefabSector == null || prefabSector.data == null)
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

        bool appliedValidOverrideVisual = false;
        if (sector.data != null && sector.data.rouletteVisualPrefab != null)
        {
            GameObject overrideVisual = Instantiate(sector.data.rouletteVisualPrefab, sectorObj.transform, false);
            overrideVisual.name = $"{sector.data.rouletteVisualPrefab.name}_Visual";
            overrideVisual.transform.localPosition = Vector3.zero;
            overrideVisual.transform.localRotation = Quaternion.identity;
            overrideVisual.transform.localScale = Vector3.one;

            Renderer[] overrideRendererArray = overrideVisual.GetComponentsInChildren<Renderer>(true);
            bool hasValidOverrideRenderer = false;
            for (int i = 0; i < overrideRendererArray.Length; i++)
            {
                if (HasRenderableGeometry(overrideRendererArray[i]))
                {
                    hasValidOverrideRenderer = true;
                    break;
                }
            }

            if (hasValidOverrideRenderer)
            {
                HashSet<Renderer> overrideRenderers = new HashSet<Renderer>(overrideRendererArray);
                Renderer[] allRenderers = sectorObj.GetComponentsInChildren<Renderer>(true);

                for (int i = 0; i < allRenderers.Length; i++)
                {
                    Renderer renderer = allRenderers[i];
                    if (renderer != null && !overrideRenderers.Contains(renderer))
                        renderer.enabled = false;
                }

                appliedValidOverrideVisual = true;
            }
            else
            {
                Destroy(overrideVisual);
                Debug.LogWarning($"Roulette visual override for '{sector.data.name}' has missing mesh/sprite render data. Using base visual prefab renderers.");
            }
        }

        Quaternion computedSectorRotation = CalculateSectorLocalRotation(
            startSlot,
            size,
            prefabSector,
            parent
        );

        sectorObj.transform.localRotation = computedSectorRotation;
        FitSectorScaleToSlots(sectorObj.transform, startSlot, endSlot);

        Renderer[] allSectorRenderers = sectorObj.GetComponentsInChildren<Renderer>(true);
        List<Renderer> visibleRenderers = new List<Renderer>(allSectorRenderers.Length);
        for (int i = 0; i < allSectorRenderers.Length; i++)
        {
            if (allSectorRenderers[i] != null && allSectorRenderers[i].enabled && HasRenderableGeometry(allSectorRenderers[i]))
                visibleRenderers.Add(allSectorRenderers[i]);
        }

        if (visibleRenderers.Count == 0 && appliedValidOverrideVisual)
        {
            string sectorName = sector.data != null ? sector.data.name : sector.name;
            Debug.LogWarning($"Sector '{sectorName}' resolved with empty renderers after override. Falling back to slot placeholder tint.");
        }

        Renderer[] slotRenderers = visibleRenderers.ToArray();
        Renderer sectorRenderer = slotRenderers.Length > 0 ? slotRenderers[0] : null;
        bool usingPerSlotVisuals = slotRenderers.Length >= size;
        bool fallbackToSlotPlaceholder = sectorRenderer == null;

        for (int i = startSlot; i <= endSlot; i++)
        {
            allSlots[i].sector = sector;
            allSlots[i].index = i;
            if (fallbackToSlotPlaceholder)
            {
                Renderer placeholder = i < slotPlaceholderVisuals.Count ? slotPlaceholderVisuals[i] : null;
                allSlots[i].visual = placeholder;
                SetGraySlotVisible(i, true);
                ApplySlotPlaceholderColor(i, sector.data != null ? sector.data.Type : SectorType.Attack);
            }
            else
            {
                allSlots[i].visual = usingPerSlotVisuals
                    ? slotRenderers[i - startSlot]
                    : sectorRenderer;
                SetGraySlotVisible(i, false);
                RestoreSlotPlaceholderColor(i);
            }
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
        if (!useSlotAnchorsForSpawn && placeAtWheelCenter)
        {
            return wheelCenter != null ? wheelCenter.position : transform.position;
        }

        Vector3 accumulated = Vector3.zero;
        int count = 0;

        for (int i = startSlot; i <= endSlot; i++)
        {
            Transform anchor = GetSlotAnchor(i);
            if (anchor == null)
            {
                continue;
            }

            accumulated += anchor.position;
            count++;
        }

        if (count == 0)
        {
            return transform.position;
        }

        return accumulated / count;
    }

    private Quaternion CalculateSectorLocalRotation(int startSlot, int size, BaseSector prefabSector, Transform parent)
    {
        if (allSlots == null || allSlots.Count == 0)
        {
            return prefabSector != null ? prefabSector.transform.localRotation : Quaternion.identity;
        }

        if (useSlotBasedRotation)
        {
            float slotCenterIndex = startSlot + (size - 1) * 0.5f;
            int leftIndex = Mathf.Clamp(Mathf.FloorToInt(slotCenterIndex), 0, allSlots.Count - 1);
            int rightIndex = Mathf.Clamp(Mathf.CeilToInt(slotCenterIndex), 0, allSlots.Count - 1);

            Transform leftSlot = GetSlotAnchor(leftIndex);
            Transform rightSlot = GetSlotAnchor(rightIndex);

            if (leftSlot != null || rightSlot != null)
            {
                float slotVisualOffsetZ = prefabSector != null ? prefabSector.visualRotationOffsetZ : 0f;
                Vector3 slotPrefabEuler = prefabSector != null
                    ? prefabSector.transform.localEulerAngles
                    : Vector3.zero;

                Quaternion leftRotation = leftSlot != null ? leftSlot.transform.rotation : rightSlot.transform.rotation;
                Quaternion rightRotation = rightSlot != null ? rightSlot.transform.rotation : leftRotation;
                float blend = Mathf.Clamp01(slotCenterIndex - leftIndex);

                Quaternion worldRotation = Quaternion.Slerp(leftRotation, rightRotation, blend) * Quaternion.Euler(0f, 0f, slotVisualOffsetZ);
                Quaternion localRotation = parent != null
                    ? Quaternion.Inverse(parent.rotation) * worldRotation
                    : worldRotation;

                Vector3 localEuler = localRotation.eulerAngles;
                return Quaternion.Euler(slotPrefabEuler.x, slotPrefabEuler.y, localEuler.z + sectorRotationOffset);
            }
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
            Transform currentAnchor = GetSlotAnchor(i);
            Transform nextAnchor = GetSlotAnchor(i + 1);
            if (currentAnchor == null || nextAnchor == null)
            {
                continue;
            }

            width += Vector3.Distance(currentAnchor.position, nextAnchor.position);
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

        Transform currentAnchor = GetSlotAnchor(slotIndex);
        Transform previousAnchor = GetSlotAnchor(previous);
        Transform nextAnchor = GetSlotAnchor(next);
        if (currentAnchor == null)
        {
            return 0.2f;
        }

        float previousDistance = previousAnchor != null
            ? Vector3.Distance(currentAnchor.position, previousAnchor.position)
            : 0f;
        float nextDistance = nextAnchor != null
            ? Vector3.Distance(currentAnchor.position, nextAnchor.position)
            : 0f;

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

    private Transform GetSlotAnchor(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= allSlots.Count)
        {
            return null;
        }

        if (slotSpawnAnchors != null && slotIndex < slotSpawnAnchors.Count && slotSpawnAnchors[slotIndex] != null)
        {
            return slotSpawnAnchors[slotIndex];
        }

        Slot slot = allSlots[slotIndex];
        return slot != null ? slot.transform : null;
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
        slotPlaceholderBaseColors.Clear();
        for (int i = 0; i < allSlots.Count; i++)
        {
            Renderer renderer = allSlots[i] != null ? allSlots[i].visual : null;
            slotPlaceholderVisuals.Add(renderer);
            slotPlaceholderBaseColors.Add(GetRendererColor(renderer));
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
            RestoreSlotPlaceholderColor(i);
            SetGraySlotVisible(i, true);
        }
    }

    private bool HasRenderableGeometry(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        MeshRenderer meshRenderer = renderer as MeshRenderer;
        if (meshRenderer != null)
        {
            MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null;
        }

        SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
        if (skinnedRenderer != null)
        {
            return skinnedRenderer.sharedMesh != null;
        }

        SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
        if (spriteRenderer != null)
        {
            return spriteRenderer.sprite != null;
        }

        return true;
    }

    private void ApplySlotPlaceholderColor(int slotIndex, SectorType sectorType)
    {
        if (slotIndex < 0 || slotIndex >= slotPlaceholderVisuals.Count)
        {
            return;
        }

        Renderer renderer = slotPlaceholderVisuals[slotIndex];
        if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_Color"))
        {
            return;
        }

        renderer.material.color = GetSectorTint(sectorType);
    }

    private void RestoreSlotPlaceholderColor(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotPlaceholderVisuals.Count || slotIndex >= slotPlaceholderBaseColors.Count)
        {
            return;
        }

        Renderer renderer = slotPlaceholderVisuals[slotIndex];
        if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_Color"))
        {
            return;
        }

        renderer.material.color = slotPlaceholderBaseColors[slotIndex];
    }

    private Color GetRendererColor(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty("_Color"))
        {
            return Color.white;
        }

        return renderer.sharedMaterial.color;
    }

    private Color GetSectorTint(SectorType sectorType)
    {
        switch (sectorType)
        {
            case SectorType.Attack:
                return new Color(0.95f, 0.35f, 0.35f, 1f);
            case SectorType.Shield:
                return new Color(0.35f, 0.85f, 1f, 1f);
            case SectorType.Heal:
                return new Color(0.35f, 1f, 0.45f, 1f);
            case SectorType.Money:
                return new Color(1f, 0.85f, 0.2f, 1f);
            default:
                return Color.white;
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