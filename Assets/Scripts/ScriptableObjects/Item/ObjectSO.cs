using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO")]
public class ObjectSO : ItemSO {
    public enum ObjectTypes {
        ItemProducer,
        ItemConverter,
        Chest,
        Bed,
        Fence,
        Gate,
        Sprinkler,
        Rose,
    }

    [Header("Place Object Settings")]
    public ObjectTypes ObjectType;

    [Header("Crafting")]
    public List<CraftItemSlot> ItemsToCraftList;

    [Header("Pick-Up")]
    public ItemSO ItemToPickUpObject;

    [Header("Visuals")]
    public Sprite InactiveSprite;
    public Sprite ActiveSprite;    
    public Vector2[] PolygonColliderPath = new Vector2[] {
        new(0f, 0f),
        new(0f, 0f),
        new(0f, 0f),
        new(0f, 0f)
    };

    [Serializable]
    public class CraftItemSlot {
        public ItemSO Item;
        public int Amount;
        public int RarityId;
    }
}
