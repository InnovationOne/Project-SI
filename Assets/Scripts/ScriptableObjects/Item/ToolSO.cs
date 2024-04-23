using System.Collections.Generic;
using UnityEngine;

public enum ToolTypes {
    Axe, FishingRod, Hoe, MilkingBucket, Pickaxe, Scythe, Shears, WateringCan,
}

public enum ToolRarityNames {
    none, Wood, Stone, Copper, Iron, Gold, Diamond,
}
// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/Tool")]
public class ToolSO : ItemSO {
    public List<Sprite> ToolItemRarity;

    [Header("Attack Settings")]
    public bool IsWeapon;
    [ConditionalHide("IsWeapon", true)]
    public int Damage;

    [Header("Tool Settings")]
    [ConditionalHide("IsTool", true)]
    public List<int> UsageOrDamageOnAction;
    [ConditionalHide("IsTool", true)]
    public List<int> VolumeOrBiteRate; // Watering Can and Fishing Rod only
    [ConditionalHide("IsTool", true)]
    public List<int> CatchChance; // Fishing Rod only
    [ConditionalHide("IsTool", true)]
    public List<int> EnergyOnAction;
}
