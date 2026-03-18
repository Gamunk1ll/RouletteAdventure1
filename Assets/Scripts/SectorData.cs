using UnityEngine;

public enum SectorType
{
    Attack,
    Shield,
    Heal,
    Money
}

[CreateAssetMenu(fileName = "SectorData", menuName = "Roulette/Sector")]
public class SectorData : ScriptableObject
{
    public SectorType Type;
    public int basePower;
    public int size;
    public int sellPrice;
    public int buyPrice;
    public Sprite icon;

    [Header("3D Model")]
    public GameObject visualPrefab;

    [Header("Placement")]
    public float visualRotationOffsetZ;
}