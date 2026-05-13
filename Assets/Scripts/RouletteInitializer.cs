using System.Collections.Generic;
using UnityEngine;

public class RouletteInitializer : MonoBehaviour
{
    [Header("Circle-объекты из Blender (по порядку слотов)")]
    public List<MeshRenderer> slotRenderers = new();

    [Header("Префабы секторов")]
    public List<GameObject> sectorPrefabs = new();

    [Header("Стартовая конфигурация (один индекс = один сектор)")]
    public List<int> startingConfiguration = new();

    [Header("Логические слоты")]
    public List<Slot> allSlots = new();

    [Header("Спавн префабов на рулетке")]
    [Tooltip("Центр рулетки. Позиции слотов вычисляются ОТНОСИТЕЛЬНО этого объекта.")]
    public Transform wheelSpinTransform;

    [Tooltip("Родитель для спавнимых секторов. ВАЖНО: должен иметь localScale=(1,1,1) и localRotation=(0,0,0), " +
             "либо быть самим wheelSpinTransform. Иначе позиции исказятся из-за масштабирования.")]
    public Transform spawnedSectorsParent;

    [Header("Геометрия рулетки")]
    [Tooltip("Радиус от центра WheelSpin до центра слота. " +
             "Совместите голубые сферы Gizmos с Circle-мешами в Scene View.")]
    public float slotRadius = 1.5f;

    [Tooltip("Угол первого слота в локальном пространстве WheelSpin (градусы). " +
             "Красная линия Gizmo = слот 0.")]
    public float startAngleDeg = 90f;

    [Tooltip("Направление обхода: true = по часовой, false = против часовой")]
    public bool clockwise = false;

    [Tooltip("Поворот префаба вокруг его локальной оси Up. Подберите 0 / 90 / 180 / -90.")]
    public float sectorRotationOffset = 0f;

    [Header("Материалы (для пустых слотов)")]
    public Material emptyMaterial;
    public Material attackMaterial;
    public Material moneyMaterial;
    public Material shieldMaterial;
    public Material healMaterial;

    public int[] slotAssignment;

    private readonly Dictionary<int, GameObject> spawnedSectorObjects = new();

    // -----------------------------------------------------------------------
    private void Start()
    {
        ValidateParentTransform();
        BuildFromConfiguration(startingConfiguration);
        SetupSlotColliders();
    }

    // -----------------------------------------------------------------------
    // Проверка: если spawnedSectorsParent имеет неединичный scale — предупреждаем
    // -----------------------------------------------------------------------
    private void ValidateParentTransform()
    {
        Transform parent = spawnedSectorsParent != null ? spawnedSectorsParent : transform;

        if (parent != wheelSpinTransform)
        {
            Vector3 s = parent.localScale;
            Vector3 r = parent.localEulerAngles;

            bool scaleOk = Mathf.Approximately(s.x, 1f) && Mathf.Approximately(s.y, 1f) && Mathf.Approximately(s.z, 1f);
            bool rotOk = Mathf.Approximately(r.x, 0f) && Mathf.Approximately(r.y, 0f) && Mathf.Approximately(r.z, 0f);

            if (!scaleOk || !rotOk)
            {
                Debug.LogWarning($"[RouletteInitializer] WARNING: spawnedSectorsParent '{parent.name}' имеет неединичные трансформации!" +
                    $"\n  localScale = {s} (должно быть 1,1,1)" +
                    $"\n  localRotation = {r} (должно быть 0,0,0)" +
                    $"\n\nРЕШЕНИЕ:" +
                    $"\n  1) Либо установите Scale=(1,1,1) и Rotation=(0,0,0) у объекта '{parent.name}'" +
                    $"\n  2) Либо в Inspector перетащите WheelSpin в поле Spawned Sectors Parent (вместо '{parent.name}')");
            }
        }
    }

    // -----------------------------------------------------------------------
    // Локальный офсет в XZ-плоскости WheelSpin по углу и радиусу
    // -----------------------------------------------------------------------
    private Vector3 LocalOffsetForAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * slotRadius, 0f, Mathf.Sin(rad) * slotRadius);
    }

    private float GetMidAngleDeg(int startSlot, int endSlot)
    {
        int total = Mathf.Max(slotRenderers.Count, 1);
        float stepDeg = 360f / total;
        float dir = clockwise ? -1f : 1f;
        float mid = (startSlot + endSlot) / 2f;
        return startAngleDeg + dir * stepDeg * mid;
    }

    private Vector3 GetSlotRangeWorldPosition(int startSlot, int endSlot)
    {
        Transform center = wheelSpinTransform != null ? wheelSpinTransform : transform;
        return center.TransformPoint(LocalOffsetForAngle(GetMidAngleDeg(startSlot, endSlot)));
    }

    private Quaternion GetSlotWorldRotation(int startSlot, int endSlot)
    {
        Transform center = wheelSpinTransform != null ? wheelSpinTransform : transform;
        float angleDeg = GetMidAngleDeg(startSlot, endSlot);

        Vector3 localOut = LocalOffsetForAngle(angleDeg).normalized;
        Vector3 worldOut = center.TransformDirection(localOut);
        Vector3 worldUp = center.up;

        Quaternion baseRot = Quaternion.LookRotation(worldOut, worldUp);
        return baseRot * Quaternion.Euler(0f, sectorRotationOffset, 0f);
    }

    // -----------------------------------------------------------------------
    // Построение рулетки из конфигурации
    // -----------------------------------------------------------------------
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

    // -----------------------------------------------------------------------
    // Размещение одного сектора
    // -----------------------------------------------------------------------
    private void PlaceSector(int startSlot, int prefabIndex, int size)
    {
        if (prefabIndex < 0 || prefabIndex >= sectorPrefabs.Count) return;
        if (startSlot + size > slotRenderers.Count) return;

        Vector3 worldPos = GetSlotRangeWorldPosition(startSlot, startSlot + size - 1);
        Quaternion worldRotation = GetSlotWorldRotation(startSlot, startSlot + size - 1);

        // Визуальный префаб
        GameObject prefab = sectorPrefabs[prefabIndex];
        BaseSector prefabSector = ResolveSector(prefab);
        GameObject visualPrefab = prefab;
        if (prefabSector?.data?.rouletteVisualPrefab != null)
            visualPrefab = prefabSector.data.rouletteVisualPrefab;

        // Parent
        Transform parent = spawnedSectorsParent != null ? spawnedSectorsParent : transform;

        // === КЛЮЧЕВОЙ МОМЕНТ ===
        // Переводим мировую позицию/поворот в локальное пространство parent.
        // Это работает ТОЛЬКО если parent имеет localScale=(1,1,1) и localRotation=(0,0,0),
        // либо если parent == wheelSpinTransform (тогда TransformPoint/InverseTransformPoint взаимно обратны).
        Vector3 localPos = parent.InverseTransformPoint(worldPos);
        Quaternion localRot = Quaternion.Inverse(parent.rotation) * worldRotation;

        GameObject obj = Instantiate(visualPrefab, parent);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = localRot;
        obj.transform.localScale = visualPrefab.transform.localScale;

        // Логика и скрытие Circle-мешей
        BaseSector sector = ResolveSector(obj);
        for (int i = startSlot; i < startSlot + size; i++)
        {
            slotAssignment[i] = prefabIndex;
            if (i < allSlots.Count && allSlots[i] != null)
            {
                allSlots[i].sector = sector;
                allSlots[i].index = i;
            }
            if (i < slotRenderers.Count && slotRenderers[i] != null)
                slotRenderers[i].enabled = false;
        }

        spawnedSectorObjects[startSlot] = obj;

        float angle = GetMidAngleDeg(startSlot, startSlot + size - 1);
        Debug.Log($"[Roulette] prefab={prefabIndex} slot={startSlot} size={size} angle={angle:F1}° worldPos={worldPos} localPos={localPos}");
    }

    // -----------------------------------------------------------------------
    // Утилиты диапазона / замены
    // -----------------------------------------------------------------------
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

        int end = Mathf.Min(start + size - 1, slotAssignment.Length - 1);
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
            Debug.LogWarning("Сектора пересекаются!");
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
            Debug.LogWarning("Не удалось поменять сектора местами");
        }
    }

    // -----------------------------------------------------------------------
    // Коллайдеры
    // -----------------------------------------------------------------------
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

    // -----------------------------------------------------------------------
    // Визуалы пустых слотов
    // -----------------------------------------------------------------------
    public void RefreshVisuals() => RefreshEmptySlots();

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

    private void ClearAllSpawnedSectors()
    {
        foreach (var kv in spawnedSectorObjects)
            if (kv.Value != null) Destroy(kv.Value);
        spawnedSectorObjects.Clear();

        foreach (var r in slotRenderers)
            if (r != null) r.enabled = true;
    }

    // -----------------------------------------------------------------------
    // Публичные обёртки
    // -----------------------------------------------------------------------
    public void RemoveSectorFromSlot(int slotIndex) => RemoveWholeSector(slotIndex);
    public void PlaceSectorAtSlot(int slotIndex, int prefabIndex) => PlaceWholeSector(slotIndex, prefabIndex);

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

    // -----------------------------------------------------------------------
    // Вспомогательные
    // -----------------------------------------------------------------------
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

    // -----------------------------------------------------------------------
    // Gizmos: пронумерованные сферы + кольцо + красная стрелка к слоту 0
    // -----------------------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (wheelSpinTransform == null) return;
        int total = slotRenderers.Count > 0 ? slotRenderers.Count : 16;

        for (int i = 0; i < total; i++)
        {
            Vector3 pos = GetSlotRangeWorldPosition(i, i);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(pos, 0.06f);

            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(pos + wheelSpinTransform.up * 0.12f, i.ToString());

            // Зелёная стрелка = направление forward сектора
            Quaternion rot = GetSlotWorldRotation(i, i);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(pos, rot * Vector3.forward * 0.25f);
        }

        // Красная линия = слот 0
        Gizmos.color = Color.red;
        Gizmos.DrawLine(wheelSpinTransform.position, GetSlotRangeWorldPosition(0, 0));

        // Жёлтое кольцо
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        int steps = 64;
        Vector3 prev = wheelSpinTransform.TransformPoint(LocalOffsetForAngle(0f));
        for (int s = 1; s <= steps; s++)
        {
            float a = s / (float)steps * 360f;
            Vector3 next = wheelSpinTransform.TransformPoint(LocalOffsetForAngle(a));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}