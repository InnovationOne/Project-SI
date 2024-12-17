using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO")]
public class ObjectSO : ItemSO {
    [Header("Crafting")]
    public List<ItemSlot> ItemsToCraftList;

    public GameObject ObjectToSpawn;
    public Sprite[] ObjectRotationSprites;

    [Header("Tile Sprite Offset")]
    public Vector3 tileSpriteOffset = Vector3.zero; // Offset für das Sprite auf dem Tile
}
