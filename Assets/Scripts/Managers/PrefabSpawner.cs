using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using Unity.Mathematics;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(NetworkObject))]
public class PrefabSpawner : NetworkBehaviour {
    [SerializeField] Tilemap _targetTilemap;
    [SerializeField] PrefabSpawnerPresetSO _prefabsToSpawn;

    NetworkList<NetworkObjectReference> _spawnedObjects;
    float[] _cumulativeProbabilities;
    float _totalProbability;
    PolygonCollider2D _spawnArea;
    NativeArray<float2> _polygonPoints;

    void Awake() {
        _spawnArea = GetComponent<PolygonCollider2D>();
        _spawnArea.isTrigger = true;
        _spawnedObjects = new NetworkList<NetworkObjectReference>();

        InitializeCumulativeProbabilities();
        ExtractPolygonPoints();
    }

    new void OnDestroy() {
        if (_polygonPoints.IsCreated) {
            _polygonPoints.Dispose();
        }

        base.OnDestroy();
    }

    private void Update() {
        if (IsServer && Input.GetKeyDown(KeyCode.F11)) {
            Debug.Log("PrefabSpawner is spawning prefabs.");
            SpawnPrefabs();
        }
    }

    // Clean up all spawned network objects when despawning
    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsServer && _spawnedObjects != null) {
            foreach (var objRef in _spawnedObjects) {
                if (objRef.TryGet(out NetworkObject networkObject)) {
                    networkObject.Despawn();
                }
            }
            _spawnedObjects.Clear();
        }
    }

    // Prepare cumulative probability data for random prefab selection
    void InitializeCumulativeProbabilities() {
        var prefabs = _prefabsToSpawn.SpawnablePrefabs;
        int count = _prefabsToSpawn.SpawnablePrefabs.Length;

        _cumulativeProbabilities = new float[count];
        _totalProbability = 0f;

        for (int i = 0; i < count; i++) {
            _totalProbability += prefabs[i].SpawnProbability;
            _cumulativeProbabilities[i] = _totalProbability;
        }
    }

    // Store polygon points in world space for quick point-in-polygon checks
    void ExtractPolygonPoints() {
        Vector2[] localPoints = _spawnArea.GetPath(0);
        int pointCount = localPoints.Length;

        var worldPoints = new NativeArray<float2>(pointCount, Allocator.Temp);
        for (int i = 0; i < pointCount; i++) {
            Vector3 worldPos = _spawnArea.transform.TransformPoint(localPoints[i]);
            worldPoints[i] = new float2(worldPos.x, worldPos.y);
        }

        _polygonPoints = new NativeArray<float2>(worldPoints, Allocator.Persistent);
    }

    void SpawnPrefabs() {
        int spawnCount = UnityEngine.Random.Range(_prefabsToSpawn.MinSpawnCount, _prefabsToSpawn.MaxSpawnCount + 1);
        Bounds spawnBounds = _spawnArea.bounds;

        Vector3Int minTilePosition = _targetTilemap.WorldToCell(spawnBounds.min);
        Vector3Int maxTilePosition = _targetTilemap.WorldToCell(spawnBounds.max);

        // Collect all tile positions within bounds
        using var allTilePositions = new NativeList<Vector3Int>(Allocator.TempJob);
        for (int x = minTilePosition.x; x <= maxTilePosition.x; x++) {
            for (int y = minTilePosition.y; y <= maxTilePosition.y; y++) {
                Vector3Int tilePos = new(x, y, 0);
                if (_targetTilemap.HasTile(tilePos)) {
                    allTilePositions.Add(tilePos);
                }
            }
        }

        // Early exit if no tiles found
        if (allTilePositions.Length == 0) return;

        // Convert tile positions to world space
        var tileWorldPositions = new NativeArray<float2>(allTilePositions.Length, Allocator.TempJob);
        for (int i = 0; i < allTilePositions.Length; i++) {
            Vector3 worldPos = _targetTilemap.GetCellCenterWorld(allTilePositions[i]);
            tileWorldPositions[i] = new float2(worldPos.x, worldPos.y);
        }

        // Determine which tile positions are inside the polygon
        using var isInsidePolygon = new NativeArray<bool>(allTilePositions.Length, Allocator.TempJob);
        var pipJob = new FindValidTilePositionsJob {
            PolygonPoints = _polygonPoints,
            TileWorldPositions = tileWorldPositions,
            IsInsidePolygon = isInsidePolygon
        }.Schedule(allTilePositions.Length, 64);

        pipJob.Complete();

        // Create a list of valid positions based on polygon checks
        var validTilePositions = new List<Vector3Int>();
        for (int i = 0; i < allTilePositions.Length; i++) {
            if (isInsidePolygon[i]) {
                validTilePositions.Add(allTilePositions[i]);
            }
        }

        // Early exit if no valid tiles
        if (validTilePositions.Count == 0) return;

        // Filter out tiles that contain colliders (excluding the trigger area)
        var availablePositions = CheckCollisions(validTilePositions);
        if (availablePositions.Count == 0) return;

        Shuffle(availablePositions);

        // Decide how many random positions to use for spawning
        int positionsToUse = Mathf.Min(spawnCount, availablePositions.Count);

        // Spawn prefabs at the selected positions
        for (int i = 0; i < positionsToUse; i++) {
            Vector3Int tilePosition = availablePositions[i];
            GameObject selectedPrefab = GetRandomPrefab();
            if (selectedPrefab == null) continue;

            Vector3 spawnPosition = _targetTilemap.GetCellCenterWorld(tilePosition);
            SpawnPrefabOnNetwork(selectedPrefab, spawnPosition, tilePosition);
        }
    }

    // Exclude positions that have colliders other than our own trigger
    List<Vector3Int> CheckCollisions(List<Vector3Int> tilePositions) {
        var available = new List<Vector3Int>();
        foreach (var pos in tilePositions) {
            Vector3 worldPosition = _targetTilemap.GetCellCenterWorld(pos);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.1f);

            bool hasCollision = false;
            foreach (var collider in colliders) {
                if (collider != _spawnArea && !collider.isTrigger) {
                    hasCollision = true;
                    break;
                }
            }

            if (!hasCollision) {
                available.Add(pos);
            }
        }
        return available;
    }

    // Burst-compiled job to determine if a tile center is inside the polygon
    [BurstCompile]
    struct FindValidTilePositionsJob : IJobParallelFor {
        [ReadOnly] public NativeArray<float2> PolygonPoints;
        [ReadOnly] public NativeArray<float2> TileWorldPositions;
        [WriteOnly] public NativeArray<bool> IsInsidePolygon;

        public void Execute(int index) {
            float2 point = TileWorldPositions[index];
            IsInsidePolygon[index] = IsPointInPolygon(point, PolygonPoints);
        }

        readonly bool IsPointInPolygon(float2 point, NativeArray<float2> polygon) {
            int vertexCount = polygon.Length;
            bool inside = false;

            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++) {
                float xi = polygon[i].x;
                float yi = polygon[i].y;
                float xj = polygon[j].x;
                float yj = polygon[j].y;

                bool intersects = ((yi > point.y) != (yj > point.y)) &&
                                  (point.x < (xj - xi) * (point.y - yi) / math.max((yj - yi), 1e-6f) + xi);

                if (intersects) {
                    inside = !inside;
                }
            }
            return inside;
        }
    }

    // Shuffle list in-place (Fisher–Yates)
    void Shuffle(List<Vector3Int> list) {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--) {
            int k = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    // Select a random prefab using weighted probability
    GameObject GetRandomPrefab() {
        if (_prefabsToSpawn.SpawnablePrefabs.Length == 0) return null;

        float randomPoint = UnityEngine.Random.value * _totalProbability;
        int index = Array.BinarySearch(_cumulativeProbabilities, randomPoint);
        if (index < 0) index = ~index;
        index = Mathf.Clamp(index, 0, _prefabsToSpawn.SpawnablePrefabs.Length - 1);

        return _prefabsToSpawn.SpawnablePrefabs[index].Prefab;
    }

    // Spawn networked or tree resource prefabs at a specific tile
    void SpawnPrefabOnNetwork(GameObject prefab, Vector3 position, Vector3Int tilePosition) {
        // Handle special tree logic
        if (GetPrefabData(prefab).IsGrowableTree && prefab.TryGetComponent<TreeResourceNode>(out var treeResource)) {
            int seedItemId = treeResource.SeedToDrop.ItemId;
            var prefabData = GetPrefabData(prefab);

            int initialGrowthTimer = 0;
            if (prefabData?.UseInitialGrowthRange == true) {
                initialGrowthTimer = UnityEngine.Random.Range(prefabData.InitialGrowthTimeRange.x, prefabData.InitialGrowthTimeRange.y + 1);

                // Clamp growth timer if we have a valid Crop reference
                if (GameManager.Instance.ItemManager.ItemDatabase[seedItemId] is SeedSO seedSo &&
                    seedSo.CropToGrow != null) {

                    initialGrowthTimer = Mathf.Clamp(initialGrowthTimer, 0, seedSo.CropToGrow.DaysToGrow);
                }
            }

            // Use the CropsManager to seed and spawn
            GameManager.Instance.CropsManager.SeedTileServerRpc(new Vector3IntSerializable(tilePosition), seedItemId, initialGrowthTimer);
        } else {
            // Normal prefab instantiation with networking
            GameObject networkedPrefab = Instantiate(prefab, position, Quaternion.identity, transform);

            if (networkedPrefab.TryGetComponent<NetworkObject>(out var networkObject)) {
                networkObject.Spawn();
                _spawnedObjects.Add(new NetworkObjectReference(networkObject));
            } else {
                Debug.LogWarning($"Prefab '{prefab.name}' lacks a NetworkObject component.");
                Destroy(networkedPrefab);
            }
        }
    }

    // Fetch SpawnablePrefab data for extra config
    private SpawnablePrefab GetPrefabData(GameObject prefab) {
        foreach (var sp in _prefabsToSpawn.SpawnablePrefabs) {
            if (sp.Prefab == prefab) {
                return sp;
            }
        }
        return null;
    }
}
