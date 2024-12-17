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
    [SerializeField] GameObject _prefab; // The prefab to spawn
    public GameObject Prefab => _prefab;

    [SerializeField, Range(0f, 1f)] float _spawnProbability; // Spawn probability for this prefab
    public float SpawnProbability => _spawnProbability;

    [Header("Optional Tree Growth Settings")]
    [SerializeField] bool _useInitialGrowthRange = false;
    public bool UseInitialGrowthRange => _useInitialGrowthRange;

    [SerializeField] Vector2Int _initialGrowthTimeRange = new Vector2Int(0, 0);
    public Vector2Int InitialGrowthTimeRange => _initialGrowthTimeRange;
}

[RequireComponent(typeof(PolygonCollider2D))]
public class PrefabSpawner : NetworkBehaviour {
    [SerializeField] Tilemap _targetTilemap;
    [SerializeField] List<SpawnablePrefab> _prefabsToSpawn = new(); 
    [SerializeField] int _minSpawnCount = 1;
    [SerializeField] int _maxSpawnCount = 5;

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

    void OnDestroy() {
        if (_polygonPoints.IsCreated) {
            _polygonPoints.Dispose();
        }
    }

    private void Update() {
        if (IsServer && Input.GetKeyDown(KeyCode.F11)) {
            Debug.Log("PrefabSpawner is spawning prefabs.");
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

    void InitializeCumulativeProbabilities() {
        int count = _prefabsToSpawn.Count;
        _cumulativeProbabilities = new float[count];
        _totalProbability = 0f;

        for (int i = 0; i < count; i++) {
            _totalProbability += _prefabsToSpawn[i].SpawnProbability;
            _cumulativeProbabilities[i] = _totalProbability;
        }
    }

    void ExtractPolygonPoints() {
        var localPoints = _spawnArea.GetPath(0);
        int pointCount = localPoints.Length;

        var worldPoints = new NativeArray<float2>(pointCount, Allocator.Temp);
        for (int i = 0; i < pointCount; i++) {
            var worldPos = _spawnArea.transform.TransformPoint(localPoints[i]);
            worldPoints[i] = new float2(worldPos.x, worldPos.y);
        }

        _polygonPoints = new NativeArray<float2>(worldPoints, Allocator.Persistent);
        worldPoints.Dispose();
    }

    void SpawnPrefabs() {
        int spawnCount = UnityEngine.Random.Range(_minSpawnCount, _maxSpawnCount + 1);
        Bounds spawnBounds = _spawnArea.bounds;

        Vector3Int minTilePosition = _targetTilemap.WorldToCell(spawnBounds.min);
        Vector3Int maxTilePosition = _targetTilemap.WorldToCell(spawnBounds.max);

        using var allTilePositions = new NativeList<Vector3Int>(Allocator.TempJob);
        for (int x = minTilePosition.x; x <= maxTilePosition.x; x++) {
            for (int y = minTilePosition.y; y <= maxTilePosition.y; y++) {
                Vector3Int tilePos = new(x, y, 0);
                if (_targetTilemap.HasTile(tilePos)) {
                    allTilePositions.Add(tilePos);
                }
            }
        }

        if (allTilePositions.Length == 0) {
            return;
        }

        NativeArray<float2> tileWorldPositions = new NativeArray<float2>(allTilePositions.Length, Allocator.TempJob);
        for (int i = 0; i < allTilePositions.Length; i++) {
            Vector3 worldPos = _targetTilemap.GetCellCenterWorld(allTilePositions[i]);
            tileWorldPositions[i] = new float2(worldPos.x, worldPos.y);
        }

        NativeArray<bool> isInsidePolygon = new NativeArray<bool>(allTilePositions.Length, Allocator.TempJob);
        var pipJob = new FindValidTilePositionsJob {
            PolygonPoints = _polygonPoints,
            TileWorldPositions = tileWorldPositions,
            IsInsidePolygon = isInsidePolygon
        }.Schedule(allTilePositions.Length, 64);

        pipJob.Complete();

        List<Vector3Int> validTilePositions = new();
        for (int i = 0; i < allTilePositions.Length; i++) {
            if (isInsidePolygon[i]) {
                validTilePositions.Add(allTilePositions[i]);
            }
        }

        tileWorldPositions.Dispose();
        isInsidePolygon.Dispose();

        if (validTilePositions.Count == 0) {
            return;
        }

        List<Vector3Int> availablePositions = CheckCollisions(validTilePositions);
        if (availablePositions.Count == 0) {
            return;
        }

        Shuffle(availablePositions);
        int positionsToUse = Mathf.Min(spawnCount, availablePositions.Count);

        List<GameObject> prefabsToInstantiate = new(positionsToUse);
        Vector3[] spawnPositions = new Vector3[positionsToUse];
        Vector3Int[] tilePositionsForCrops = new Vector3Int[positionsToUse]; // Store tile positions for CropsManager

        for (int i = 0; i < positionsToUse; i++) {
            Vector3Int tilePosition = availablePositions[i];
            GameObject selectedPrefab = GetRandomPrefab();
            if (selectedPrefab != null) {
                spawnPositions[i] = _targetTilemap.GetCellCenterWorld(tilePosition);
                tilePositionsForCrops[i] = tilePosition;
                prefabsToInstantiate.Add(selectedPrefab);
            }
        }

        for (int i = 0; i < prefabsToInstantiate.Count; i++) {
            SpawnPrefabOnNetwork(prefabsToInstantiate[i], spawnPositions[i], tilePositionsForCrops[i]);
        }
    }

    List<Vector3Int> CheckCollisions(List<Vector3Int> tilePositions) {
        List<Vector3Int> availablePositions = new();
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
                availablePositions.Add(pos);
            }
        }
        return availablePositions;
    }

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

    void Shuffle(List<Vector3Int> list) {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--) {
            int k = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    GameObject GetRandomPrefab() {
        if (_prefabsToSpawn.Count == 0) return null;
        float randomPoint = UnityEngine.Random.value * _totalProbability;
        int index = Array.BinarySearch(_cumulativeProbabilities, randomPoint);
        if (index < 0) index = ~index;
        index = Mathf.Clamp(index, 0, _prefabsToSpawn.Count - 1);
        return _prefabsToSpawn[index].Prefab;
    }

    void SpawnPrefabOnNetwork(GameObject prefab, Vector3 position, Vector3Int tilePosition) {
        if (prefab.TryGetComponent<TreeResourceNode>(out var treeResource)) {
            // Fetch seedItemId from the TreeResourceNode
            int seedItemId = treeResource.SeedToDrop.ItemId;
            var prefabData = GetPrefabData(prefab);
            int initialGrowthTimer = 0;

            // If we have an initial growth range, pick a random value and clamp it
            if (prefabData != null && prefabData.UseInitialGrowthRange) {
                initialGrowthTimer = UnityEngine.Random.Range(prefabData.InitialGrowthTimeRange.x, prefabData.InitialGrowthTimeRange.y + 1);

                // Clamp the initialGrowthTimer to CropSO's DaysToGrow
                if (ItemManager.Instance.ItemDatabase[seedItemId] is SeedSO seedSo && seedSo.CropToGrow != null) {
                    initialGrowthTimer = Mathf.Clamp(initialGrowthTimer, 0, seedSo.CropToGrow.DaysToGrow);
                }
            }

            // Directly call the CropsManager to handle seeding and spawning the tree
            CropsManager.Instance.SeedTileServerRpc(new Vector3IntSerializable(tilePosition), seedItemId, initialGrowthTimer);
        } else {
            // For non-tree prefabs, proceed with the normal network instantiation and spawning
            GameObject networkedPrefab = Instantiate(prefab, position, Quaternion.identity, transform);

            if (networkedPrefab.TryGetComponent<NetworkObject>(out var networkObject)) {
                networkObject.Spawn();
                _spawnedObjects.Add(new NetworkObjectReference(networkObject));
            } else {
                Debug.LogWarning($"Prefab {prefab.name} lacks a NetworkObject component.");
                Destroy(networkedPrefab);
            }
        }
    }

    SpawnablePrefab GetPrefabData(GameObject prefab) {
        // Find the corresponding SpawnablePrefab entry
        foreach (var sp in _prefabsToSpawn) {
            if (sp.Prefab == prefab) {
                return sp;
            }
        }
        return null;
    }
}
