using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO")]
public class ObjectSO : ItemSO {
    [Header("Crafting")]
    public bool CanBeCrafted;
    [ConditionalHide("CanBeCrafted", true)]
    public List<ItemSlot> ItemsToCraftList;

    [Header("Prefab")]
    public GameObject PrefabToSpawn;
    public Sprite[] PrefabRotationSprites;

    [Header("Tile Sprite Offset")]
    public Vector3 TileSpriteOffset = Vector3.zero;

    [Header("Grid Settings")]
    public Vector2Int OccupiedSizeInCells = new Vector2Int(1, 1);
}