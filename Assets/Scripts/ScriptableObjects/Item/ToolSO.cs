using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ToolSO")]
public class ToolSO : ItemSO {
    public enum ToolTypes {
        Axe,
        FishingRod,
        Hoe,
        MilkingBucket,
        Pickaxe,
        Scythe,
        Shears,
        WateringCan
    }

    public enum ToolRarityNames {
        none,
        Wood,
        Stone,
        Copper,
        Iron,
        Gold,
        Diamond
    }

    public Sprite[] ToolItemRarity;

    [Header("Tool Settings")]
    public int[] EnergyOnAction;
}
