using UnityEngine;
using System.Collections.Generic;

public class SlotPositioner : MonoBehaviour
{
    public List<Transform> graySectors = new List<Transform>();
    public List<Transform> slots = new List<Transform>();

    [ContextMenu("Copy Positions")]
    public void CopyPositions()
    {
        for (int i = 0; i < graySectors.Count && i < slots.Count; i++)
        {
            slots[i].position = graySectors[i].position;
            slots[i].rotation = graySectors[i].rotation;
        }
        Debug.Log("Позиции скопированы!");
    }
}