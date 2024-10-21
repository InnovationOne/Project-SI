using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Database/Crop Database")]
public class CropDatabaseSO : ScriptableObject {
    // List of crops in the database
    [SerializeField] private List<CropSO> _crops = new();
    // Cache to store crops by their IDs for fast lookup
    private Dictionary<int, CropSO> _cache = new();
    public List<CropSO> Crops => _crops;

    /// <summary>
    /// Initializes the crops in the crop database on Start() and cache all crops.
    /// </summary>
    public void InitializeCrops() {
        for (int i = 0; i < _crops.Count; i++) {
            _crops[i].CropID = i;
            _cache[i] = _crops[i]; // Populate the cache
        }
    }

    /// <summary>
    /// Indexer to access crops by their IDs from the cache
    /// </summary>
    public CropSO this[int cropId] {
        get {
            if (_cache.TryGetValue(cropId, out var crop)) {
                return crop;
            } else {
                throw new KeyNotFoundException($"Crop with ID {cropId} does not exist in the cache.");
            }
        }
    }
}
