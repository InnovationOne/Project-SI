using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ToolSO")]
public class ToolSO : ItemSO {
    public enum ToolTypes {
        Axe, FishingRod, Hoe, MilkingBucket, Pickaxe, Scythe, Shears, WateringCan,
    }

    public enum ToolRarityNames {
        none, Wood, Stone, Copper, Iron, Gold, Diamond,
    }

    public List<Sprite> ToolItemRarity;

    [Header("Tool Settings")]
    public List<int> UsageOrDamageOnAction;
    public List<int> VolumeOrBiteRate; // Watering Can and Fishing Rod only
    public List<int> CatchChance; // Fishing Rod only
    public List<int> EnergyOnAction;
}
