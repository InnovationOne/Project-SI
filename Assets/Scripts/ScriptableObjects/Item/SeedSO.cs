using UnityEngine;

// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/SeedSO")]
public class SeedSO : ItemSO {
    [Header("Seed Settings")]
    public CropSO CropToGrow;

    public int MinSeedFromFruit;
    public int MaxSeedFromFruit;
}
