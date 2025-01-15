using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static FishSO;

[CreateAssetMenu(menuName = "Database/Fish Database")]
public class FishDatabaseSO : ScriptableObject {
#if UNITY_EDITOR
    /// <summary>
    /// Custom editor for FishDatabaseSO.
    /// </summary>
    [CustomEditor(typeof(FishDatabaseSO))]
    public class FishDatabaseSOEditor : Editor {
        public override void OnInspectorGUI() {
            // Reference to the current FishDatabaseSO object
            FishDatabaseSO fishDatabase = (FishDatabaseSO)target;

            GUILayout.Space(10); // Adds some space

            if (GUILayout.Button("Start Verification")) {
                VerifyFishDatabase(fishDatabase);
            }

            // Draws the standard inspector
            DrawDefaultInspector();
        }

        /// <summary>
        /// Initiates the fish database verification process.
        /// </summary>
        private void VerifyFishDatabase(FishDatabaseSO fishDatabase) {
            const string title = "Verify Fish Database";
            const string message = "Would you like to check all FishSO in the project and add or report missing fish?";
            const string option1 = "Yes, Auto Add Missing Fish";
            const string option2 = "Yes, Report Missing Fish";
            const string option3 = "No";

            var option = EditorUtility.DisplayDialogComplex(title, message, option1, option3, option2);

            switch (option) {
                case 0:
                    fishDatabase.VerifyAllFishInDatabase(0);
                    break;
                case 2:
                    fishDatabase.VerifyAllFishInDatabase(1);
                    break;
                case 1:
                    // User chose 'No', do nothing
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Converts the _fish array to a HashSet for efficient lookups.
    /// </summary>
    private HashSet<FishSO> FishSet => new HashSet<FishSO>(_fish);

    /// <summary>
    /// Verifies all FishSO assets in the project against the database.
    /// </summary>
    /// <param name="option">0 to auto-add missing fish, 1 to report missing fish.</param>
    public void VerifyAllFishInDatabase(int option) {
        // Retrieve all FishSO asset GUIDs
        var guids = AssetDatabase.FindAssets("t:FishSO");
        var allFish = new List<FishSO>(guids.Length);

        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fish = AssetDatabase.LoadAssetAtPath<FishSO>(path);
            if (fish != null) {
                allFish.Add(fish);
            }
        }

        // Identify missing FishSO assets
        var missingFish = allFish.Where(fish => !FishSet.Contains(fish) &&
                                                fish.name != "_Template").ToList();

        if (missingFish.Count > 0) {
            switch (option) {
                case 0:
                    AddMissingFish(missingFish);
                    break;
                case 1:
                    ReportMissingFish(missingFish);
                    break;
            }
        } else {
            EditorUtility.DisplayDialog("Verification Complete", "All FishSO are included in the database.", "OK");
            Debug.Log("All FishSO are included in the database.");
        }
    }

    /// <summary>
    /// Adds missing FishSO assets to the database.
    /// </summary>
    /// <param name="missingFish">List of missing FishSO assets.</param>
    private void AddMissingFish(List<FishSO> missingFish) {
        var updatedFishList = new List<FishSO>(_fish.Length + missingFish.Count);
        updatedFishList.AddRange(_fish);
        updatedFishList.AddRange(missingFish);
        _fish = updatedFishList.ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        foreach (var fish in missingFish) {
            Debug.Log($"Fish '{fish.name}' was automatically added to the database.");
        }

        EditorUtility.DisplayDialog("Verification Complete", $"{missingFish.Count} FishSO were added to the database.", "OK");
    }

    /// <summary>
    /// Reports missing FishSO assets by logging their paths.
    /// </summary>
    /// <param name="missingFish">List of missing FishSO assets.</param>
    private void ReportMissingFish(List<FishSO> missingFish) {
        foreach (var fish in missingFish) {
            var path = AssetDatabase.GetAssetPath(fish);
            Debug.LogWarning($"Fish '{fish.name}' is not in the database and is located at: {path}");
        }

        EditorUtility.DisplayDialog("Verification Complete", $"{missingFish.Count} FishSO are missing from the database. Check the Console for details.", "OK");
    }
#endif

    // Array of fish for contiguous memory and faster access
    [SerializeField] private FishSO[] _fish = Array.Empty<FishSO>();

    // ReadOnlyDictionary for fast fish lookup by ID
    private Dictionary<int, FishSO> _cache = new();

    // Predefined probability tables for each tile
    private static readonly Dictionary<int, Dictionary<FishType, float>> PredefinedProbabilities = new()
    {
        { 0, new Dictionary<FishType, float>
            {
                { FishType.VerySmall, 0.45f },
                { FishType.Small,      0.30f },
                { FishType.Medium,     0.20f },
                { FishType.Large,      0.05f },
                { FishType.VeryLarge,  0.00f },
                { FishType.Leviathan,  0.00f }
            }
        },
        { 1, new Dictionary<FishType, float>
            {
                { FishType.VerySmall, 0.15f },
                { FishType.Small,      0.27f },
                { FishType.Medium,     0.40f },
                { FishType.Large,      0.15f },
                { FishType.VeryLarge,  0.025f },
                { FishType.Leviathan,  0.005f }
            }
        },
        { 2, new Dictionary<FishType, float>
            {
                { FishType.VerySmall, 0.00f },
                { FishType.Small,      0.10f },
                { FishType.Medium,     0.30f },
                { FishType.Large,      0.40f },
                { FishType.VeryLarge,  0.15f },
                { FishType.Leviathan,  0.05f }
            }
        },
        { 3, new Dictionary<FishType, float>
            {
                { FishType.VerySmall, 0.40f },
                { FishType.Small,      0.275f },
                { FishType.Medium,     0.175f },
                { FishType.Large,      0.125f },
                { FishType.VeryLarge,  0.025f },
                { FishType.Leviathan,  0.00f }
            }
        },
        { 4, new Dictionary<FishType, float>
            {
                { FishType.VerySmall, 0.40f },
                { FishType.Small,      0.275f },
                { FishType.Medium,     0.175f },
                { FishType.Large,      0.125f },
                { FishType.VeryLarge,  0.025f },
                { FishType.Leviathan,  0.00f }
            }
        }
    };

    // Mapping from tileId to locationId for faster access
    private static readonly Dictionary<int, int> TileIdToLocationId = new()
    {
        { 0, 0 },
        { 1, 0 },
        { 2, 0 },
        { 3, 1 },
        { 4, 2 }
    };

    /// <summary>
    /// Initializes the fish in the fish database on Start() and cache all fish.
    /// </summary>
    public void InitializeFishData() {
        _cache.Clear();
        for (int i = 0; i < _fish.Length; i++) {
            _fish[i].FishId = i;
            _cache[i] = _fish[i];
        }
    }

    /// <summary>
    /// Indexer for accessing fish by Id
    /// </summary>
    public FishSO this[int fishId] => _cache.TryGetValue(fishId, out var fish)
        ? fish
        : throw new KeyNotFoundException($"Fish with ID {fishId} does not exist in the cache.");

    /// <summary>
    /// Retrieves a fish based on the fishing conditions
    /// </summary>
    public FishSO GetFish(FishingRodToolSO fishingRod, int tileId, CatchingMethod catchingMethod) {
        var timeManager = GameManager.Instance.TimeManager;
        var currentSeason = timeManager.CurrentDate.Value.Season;
        var currentTimeOfDay = timeManager.CurrentTimeOfDay;

        // Map tileId to locationId using a precomputed dictionary for faster access
        if (!TileIdToLocationId.TryGetValue(tileId, out int locationId)) {
            Debug.LogError($"Invalid tileId: {tileId}");
            return null;
        }

        // Calculate catch chance
        int rarityId = PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().RarityId;
        float catchChance = fishingRod.CatchChance[rarityId - 1] / 100f;

        // Filter available fish without using LINQ to reduce allocations
        var availableFish = new List<FishSO>();
        for (int i = 0; i < _fish.Length; i++) {
            var fish = _fish[i];
            if (fish.Locations.Contains((FishLocation)locationId) &&
                fish.Method == catchingMethod &&
                fish.Seasons.Contains((TimeManager.SeasonName)currentSeason) &&
                fish.TimeOfDay.Contains(currentTimeOfDay)) {
                availableFish.Add(fish);
            }
        }

        if (availableFish.Count == 0) {
            Debug.LogError("No available fish");
            return null;
        }

        var probabilities = GetAdjustedProbabilitiesForTile(tileId, catchChance);
        if (probabilities.Count == 0) {
            Debug.LogError("Probabilities not found for the given tile.");
            return null;
        }

        FishSO selectedFish = null;

        // Initialize a HashSet to keep track of tried fish sizes
        var triedSizes = new HashSet<FishType>();

        while (selectedFish == null && triedSizes.Count < probabilities.Count) {
            // Create a copy of probabilities excluding tried sizes
            var remainingProbabilities = probabilities
                .Where(kvp => !triedSizes.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // If no remaining probabilities, break the loop
            if (remainingProbabilities.Count == 0) {
                Debug.LogWarning("All fish sizes have been tried.");
                break;
            }

            // Select a random fish size based on probabilities
            FishType selectedSize = GetRandomFishSize(remainingProbabilities);

            // Add the selected size to the tried sizes
            triedSizes.Add(selectedSize);

            // Select a random fish of the selected size
            var fishOfSize = new List<FishSO>();
            for (int i = 0; i < availableFish.Count; i++) {
                if (availableFish[i].FishSize == selectedSize) {
                    fishOfSize.Add(availableFish[i]);
                }
            }

            if (fishOfSize.Count > 0) {
                selectedFish = fishOfSize[UnityEngine.Random.Range(0, fishOfSize.Count)];
            }
        }

        if (selectedFish == null) {
            Debug.LogError("Failed to select a fish after maximum attempts.");
        }

        return selectedFish;
    }

    /// <summary>
    /// Adjusts the base probabilities based on the catch chance.
    /// </summary>
    private Dictionary<FishType, float> GetAdjustedProbabilitiesForTile(int tileId, float catchChance) {
        if (!PredefinedProbabilities.TryGetValue(tileId, out var baseProbabilities)) {
            return new Dictionary<FishType, float>();
        }

        var adjustedProbabilities = new Dictionary<FishType, float>(baseProbabilities.Count);
        float total = 0f;

        foreach (var kvp in baseProbabilities) {
            float adjustedValue = kvp.Value;
            if (IsRareFish(kvp.Key)) {
                adjustedValue *= (1 + catchChance); // Increase probability for rare fish
            } else {
                adjustedValue *= (1 - catchChance); // Decrease probability for common fish
            }

            // Ensure probabilities don't go negative
            adjustedValue = Mathf.Max(adjustedValue, 0f);

            adjustedProbabilities[kvp.Key] = adjustedValue;
            total += adjustedValue;
        }

        // Normalize probabilities to ensure they sum up to 1
        if (total > 0f) {
            var keys = new List<FishType>(adjustedProbabilities.Keys);
            foreach (var key in keys) {
                adjustedProbabilities[key] /= total;
            }
        }

        return adjustedProbabilities;
    }

    /// <summary>
    /// Determines if a fish type is considered rare.
    /// </summary>
    private static bool IsRareFish(FishType fishType) => fishType == FishType.VeryLarge || fishType == FishType.Leviathan;

    /// <summary>
    /// Selects a random fish size based on adjusted probabilities.
    /// </summary>
    private FishType GetRandomFishSize(Dictionary<FishType, float> probabilities) {
        float randomValue = UnityEngine.Random.value;
        float cumulative = 0f;

        foreach (var entry in probabilities) {
            cumulative += entry.Value;
            if (randomValue < cumulative) {
                return entry.Key;
            }
        }

        Debug.LogError("Failed to select a fish size based on probabilities.");
        return FishType.None; // FishType.None as a fallback
    }
}
