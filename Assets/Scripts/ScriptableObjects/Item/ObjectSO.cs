using UnityEngine;

// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/Object")]
public class ObjectSO : ItemSO {
    [Header("Place Object Settings")]
    public GameObject ObjectPrefabToPlace;
    public ItemSO ItemSOToPickUpObject;
}
