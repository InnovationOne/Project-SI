using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;
using System.Diagnostics;
using System.Linq;

public class TestCropsManager : MonoBehaviour {
    public static TestCropsManager Instance;

    public Tilemap targetTilemap;
    public bool Test;

    private CropsManager cropsManager;
    private TimeManager timeManager;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        cropsManager = CropsManager.Instance;
        timeManager = TimeManager.Instance;
    }

    private void Update() {
        if (Test) {
            Test = false;
            StartCoroutine(RunAllTests());
        }
    }

    /// <summary>
    /// Runs all tests sequentially.
    /// </summary>
    private IEnumerator RunAllTests() {
        UnityEngine.Debug.Log("Starting CropsManager Tests...");

        // Clear existing crops
        cropsManager.CropTiles.Clear();
        Stopwatch stopwatch = new();
        yield return null;

        // Test 1: Plow Tiles
        UnityEngine.Debug.Log("Test 1: Plowing all plowable tiles...");
        stopwatch.Start();
        PlowAllPlowableTiles();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"PlowTiles_Test completed in {stopwatch.ElapsedMilliseconds} ms.");
        stopwatch.Reset();

        // Test 2: Seed Tiles
        UnityEngine.Debug.Log("Test 2: Seeding all plowed tiles...");
        stopwatch.Start();
        SeedAllPlowedTiles();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"SeedTiles_Test completed in {stopwatch.ElapsedMilliseconds} ms.");
        stopwatch.Reset();
        yield break;
        yield return new WaitForSeconds(2f);

        // Test 3: Water Tiles
        UnityEngine.Debug.Log("Test 3: Watering all seeded tiles...");
        stopwatch.Start();
        WaterAllSeededTiles();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"WaterTiles_Test completed in {stopwatch.ElapsedMilliseconds} ms.");
        stopwatch.Reset();
        yield return new WaitForSeconds(2f);

        // Test 4: Simulate Days
        UnityEngine.Debug.Log("Test 4: Simulating 5 days...");
        stopwatch.Start();
        for (int i = 0; i < 30; i++) {
            timeManager.StartNextDay();
            WaterAllSeededTiles();
            yield return new WaitForSeconds(2f);
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log($"StartNextDayAndWaterTiles_Test completed in {stopwatch.ElapsedMilliseconds} ms.");
        stopwatch.Reset();

        // Test 5: Harvest Crops
        UnityEngine.Debug.Log("Test 5: Harvesting all harvestable crops...");
        stopwatch.Start();
        HarvestAllHarvestableCrops();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"HarvestCrops_Test completed in {stopwatch.ElapsedMilliseconds} ms.");
        stopwatch.Reset();

        UnityEngine.Debug.Log("CropsManager Tests Completed.");
    }

    /// <summary>
    /// Plows all tiles that can be plowed according to the CropDatabase.
    /// </summary>
    private void PlowAllPlowableTiles() {
        // Iterate through the entire tilemap
        foreach (var position in targetTilemap.cellBounds.allPositionsWithin) {
            if (targetTilemap.HasTile(position)) {
                TileBase tile = targetTilemap.GetTile(position);
                if (cropsManager.TilesThatCanBePlowed.Contains(tile)) {
                    Vector3IntSerializable posSerializable = new Vector3IntSerializable(position);
                    cropsManager.PlowTilesServerRpc(new Vector3IntSerializable[] { posSerializable }, usedEnergy: 0);
                }
            }
        }
    }

    /// <summary>
    /// Seeds all plowed tiles with crops from the CropDatabase.
    /// </summary>
    private void SeedAllPlowedTiles() {
        for (int i = 0; i < 1500; i++) {
            var position = FindNextPlowedTile();
            Vector3IntSerializable posSerializable = new(position);
            //var id = Random.Range(141, 244 + 1);
            cropsManager.SeedTileServerRpc(posSerializable, 223);
        }
    }

    /// <summary>
    /// Waters all seeded tiles.
    /// </summary>
    private void WaterAllSeededTiles() {
        List<Vector3IntSerializable> positionsToWater = new List<Vector3IntSerializable>();
        foreach (var cropTile in cropsManager.CropTiles) {
            positionsToWater.Add(new Vector3IntSerializable(cropTile.CropPosition));
        }

        // Simulate watering with usedEnergy proportional to tiles
        cropsManager.WaterCropTileServerRpc(positionsToWater.ToArray(), usedEnergy: 0, default);
    }

    /// <summary>
    /// Harvests all crops that are ready to be harvested.
    /// </summary>
    private void HarvestAllHarvestableCrops() {
        foreach (var cropTile in cropsManager.CropTiles) {
            if (cropTile.IsCropHarvestable()) {
                if (cropsManager.CropDatabase[cropTile.CropId].IsTree) {
                    cropsManager.HarvestTreeServerRpc(new Vector3IntSerializable(cropTile.CropPosition), default);
                } else {
                    cropsManager.HarvestCropServerRpc(new Vector3IntSerializable(cropTile.CropPosition), default);
                }
            }
        }
    }

    /// <summary>
    /// Finds the next plowed tile in the tilemap.
    /// </summary>
    /// <returns>Position of the next plowed tile or Vector3Int.zero if none found.</returns>
    private Vector3Int FindNextPlowedTile() {
        // Iterate through the tilemap to find plowed tiles not yet seeded
        foreach (var position in targetTilemap.cellBounds.allPositionsWithin) {
            if (targetTilemap.HasTile(position)) {
                CropTile? existingCrop = cropsManager.GetCropTileAtPosition(position);
                if (!existingCrop.HasValue || existingCrop.Value.CropId == -1) {
                    return position;
                }
            }
        }

        return Vector3Int.zero;
    }
}
