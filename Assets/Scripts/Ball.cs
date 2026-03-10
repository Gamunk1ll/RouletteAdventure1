using UnityEngine;

public class Ball : MonoBehaviour
{
    public void TriggerSection(Slot slot) 
    {
        if(slot.sector==null)
        return;
        GameManager.Instance.ResolveSector(slot.sector);
    }
}
