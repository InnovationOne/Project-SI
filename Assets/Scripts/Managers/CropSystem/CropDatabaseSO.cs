using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Database/Crop Database")]
public class CropDatabaseSO : ScriptableObject {
    [SerializeField] List<CropSO> _crops = new();
    Dictionary<int, CropSO> _cache;

    public List<CropSO> Crops => _crops;

    // Initializes crops and caches them by ID.
    public void InitializeCrops() {
        _cache = new Dictionary<int, CropSO>(_crops.Count);
        for (int i = 0; i < _crops.Count; i++) {
            _crops[i].CropID = i;
            _cache[i] = _crops[i];
        }
    }

    // Access crops by ID.
    public CropSO this[int cropId] {
        get {
            if (_cache.TryGetValue(cropId, out var crop)) {
                return crop;
            }
            throw new KeyNotFoundException($"Crop with ID {cropId} not found.");
        }
    }
}
