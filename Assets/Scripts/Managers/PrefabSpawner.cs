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
    [SerializeField] private GameObject _prefab; // Das Prefab, das gespawnt werden soll
    public GameObject Prefab => _prefab;

    [SerializeField][Range(0f, 1f)] private float _spawnProbability; // Wahrscheinlichkeit, dass dieses Prefab gespawnt wird
    public float SpawnProbability => _spawnProbability;
}

[RequireComponent(typeof(Collider2D))]
public class PrefabSpawner : NetworkBehaviour {
    [SerializeField] private Tilemap _targetTilemap; // Referenz zur Tilemap
    [SerializeField] private List<SpawnablePrefab> _prefabsToSpawn = new(); // Liste der Prefabs mit Spawn-Wahrscheinlichkeiten
    [SerializeField] private int _minSpawnCount = 1; // Minimale Anzahl von Prefabs, die gespawnt werden
    [SerializeField] private int _maxSpawnCount = 5; // Maximale Anzahl von Prefabs, die gespawnt werden

    // LayerMask für Kollisionserkennung (kann im Inspector festgelegt werden)
    [SerializeField] private LayerMask _collisionLayerMask;

    // NetworkList zur Verfolgung der auf dem Netzwerk gespawnten Objekte
    private NetworkList<NetworkObjectReference> _spawnedObjects;

    // Kumulative Wahrscheinlichkeiten für effiziente Zufallsauswahl
    private float[] _cumulativeProbabilities;
    private float _totalProbability;

    private PolygonCollider2D _spawnArea;

    // Polygon-Daten für thread-sichere Punkt-in-Polygon-Prüfungen
    private NativeArray<float2> _polygonPoints;

    private void Awake() {
        _spawnArea = GetComponent<PolygonCollider2D>();

        // Initialisiere NetworkList
        _spawnedObjects = new NetworkList<NetworkObjectReference>();

        // Initialisiere kumulative Wahrscheinlichkeiten
        InitializeCumulativeProbabilities();

        // Extrahiere Polygonpunkte und konvertiere sie in NativeArray für den Job
        ExtractPolygonPoints();
    }

    private void OnDestroy() {
        // Dispose NativeArray zur Vermeidung von Speicherlecks
        if (_polygonPoints.IsCreated) {
            _polygonPoints.Dispose();
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Stelle sicher, dass nur der Server Prefabs spawnt
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
        }
    }

    /// <summary>
    /// Initialisiert die kumulativen Wahrscheinlichkeiten für die Prefab-Auswahl.
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
    /// Extrahiert die Polygonpunkte aus dem PolygonCollider2D und speichert sie in einem NativeArray.
    /// </summary>
    private void ExtractPolygonPoints() {
        // Angenommen, der PolygonCollider2D hat einen einzigen Pfad
        Vector2[] localPoints = _spawnArea.GetPath(0);
        int pointCount = localPoints.Length;

        // Transformiere die lokalen Punkte in Weltkoordinaten
        Vector2[] worldPoints = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++) {
            worldPoints[i] = _spawnArea.transform.TransformPoint(localPoints[i]);
        }

        _polygonPoints = new NativeArray<float2>(pointCount, Allocator.Persistent);
        for (int i = 0; i < pointCount; i++) {
            _polygonPoints[i] = new float2(worldPoints[i].x, worldPoints[i].y);
        }
    }

    /// <summary>
    /// Spawnt Prefabs basierend auf den definierten Wahrscheinlichkeiten und gültigen Tile-Positionen.
    /// </summary>
    public void SpawnPrefabs() {
        int spawnCount = UnityEngine.Random.Range(_minSpawnCount, _maxSpawnCount + 1);
        Bounds spawnBounds = _spawnArea.bounds;

        Vector3Int minTilePosition = _targetTilemap.WorldToCell(spawnBounds.min);
        Vector3Int maxTilePosition = _targetTilemap.WorldToCell(spawnBounds.max);

        // Sammle alle Tile-Positionen innerhalb der Spawn-Bounds, die ein Tile haben
        List<Vector3Int> allTilePositions = new List<Vector3Int>();
        for (int x = minTilePosition.x; x <= maxTilePosition.x; x++) {
            for (int y = minTilePosition.y; y <= maxTilePosition.y; y++) {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                if (_targetTilemap.HasTile(tilePos)) {
                    allTilePositions.Add(tilePos);
                }
            }
        }

        if (allTilePositions.Count == 0) {
            Debug.LogWarning("Keine Tiles innerhalb der Spawn-Bounds gefunden.");
            return;
        }

        // Konvertiere die Tile-Positionen in Weltkoordinaten
        NativeArray<float2> tileWorldPositions = new NativeArray<float2>(allTilePositions.Count, Allocator.TempJob);
        for (int i = 0; i < allTilePositions.Count; i++) {
            Vector3 worldPos = _targetTilemap.GetCellCenterWorld(allTilePositions[i]);
            tileWorldPositions[i] = new float2(worldPos.x, worldPos.y);
        }

        // Erstelle einen NativeArray für die Ergebnisse der PIP-Prüfung
        NativeArray<bool> isInsidePolygon = new NativeArray<bool>(allTilePositions.Count, Allocator.TempJob);

        // Erstelle und plane den Job zur Überprüfung der Punkte
        var pipJob = new FindValidTilePositionsJob {
            PolygonPoints = _polygonPoints,
            TileWorldPositions = tileWorldPositions,
            IsInsidePolygon = isInsidePolygon
        }.Schedule(allTilePositions.Count, 64);

        // Warte auf den Abschluss des Jobs
        pipJob.Complete();

        // Sammle die gültigen Tile-Positionen
        List<Vector3Int> validTilePositions = new List<Vector3Int>();
        for (int i = 0; i < allTilePositions.Count; i++) {
            if (isInsidePolygon[i]) {
                validTilePositions.Add(allTilePositions[i]);
            }
        }

        // Dispose der temporären NativeArrays
        tileWorldPositions.Dispose();
        isInsidePolygon.Dispose();

        if (validTilePositions.Count == 0) {
            Debug.LogWarning("Keine gültigen Tile-Positionen innerhalb des Polygons gefunden.");
            return;
        }

        // Überprüfe, ob auf den Positionen bereits Objekte mit Collider2D vorhanden sind
        List<Vector3Int> availablePositions = new List<Vector3Int>();
        foreach (var pos in validTilePositions) {
            Vector3 worldPosition = _targetTilemap.GetCellCenterWorld(pos);
            // Verwende einen kleinen Radius, um Überlappungen zu erkennen
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.1f);
            bool hasCollision = false;

            foreach (var collider in colliders) {
                // Schließe den eigenen Collider aus
                if (collider != _spawnArea && !collider.isTrigger) {
                    hasCollision = true;
                    break;
                }
            }

            if (!hasCollision) {
                availablePositions.Add(pos);
            }
        }

        if (availablePositions.Count == 0) {
            Debug.LogWarning("Keine verfügbaren Positionen zum Spawnen gefunden (alle Positionen sind besetzt).");
            return;
        }

        // Shuffle der verfügbaren Positionen mit Fisher-Yates Algorithmus
        Shuffle(availablePositions);

        // Begrenze die Anzahl der zu verwendenden Positionen auf die Spawn-Anzahl
        int positionsToUse = Mathf.Min(spawnCount, availablePositions.Count);

        // Liste zur Sammlung der Prefabs und deren Spawn-Positionen
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

        // Instanziiere und spawne Prefabs im Netzwerk
        for (int i = 0; i < prefabsToInstantiate.Count; i++) {
            SpawnPrefabOnNetwork(prefabsToInstantiate[i], spawnPositions[i]);
        }
    }

    /// <summary>
    /// Findet gültige Tile-Positionen innerhalb der angegebenen Grenzen unter Verwendung eines Burst-kompilierten Jobs.
    /// </summary>
    [BurstCompile]
    private struct FindValidTilePositionsJob : IJobParallelFor {
        [ReadOnly] public NativeArray<float2> PolygonPoints;
        [ReadOnly] public NativeArray<float2> TileWorldPositions;
        public NativeArray<bool> IsInsidePolygon;

        public void Execute(int index) {
            float2 point = TileWorldPositions[index];
            IsInsidePolygon[index] = IsPointInPolygon(point, PolygonPoints);
        }

        /// <summary>
        /// Implementiert den Ray-Casting-Algorithmus für Punkt-in-Polygon-Tests.
        /// </summary>
        /// <param name="point">Zu testender Punkt</param>
        /// <param name="polygon">Polygon-Eckpunkte</param>
        /// <returns>True, wenn der Punkt innerhalb des Polygons liegt, sonst False</returns>
        private bool IsPointInPolygon(float2 point, NativeArray<float2> polygon) {
            int vertexCount = polygon.Length;
            bool inside = false;

            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++) {
                float xi = polygon[i].x, yi = polygon[i].y;
                float xj = polygon[j].x, yj = polygon[j].y;

                bool intersect = ((yi > point.y) != (yj > point.y)) &&
                                 (point.x < (xj - xi) * (point.y - yi) / ((yj - yi) + 1e-6f) + xi);
                if (intersect) {
                    inside = !inside;
                }
            }

            return inside;
        }
    }

    /// <summary>
    /// Mischt die Elemente in der bereitgestellten Liste mithilfe des Fisher-Yates-Algorithmus.
    /// </summary>
    /// <typeparam name="T">Typ der Elemente in der Liste</typeparam>
    /// <param name="list">Zu mischende Liste</param>
    private void Shuffle<T>(List<T> list) {
        int n = list.Count;
        if (n <= 1) return;

        for (int i = n - 1; i > 0; i--) {
            int k = UnityEngine.Random.Range(0, i + 1);
            // Elemente tauschen
            (list[k], list[i]) = (list[i], list[k]);
        }
    }

    /// <summary>
    /// Holt ein zufälliges Prefab basierend auf den vorab berechneten kumulativen Wahrscheinlichkeiten.
    /// </summary>
    /// <returns>Zufällig ausgewähltes Prefab-GameObject</returns>
    private GameObject GetRandomPrefab() {
        if (_prefabsToSpawn.Count == 0) return null;

        float randomPoint = UnityEngine.Random.value * _totalProbability;

        // Binäre Suche für effiziente Suche
        int index = Array.BinarySearch(_cumulativeProbabilities, randomPoint);
        if (index < 0) {
            index = ~index;
        }

        // Index begrenzen, um Out-of-Range-Fehler zu vermeiden
        index = Mathf.Clamp(index, 0, _prefabsToSpawn.Count - 1);
        return _prefabsToSpawn[index].Prefab;
    }

    /// <summary>
    /// Spawnt das Prefab im Netzwerk und verfolgt es mithilfe einer NetworkList.
    /// </summary>
    /// <param name="prefab">Zu spawnendes Prefab</param>
    /// <param name="position">Position, an der das Prefab gespawnt wird</param>
    private void SpawnPrefabOnNetwork(GameObject prefab, Vector3 position) {
        // Instanziere das Prefab als Netzwerkobjekt
        GameObject networkedPrefab = Instantiate(prefab, position, Quaternion.identity);
        if (networkedPrefab.TryGetComponent<NetworkObject>(out var networkObject)) {
            // Spawne das Objekt im Netzwerk
            networkObject.Spawn();

            // Verfolge das gespawnte Objekt
            _spawnedObjects.Add(new NetworkObjectReference(networkObject));
        } else {
            Debug.LogWarning($"Prefab {prefab.name} hat keine NetworkObject-Komponente.");
            Destroy(networkedPrefab);
        }
    }
}
