using System.Collections.Generic;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance;

    public List<Slot> slots = new();

    private void Awake()
    {
        Instance = this;
    }

    public bool TriggerRandomSlot()
    {
        if (slots == null || slots.Count == 0)
        {
            Debug.LogError("RouletteManager: slots list is empty.");
            return false;
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
            return false;
        }

        int index = Random.Range(0, validSlots.Count);
        validSlots[index].Activate();
        return true;
    }

}
