using UnityEngine;

public class BaseSector : MonoBehaviour
{
    public SectorData data;

    public int GetPower()
    {
        return Mathf.Max(1, data.basePower);
    }
}
