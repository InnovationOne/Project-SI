using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;
using System.Diagnostics;
using System.Linq;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class TestCropsManager : NetworkBehaviour {
    public static TestCropsManager Instance;

    public Tilemap targetTilemap;
    public bool Test;
    private bool _test;

    private CropsManager cropsManager;
    private TimeManager timeManager;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        cropsManager = GameManager.Instance.CropsManager;
        timeManager = GameManager.Instance.TimeManager;
        _test = Test;
    }

    private void Update() {
        if (_test) {
            _test = false;
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
        yield return null;

        // Test 1: Plow Tiles
        UnityEngine.Debug.Log("Test 1: Plowing all plowable tiles...");
        PlowAllPlowableTiles();

        // Test 2: Seed Tiles
        UnityEngine.Debug.Log("Test 2: Seeding all plowed tiles...");
        SeedAllPlowedTiles();

        // Test 3: Water Tiles
        UnityEngine.Debug.Log("Test 3: Watering all seeded tiles...");
        WaterAllSeededTiles();

        // Test 4: Simulate Days
        UnityEngine.Debug.Log("Test 4: Simulating 5 days...");
        for (int i = 0; i < 6; i++) {
            cropsManager.TestOnNextDayStarted();
            WaterAllSeededTiles();
            yield return new WaitForSeconds(.5f);
        }

        // Test 5: Harvest Crops
        UnityEngine.Debug.Log("Test 5: Destroy");
        //HarvestAllHarvestableCrops();
        PickaxeAllCrops();
        yield return new WaitForSeconds(3f);
        PickaxeAllCrops();


        UnityEngine.Debug.Log("Test 6: Scythe Seed");
        yield return new WaitForSeconds(5f);
        PlowAllPlowableTiles();
        SeedAllPlowedTiles();
        ScytheAllCrops();


        UnityEngine.Debug.Log("Test 7: Scythe harvestable Plant");
        yield return new WaitForSeconds(5f);
        PlowAllPlowableTiles();
        SeedAllPlowedTiles();
        WaterAllSeededTiles();
        for (int i = 0; i < 6; i++) {
            cropsManager.TestOnNextDayStarted();
            WaterAllSeededTiles();
            yield return new WaitForSeconds(.5f);
        }

        UnityEngine.Debug.Log("Test 8: Scythe Plant");
        yield return new WaitForSeconds(5f);
        PlowAllPlowableTiles();
        SeedAllPlowedTiles();
        WaterAllSeededTiles();
        for (int i = 0; i < 3; i++) {
            cropsManager.TestOnNextDayStarted();
            WaterAllSeededTiles();
            yield return new WaitForSeconds(.5f);
        }
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
        for (int i = 0; i < 2; i++) {
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
            if (cropTile.CropId != -1 && cropTile.IsCropHarvestable(cropsManager.CropDatabase)) {
                if (cropsManager.CropDatabase[cropTile.CropId].IsTree) {
                    cropsManager.HarvestTreeServerRpc(new Vector3IntSerializable(cropTile.CropPosition), default);
                } else {
                    cropsManager.HarvestCropServerRpc(new Vector3IntSerializable(cropTile.CropPosition), default);
                }
            }
        }
    }

    private void PickaxeAllCrops() {
        foreach (var cropTile in cropsManager.CropTiles) {
            cropsManager.DestroyCropTileServerRpc(new Vector3IntSerializable(cropTile.CropPosition), 0, ToolSO.ToolTypes.Pickaxe);
        }
    }

    private void ScytheAllCrops() {
        foreach (var cropTile in cropsManager.CropTiles) {
            cropsManager.DestroyCropTileServerRpc(new Vector3IntSerializable(cropTile.CropPosition), 0, ToolSO.ToolTypes.Scythe);
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
