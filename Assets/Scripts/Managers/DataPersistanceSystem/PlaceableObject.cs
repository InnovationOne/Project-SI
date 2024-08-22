using System;
using UnityEngine;

[Serializable]
public class PlaceableObject {
    public int ObjectId;
    public GameObject Prefab;
    public Vector3Int Position;
    public string State;

    [Serializable]
    public class PlaceableObjectData {
        public int ObjectId;
        public Vector3 Position;
        public string State;
    }
}