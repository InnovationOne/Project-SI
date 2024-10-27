using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO")]
public class ObjectSO : ItemSO {
    [Header("Crafting")]
    public List<ItemSlot> ItemsToCraftList;

    public GameObject ObjectToSpawn;
}
