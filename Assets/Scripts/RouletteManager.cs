using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance;

    public List<Slot> slots = new();

    [Header("Spin animation")]
    public Transform wheelRoot;
    public float spinDuration = 1.6f;
    public int minFullRotations = 3;
    public int maxFullRotations = 6;
    [Range(0f, 0.45f)] public float landingRandomness = 0.3f;

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
        {
            return false;
        }

        selectedSlot.Activate();
        return true;
    }

    public IEnumerator SpinAndActivateRandomSlot()
    {
        if (isSpinning)
        {
            yield break;
        }

        Slot selectedSlot = GetRandomValidSlot();
        if (selectedSlot == null)
        {
            yield break;
        }

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
            {
                validSlots.Add(slot);
            }
        }

        if (validSlots.Count == 0)
        {
            Debug.LogError("RouletteManager: no valid slots assigned.");
            return null;
        }

        int index = Random.Range(0, validSlots.Count);
        return validSlots[index];
    }

    private IEnumerator SpinToSlot(Slot targetSlot)
    {
        float startAngle = wheelRoot.eulerAngles.z;
        float slotAngle = GetSlotAngle(targetSlot);
        float randomOffset = GetRandomSectorOffset();
        int fullRotations = Random.Range(minFullRotations, maxFullRotations + 1);

        float targetAngle = startAngle + fullRotations * 360f + (360f - slotAngle) + randomOffset;

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, eased);
            wheelRoot.rotation = Quaternion.Euler(0f, 0f, currentAngle);
            yield return null;
        }

        wheelRoot.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    private float GetSlotAngle(Slot slot)
    {
        if (slot == null)
        {
            return 0f;
        }

        Vector3 localSlot = transform.InverseTransformPoint(slot.transform.position);
        float angle = Mathf.Atan2(localSlot.y, localSlot.x) * Mathf.Rad2Deg;
        return (angle + 360f) % 360f;
    }

    private float GetRandomSectorOffset()
    {
        if (slots == null || slots.Count == 0)
        {
            return 0f;
        }

        float sectorAngle = 360f / slots.Count;
        float maxOffset = sectorAngle * landingRandomness;
        return Random.Range(-maxOffset, maxOffset);
    }
}
