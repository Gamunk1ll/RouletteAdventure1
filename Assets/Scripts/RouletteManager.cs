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

    public void TriggerRandomSlot()
    {

        int index = Random.Range(0, slots.Count);
        slots[index].Activate();
    }

}
