using System;
using System.Collections.Generic;
using UnityEngine;

// This class contains information of the placed item
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

// This class is a container for all placeable objects
[CreateAssetMenu(menuName = "Container/Placeable Objects Container")]
public class PlaceableObjectsContainer : ScriptableObject {
    public List<PlaceableObject> placeableObjects;

    public PlaceableObject Get(Vector3Int position) {
        return placeableObjects.Find(x => x.objectPositionOnGrid == position);
    }
}
