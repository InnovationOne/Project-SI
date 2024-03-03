using System.Collections.Generic;
using UnityEngine;

public enum ItemTypes {
    Tools, Resources, Food, Crafts, Plants, Seeds, Fish, Insects, Artifacts, Minerals, none,
}

public enum WikiTypes {
    Food, ToolAndCraft, PlantAndSeed, Fish, Insect, Letter, Fossil, Mineral, Achievement,
}

public enum ItemRarityNames {
    none, Common, Rare, Epic, Legendary,
}

public enum ToolRarityNames {
    none, Wood, Stone, Copper, Iron, Gold, Diamond, 
}

public enum ToolTypes {
    Axe, FishingRod, Hoe, MilkingBucket, Pickaxe, Scythe, Shears, WateringCan,
}


// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/Item")]
public class ItemSO : ScriptableObject {
    [HideInInspector] public int ItemID;
    
    [Header("Search & Sort")]
    public ItemTypes ItemType;
    [HideInInspector] public int ItemTypeID;

    // What item type it is for sorting in the collection
    [Header("Wiki")]
    public WikiTypes WikiType;

    // Standart setting of each item    
    [Header("Basic Settings")]
    public string ItemName;
    public Sprite ItemIcon;
    public List<Sprite> ToolItemRarity;
    [HideInInspector] public readonly List<float> ItemRarityScaler = new() { 2.5f, 1.75f, 1.25f, 1.0f };

    [TextArea(6, 6)]
    public string FullDescription;
    [TextArea(6, 6)]
    public string ItemInfoText;
    [TextArea(6, 6)]
    public string FunnyText;


    [Header("Museum Settings")]
    public bool CanBeMuseum;

    [Header("Attack Settings")]
    public bool IsWeapon;
    [ConditionalHide("IsWeapon", true)]
    public int Damage;

    // When the item can be stacked
    [Header("Stackable Settings")]
    public bool IsStackable;
    [ConditionalHide("IsStackable", true)]
    public int MaxStackableAmount = 1;

    // When the item can be sold
    [Header("Money Settings")]
    public bool CanBeSold;
    [ConditionalHide("CanBeSold", true)]
    public int BuyPrice;
    [ConditionalHide("CanBeSold", true)]
    public int LowestRaritySellPrice;

    // The player uses a item to heal
    [Header("Use Settings")]
    public bool CanRestoreHpOrEnergy;
    [ConditionalHide("CanRestoreHpOrEnergy", true)]
    public int LowestRarityRestoringHpAmount;
    [ConditionalHide("CanRestoreHpOrEnergy", true)]
    public int LowestRarityRestoringEnergyAmount;

    [Header("Tool Settings")]
    public bool IsTool;
    [ConditionalHide("IsTool", true)]
    public List<ToolActionSO> OnGridAction;
    [ConditionalHide("IsTool", true)]
    public List<int> UsageOrDamageOnAction;
    [ConditionalHide("IsTool", true)]
    public List<int> VolumeOrBiteRate; // Watering Can and Fishing Rod only
    [ConditionalHide("IsTool", true)]
    public List<int> CatchChance; // Fishing Rod only
    [ConditionalHide("IsTool", true)]
    public List<int> EnergyOnAction;

    [Header("Crop Settings")]
    public bool IsSeed;
    [ConditionalHide("IsSeed", true)]
    public CropSO CropToGrow;

    [Header("Place Object Settings")]
    public bool IsPlaceableObject;
    [ConditionalHide("IsPlaceableObject", true)]
    public GameObject ObjectPrefabToPlace;
    [ConditionalHide("IsPlaceableObject", true)]
    public ItemSO ItemSOToPickUpObject;
}
