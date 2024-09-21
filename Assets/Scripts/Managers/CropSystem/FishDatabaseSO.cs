using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static FishSO;

[CreateAssetMenu(menuName = "Database/Fish Database")]
public class FishDatabaseSO : ScriptableObject {
    // List of fish in the database
    [SerializeField] private List<FishSO> _fish = new();
    // Cache to store fish by their IDs for fast lookup
    private Dictionary<int, FishSO> _cache = new();

    /// <summary>
    /// Initializes the fish in the fish database on Start() and cache all fish.
    /// </summary>
    public void InitializeFishData() {
        for (int i = 0; i < _fish.Count; i++) {
            _fish[i].FishId = i;
            _cache[i] = _fish[i]; // Populate the cache
        }
    }

    /// <summary>
    /// Indexer to access fish by their IDs from the cache
    /// </summary>
    public FishSO this[int fishId] {
        get {
            if (_cache.TryGetValue(fishId, out var fish)) {
                return fish;
            } else {
                throw new KeyNotFoundException($"Crop with ID {fishId} does not exist in the cache.");
            }
        }
    }

    public FishSO GetFish(int tileId, CatchingMethod catchingMethod) {
        var currentSeason = TimeAndWeatherManager.Instance.CurrentSeason;
        var currentTimeOfDay = TimeAndWeatherManager.Instance.CurrentTimeOfDay;
        int locationId = tileId switch {
            0 or 1 or 2 => 0,
            3 => 1,
            4 => 2,
            _ => -1
        };

        var availableFish = _fish
            .Where(fish => fish.Locations.Contains((FishLocation)locationId) &&
                           fish.Method == catchingMethod &&
                           fish.Seasons.Contains((TimeAndWeatherManager.SeasonName)currentSeason.Value) &&
                           fish.TimeOfDay.Contains(currentTimeOfDay))
            .ToList();

        if (availableFish.Count == 0) {
            Debug.LogError("No available fish");
            return null;
        }

        var fishBySize = new List<FishSO>();
        do {
            var probabilities = GetProbabilitiesForTile(tileId);
            var selectedSize = GetRandomFishSize(probabilities);

            fishBySize = availableFish.Where(fish => fish.FishSize == selectedSize).ToList();
        }
        while (fishBySize.Count == 0);
        
        return fishBySize[Random.Range(0, fishBySize.Count)];
    }
    
    private Dictionary<FishType, float> GetProbabilitiesForTile(int tileId) {
        return tileId switch {
            0 => new Dictionary<FishType, float> {
                    { FishType.VerySmall, 0.45f },
                    { FishType.Small, 0.30f },
                    { FishType.Medium, 0.20f },
                    { FishType.Large, 0.05f },
                    { FishType.VeryLarge, 0.0f },
                    { FishType.Leviathan, 0.0f }},
            1 => new Dictionary<FishType, float> {
                    { FishType.VerySmall, 0.15f },
                    { FishType.Small, 0.27f },
                    { FishType.Medium, 0.40f },
                    { FishType.Large, 0.15f },
                    { FishType.VeryLarge, 0.025f },
                    { FishType.Leviathan, 0.005f }},
            2 => new Dictionary<FishType, float> {
                    { FishType.VerySmall, 0.0f },
                    { FishType.Small, 0.10f },
                    { FishType.Medium, 0.30f },
                    { FishType.Large, 0.40f },
                    { FishType.VeryLarge, 0.15f },
                    { FishType.Leviathan, 0.05f }},
            3 => new Dictionary<FishType, float> {
                    { FishType.VerySmall, 0.4f },
                    { FishType.Small, 0.275f },
                    { FishType.Medium, 0.175f },
                    { FishType.Large, 0.09f },
                    { FishType.VeryLarge, 0.05f },
                    { FishType.Leviathan, 0.001f }},
            4 => new Dictionary<FishType, float> {
                    { FishType.VerySmall, 0.4f },
                    { FishType.Small, 0.275f },
                    { FishType.Medium, 0.175f },
                    { FishType.Large, 0.09f },
                    { FishType.VeryLarge, 0.05f },
                    { FishType.Leviathan, 0.001f }},
            _ => new Dictionary<FishType, float>(),
        };
    }

    private FishType GetRandomFishSize(Dictionary<FishType, float> probabilities) {
        float randomValue = Random.value;
        float cumulative = 0.0f;

        foreach (var entry in probabilities) {
            cumulative += entry.Value;
            if (randomValue < cumulative) {
                return entry.Key;
            }
        }

        Debug.LogError("Error while selecting the fish.");
        return probabilities.Keys.Last(); // Fallback, should not happen
    }
}
