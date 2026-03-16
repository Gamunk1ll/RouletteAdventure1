using UnityEngine;

public class Slot : MonoBehaviour
{
    public int index;
    public BaseSector sector;
    public Renderer visual;

    public void Highlight()
    {
        if (visual != null)
        {
            visual.material.color = Color.white;
        }
    }

    public void Activate()
    {
        Highlight();

        SectorSelector selector = FindObjectOfType<SectorSelector>();
        if (selector != null && selector.IsEditMode())
        {
            selector.SelectSlot(index);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("Slot: GameManager.Instance is null.");
            return;
        }

        if (sector == null)
        {
            Debug.Log($"Slot {index} - EMPTY");
            GameManager.Instance.ResolveEmptySlot();
            return;
        }

        Debug.Log($"Slot {index} - {sector.data.Type} | Power: {sector.GetPower()}");
        GameManager.Instance.ResolveSector(sector);
    }

    void OnMouseDown()
    {
        Activate();
    }
}