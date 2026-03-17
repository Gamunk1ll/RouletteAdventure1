using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance;

    [Header("Slots")]
    public List<Slot> slots = new();

    public enum SpinAxis
    {
        LocalX,
        LocalY,
        LocalZ
    }

    [Header("Spin animation")]

    public Transform wheelRoot;
    public SpinAxis spinAxis = SpinAxis.LocalY;

    [Min(0.1f)]
    public float spinDuration = 1.6f;

    [Min(0)]
    public int minFullRotations = 3;

    [Min(0)]
    public int maxFullRotations = 6;

    private bool isSpinning;
    public bool IsSpinning => isSpinning;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator SpinVisualOnly()
    {
        if (isSpinning || wheelRoot == null)
            yield break;

        isSpinning = true;
        yield return StartCoroutine(SpinByRandomTurns());
        isSpinning = false;
    }
    public bool TriggerRandomSlot()
    {
        Slot selectedSlot = GetRandomValidSlot();
        if (selectedSlot == null)
            return false;

        selectedSlot.Activate();
        return true;
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

    private IEnumerator SpinByRandomTurns()
    {
        int minRot = Mathf.Min(minFullRotations, maxFullRotations);
        int maxRot = Mathf.Max(minFullRotations, maxFullRotations);
        int fullRotations = Random.Range(minRot, maxRot + 1);

        Vector3 baseEuler = wheelRoot.localEulerAngles;
        float startAxisAngle = GetAxisValue(baseEuler);
        float targetAxisAngle = startAxisAngle + fullRotations * 360f;

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