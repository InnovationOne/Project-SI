using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using Unity.Mathematics;

[Serializable]
public class SpawnablePrefab {
    [SerializeField] private GameObject _prefab; // The prefab to spawn
    public GameObject Prefab => _prefab;

    [SerializeField, Range(0f, 1f)] private float _spawnProbability; // Spawn probability for this prefab
    public float SpawnProbability => _spawnProbability;
}

[RequireComponent(typeof(Collider2D), typeof(PolygonCollider2D))]
public class PrefabSpawner : NetworkBehaviour {
    [SerializeField] private Tilemap _targetTilemap; // Reference to the Tilemap
    [SerializeField] private List<SpawnablePrefab> _prefabsToSpawn = new(); // List of prefabs with spawn probabilities
    [SerializeField] private int _minSpawnCount = 1; // Minimum number of prefabs to spawn
    [SerializeField] private int _maxSpawnCount = 5; // Maximum number of prefabs to spawn

    // Removed LayerMask as per user request

    private NetworkList<NetworkObjectReference> _spawnedObjects;

    private float[] _cumulativeProbabilities;
    private float _totalProbability;

    private PolygonCollider2D _spawnArea;

    private NativeArray<float2> _polygonPoints;

    private void Awake() {
        _spawnArea = GetComponent<PolygonCollider2D>();
        _spawnedObjects = new NetworkList<NetworkObjectReference>();
        InitializeCumulativeProbabilities();
        ExtractPolygonPoints();
    }

    private void OnDestroy() {
        // Dispose NativeArray to prevent memory leaks
        if (_polygonPoints.IsCreated) {
            _polygonPoints.Dispose();
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Initialize NetworkList on network spawn to ensure synchronization
        if (IsServer) {
            SpawnPrefabs();
        }
    }

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

    /// <summary>
    /// Initializes the cumulative probabilities for prefab selection.
    /// </summary>
    private void InitializeCumulativeProbabilities() {
        int count = _prefabsToSpawn.Count;
        _cumulativeProbabilities = new float[count];
        _totalProbability = 0f;

        for (int i = 0; i < count; i++) {
            _totalProbability += _prefabsToSpawn[i].SpawnProbability;
            _cumulativeProbabilities[i] = _totalProbability;
        }
    }

    /// <summary>
    /// Extracts polygon points from the PolygonCollider2D and stores them in a NativeArray.
    /// </summary>
    private void ExtractPolygonPoints() {
        // Assume the PolygonCollider2D has a single path
        Vector2[] localPoints = _spawnArea.GetPath(0);
        int pointCount = localPoints.Length;

        // Transform local points to world coordinates
        NativeArray<float2> worldPoints = new NativeArray<float2>(pointCount, Allocator.Temp);
        for (int i = 0; i < pointCount; i++) {
            Vector3 worldPos = _spawnArea.transform.TransformPoint(localPoints[i]);
            worldPoints[i] = new float2(worldPos.x, worldPos.y);
        }

        // Copy to _polygonPoints for persistent usage
        _polygonPoints = new NativeArray<float2>(worldPoints, Allocator.Persistent);
        worldPoints.Dispose();
    }

    /// <summary>
    /// Spawns prefabs based on defined probabilities and valid tile positions.
    /// </summary>
    private void SpawnPrefabs() {
        int spawnCount = UnityEngine.Random.Range(_minSpawnCount, _maxSpawnCount + 1);
        Bounds spawnBounds = _spawnArea.bounds;

        Vector3Int minTilePosition = _targetTilemap.WorldToCell(spawnBounds.min);
        Vector3Int maxTilePosition = _targetTilemap.WorldToCell(spawnBounds.max);

        // Collect all tile positions within spawn bounds that have a tile
        NativeList<Vector3Int> allTilePositions = new NativeList<Vector3Int>(Allocator.TempJob);
        for (int x = minTilePosition.x; x <= maxTilePosition.x; x++) {
            for (int y = minTilePosition.y; y <= maxTilePosition.y; y++) {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                if (_targetTilemap.HasTile(tilePos)) {
                    allTilePositions.Add(tilePos);
                }
            }
        }

        if (allTilePositions.Length == 0) {
            Debug.LogWarning("No tiles found within spawn bounds.");
            allTilePositions.Dispose();
            return;
        }

        // Convert tile positions to world coordinates
        NativeArray<float2> tileWorldPositions = new NativeArray<float2>(allTilePositions.Length, Allocator.TempJob);
        for (int i = 0; i < allTilePositions.Length; i++) {
            Vector3 worldPos = _targetTilemap.GetCellCenterWorld(allTilePositions[i]);
            tileWorldPositions[i] = new float2(worldPos.x, worldPos.y);
        }

        // Perform point-in-polygon test using Burst-compiled job
        NativeArray<bool> isInsidePolygon = new NativeArray<bool>(allTilePositions.Length, Allocator.TempJob);

        var pipJob = new FindValidTilePositionsJob {
            PolygonPoints = _polygonPoints,
            TileWorldPositions = tileWorldPositions,
            IsInsidePolygon = isInsidePolygon
        }.Schedule(allTilePositions.Length, 64);

        pipJob.Complete();

        // Collect valid tile positions inside the polygon
        List<Vector3Int> validTilePositions = new List<Vector3Int>();
        for (int i = 0; i < allTilePositions.Length; i++) {
            if (isInsidePolygon[i]) {
                validTilePositions.Add(allTilePositions[i]);
            }
        }

        // Dispose temporary NativeArrays
        tileWorldPositions.Dispose();
        isInsidePolygon.Dispose();
        allTilePositions.Dispose();

        if (validTilePositions.Count == 0) {
            Debug.LogWarning("No valid tile positions found within the polygon.");
            return;
        }

        // Check for collisions on the main thread
        List<Vector3Int> availablePositions = CheckCollisions(validTilePositions);

        if (availablePositions.Count == 0) {
            Debug.LogWarning("No available positions to spawn (all positions are occupied).");
            return;
        }

        // Shuffle available positions using Fisher-Yates algorithm
        Shuffle(availablePositions);

        // Determine the number of positions to use based on spawn count
        int positionsToUse = Mathf.Min(spawnCount, availablePositions.Count);

        // Prepare spawn positions and prefabs
        List<GameObject> prefabsToInstantiate = new List<GameObject>(positionsToUse);
        Vector3[] spawnPositions = new Vector3[positionsToUse];

        for (int i = 0; i < positionsToUse; i++) {
            Vector3Int tilePosition = availablePositions[i];
            GameObject selectedPrefab = GetRandomPrefab();

            if (selectedPrefab != null) {
                spawnPositions[i] = _targetTilemap.GetCellCenterWorld(tilePosition);
                prefabsToInstantiate.Add(selectedPrefab);
            }
        }

        // Instantiate and spawn prefabs in the network
        for (int i = 0; i < prefabsToInstantiate.Count; i++) {
            SpawnPrefabOnNetwork(prefabsToInstantiate[i], spawnPositions[i]);
        }
    }

    /// <summary>
    /// Checks for collisions at the given tile positions.
    /// This method runs on the main thread since Unity's Physics2D API is not thread-safe.
    /// </summary>
    /// <param name="tilePositions">List of tile positions to check for collisions.</param>
    /// <returns>List of available positions without collisions.</returns>
    private List<Vector3Int> CheckCollisions(List<Vector3Int> tilePositions) {
        List<Vector3Int> availablePositions = new List<Vector3Int>();

        foreach (var pos in tilePositions) {
            Vector3 worldPosition = _targetTilemap.GetCellCenterWorld(pos);
            // Use a small radius to detect nearby colliders
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.1f);

            bool hasCollision = false;

            foreach (var collider in colliders) {
                // Exclude the spawn area collider and trigger colliders
                if (collider != _spawnArea && !collider.isTrigger) {
                    hasCollision = true;
                    break;
                }
            }

            if (!hasCollision) {
                availablePositions.Add(pos);
            }
        }

        return availablePositions;
    }

    /// <summary>
    /// Burst-compiled job to perform point-in-polygon tests.
    /// </summary>
    [BurstCompile]
    private struct FindValidTilePositionsJob : IJobParallelFor {
        [ReadOnly] public NativeArray<float2> PolygonPoints;
        [ReadOnly] public NativeArray<float2> TileWorldPositions;
        [WriteOnly] public NativeArray<bool> IsInsidePolygon;

        public void Execute(int index) {
            float2 point = TileWorldPositions[index];
            IsInsidePolygon[index] = IsPointInPolygon(point, PolygonPoints);
        }

        /// <summary>
        /// Implements the Ray-Casting algorithm for point-in-polygon tests.
        /// </summary>
        private bool IsPointInPolygon(float2 point, NativeArray<float2> polygon) {
            int vertexCount = polygon.Length;
            bool inside = false;

            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++) {
                float xi = polygon[i].x, yi = polygon[i].y;
                float xj = polygon[j].x, yj = polygon[j].y;

                bool intersect = ((yi > point.y) != (yj > point.y)) &&
                                 (point.x < (xj - xi) * (point.y - yi) / math.max((yj - yi), 1e-6f) + xi);
                if (intersect) {
                    inside = !inside;
                }
            }

            return inside;
        }
    }

    /// <summary>
    /// Shuffles the elements in the provided list using the Fisher-Yates algorithm.
    /// </summary>
    /// <param name="list">The list to shuffle.</param>
    private void Shuffle(List<Vector3Int> list) {
        int n = list.Count;
        if (n <= 1) return;

        for (int i = n - 1; i > 0; i--) {
            int k = UnityEngine.Random.Range(0, i + 1);
            // Swap elements
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    /// <summary>
    /// Retrieves a random prefab based on precomputed cumulative probabilities.
    /// </summary>
    /// <returns>A randomly selected prefab GameObject.</returns>
    private GameObject GetRandomPrefab() {
        if (_prefabsToSpawn.Count == 0) return null;

        float randomPoint = UnityEngine.Random.value * _totalProbability;

        // Binary search for efficient lookup
        int index = Array.BinarySearch(_cumulativeProbabilities, randomPoint);
        if (index < 0) {
            index = ~index;
        }

        // Clamp index to prevent out-of-range errors
        index = Mathf.Clamp(index, 0, _prefabsToSpawn.Count - 1);
        return _prefabsToSpawn[index].Prefab;
    }

    /// <summary>
    /// Spawns the prefab in the network, sets it as a child of this GameObject, and tracks it using a NetworkList.
    /// </summary>
    /// <param name="prefab">The prefab to spawn.</param>
    /// <param name="position">The position to spawn the prefab at.</param>
    private void SpawnPrefabOnNetwork(GameObject prefab, Vector3 position) {
        // Instantiate the prefab as a networked object and set its parent to this GameObject
        GameObject networkedPrefab = Instantiate(prefab, position, Quaternion.identity, transform);

        if (networkedPrefab.TryGetComponent<NetworkObject>(out var networkObject)) {
            // Spawn the object on the network
            networkObject.Spawn();

            // Track the spawned object
            _spawnedObjects.Add(new NetworkObjectReference(networkObject));
        } else {
            Debug.LogWarning($"Prefab {prefab.name} lacks a NetworkObject component.");
            Destroy(networkedPrefab);
        }
    }
}
