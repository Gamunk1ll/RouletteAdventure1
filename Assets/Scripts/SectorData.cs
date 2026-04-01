using UnityEngine;

public enum SectorType
{
    Attack,
    Shield,
    Heal,
    Money
}

public enum ShopItemKind
{
    Sector,
    Turret,
    Ball
}

[CreateAssetMenu(fileName = "SectorData", menuName = "Roulette/Sector")]
public class SectorData : ScriptableObject
{
    public SectorType Type;
    public ShopItemKind shopItemKind = ShopItemKind.Sector;
    public int basePower;
    public int size;
    public int sellPrice;
    public int buyPrice;
    public Sprite icon;

    [Header("3D Model")]
    [Tooltip("Legacy/default world visual. Used as fallback if a specific shop/roulette prefab is not assigned.")]
    public GameObject visualPrefab;
    [Tooltip("Visual prefab for shop world offers.")]
    public GameObject shopVisualPrefab;
    [Tooltip("Visual prefab for sectors spawned on the roulette wheel.")]
    public GameObject rouletteVisualPrefab;

    [Header("Placement")]
    public float visualRotationOffsetZ;
}
