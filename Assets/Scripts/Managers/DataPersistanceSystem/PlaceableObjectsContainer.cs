using System.Collections.Generic;
using UnityEngine;

// This class is a container for all placeable objects
[CreateAssetMenu(menuName = "Container/Placeable Objects Container")]
public class PlaceableObjectsContainer : ScriptableObject {
    public List<PlaceableObject> placeableObjects;

    public PlaceableObject Get(Vector3Int position) {
        return placeableObjects.Find(x => x.objectPositionOnGrid == position);
    }
}
