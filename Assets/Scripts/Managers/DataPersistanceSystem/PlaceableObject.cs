using System;
using UnityEngine;

[Serializable]
public class PlaceableObject {
    public ItemSO placedObject;
    public Transform targetObject;
    public Vector3Int objectPositionOnGrid;
    public string objectState;

    public PlaceableObject() { }

    public PlaceableObject(ItemSO placedObject, Vector3Int objectPositionOnGrid) {
        this.placedObject = placedObject;
        this.objectPositionOnGrid = objectPositionOnGrid;
    }
}
