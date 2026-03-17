using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance;

    public List<Slot> slots = new();

    public enum SpinAxis
    {
        LocalX,
        LocalY,
        LocalZ
    }
    public Transform wheelRoot;
    public SpinAxis spinAxis = SpinAxis.LocalY;

    [Min(0.1f)] public float spinDuration = 1.6f;
    [Min(0)] public int minFullRotations = 3;
    [Min(0)] public int maxFullRotations = 6;
    [Range(0f, 0.45f)]
    public float landingRandomness = 0.3f;
    public float pointerAngleOffset = 0f;

    private bool isSpinning;
    public bool IsSpinning => isSpinning;

    private void Awake()
    {
        Instance = this;
    }

    public bool TriggerRandomSlot()
    {
        Slot selectedSlot = GetRandomValidSlot();
        if (selectedSlot == null)
            return false;

        selectedSlot.Activate();
        return true;
    }

    public IEnumerator SpinAndActivateRandomSlot()
    {
        if (isSpinning)
            yield break;

        Slot selectedSlot = GetRandomValidSlot();
        if (selectedSlot == null)
            yield break;

        if (wheelRoot != null)
        {
            isSpinning = true;
            yield return StartCoroutine(SpinToSlot(selectedSlot));
            isSpinning = false;
        }

        selectedSlot.Activate();
    }

    private Slot GetRandomValidSlot()
    {
        if (slots == null || slots.Count == 0)
        {
            Debug.LogError("RouletteManager: slots list is empty.");
            return null;
        }

        List<Slot> validSlots = new();
        foreach (Slot slot in slots)
        {
            if (slot != null)
                validSlots.Add(slot);
        }

        if (validSlots.Count == 0)
        {
            Debug.LogError("RouletteManager: no valid slots assigned.");
            return null;
        }

        return validSlots[Random.Range(0, validSlots.Count)];
    }

    private IEnumerator SpinToSlot(Slot targetSlot)
    {

        int minRot = Mathf.Min(minFullRotations, maxFullRotations);
        int maxRot = Mathf.Max(minFullRotations, maxFullRotations);

        Vector3 baseEuler = wheelRoot.localEulerAngles;
        float startAxisAngle = GetAxisValue(baseEuler);

        float slotAngle = GetSlotAngle(targetSlot);
        float randomOffset = GetRandomSectorOffset();
        int fullRotations = Random.Range(minRot, maxRot + 1);
        float targetAxisAngle = startAxisAngle
                                + fullRotations * 360f
                                + (360f - slotAngle)
                                + pointerAngleOffset
                                + randomOffset;

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            float currentAxisAngle = Mathf.Lerp(startAxisAngle, targetAxisAngle, eased);

            Vector3 euler = baseEuler;
            SetAxisValue(ref euler, currentAxisAngle);
            wheelRoot.localRotation = Quaternion.Euler(euler);

            yield return null;
        }

        Vector3 finalEuler = baseEuler;
        SetAxisValue(ref finalEuler, targetAxisAngle);
        wheelRoot.localRotation = Quaternion.Euler(finalEuler);
    }

    private float GetSlotAngle(Slot slot)
    {
        if (slot == null)
            return 0f;

        Vector3 localSlot = transform.InverseTransformPoint(slot.transform.position);
        float angle = Mathf.Atan2(localSlot.z, localSlot.x) * Mathf.Rad2Deg;
        return (angle + 360f) % 360f;
    }

    private float GetRandomSectorOffset()
    {
        if (slots == null || slots.Count == 0)
            return 0f;

        float sectorAngle = 360f / slots.Count;
        float maxOffset = sectorAngle * landingRandomness;
        return Random.Range(-maxOffset, maxOffset);
    }

    private float GetAxisValue(Vector3 euler)
    {
        switch (spinAxis)
        {
            case SpinAxis.LocalX: return euler.x;
            case SpinAxis.LocalY: return euler.y;
            default: return euler.z;
        }
    }

    private void SetAxisValue(ref Vector3 euler, float value)
    {
        switch (spinAxis)
        {
            case SpinAxis.LocalX:
                euler.x = value;
                break;
            case SpinAxis.LocalY:
                euler.y = value;
                break;
            default:
                euler.z = value;
                break;
        }
    }
}