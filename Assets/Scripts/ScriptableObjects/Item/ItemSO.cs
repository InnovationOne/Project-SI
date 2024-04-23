using System.Collections.Generic;
using UnityEngine;

public enum ItemTypes {
    Tools, Resources, Food, Crafts, Plants, Seeds, Fertilizer, Fish, Insects, Artifacts, Minerals, none,
}

public enum ItemRarityNames {
    none, Common, Rare, Epic, Legendary,
}

// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/Item")]
public class ItemSO : ScriptableObject {
    [HideInInspector] public int ItemID;
    
    [Header("Search & Sort")]
    public ItemTypes ItemType;
    [HideInInspector] public int ItemTypeID;

    // Standart setting of each item    
    [Header("Basic Settings")]
    public string ItemName;
    public Sprite ItemIcon;    
    [TextArea(6, 6)]

    // When the item can be stacked
    [Header("Stackable Settings")]
    public bool IsStackable;
    [ConditionalHide("IsStackable", true)]
    public int MaxStackableAmount = 1;

    // When the item can be sold
    [Header("Money Settings")]
    public bool CanBeBought;
    [ConditionalHide("CanBeBought", true)]
    public int BuyPrice;
    public bool CanBeSold;
    [ConditionalHide("CanBeSold", true)]
    public int LowestRaritySellPrice;

    [Header("Action Settings")]
    public string ItemInfoHoverText;
    public List<ToolActionSO> LeftClickAction;
}
