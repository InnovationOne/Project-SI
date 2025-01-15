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

    [Header("Animator")]
    public bool HasBowAnimation;
    public bool HasHurtAnimation;
    public bool HasSlashAnimation;
    public bool HasSlashReverseAnimation;
    public bool HasSpellcastAnimation;
    public bool HasThrustAnimation;

    public RuntimeAnimatorController AnimatorBG;
    public RuntimeAnimatorController AnimatorFG;
}
