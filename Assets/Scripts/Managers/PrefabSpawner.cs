using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class SpawnablePrefab {
    public GameObject prefab;
    [Range(0f, 1f)]
    public float spawnProbability;
}

[RequireComponent(typeof(Collider2D))]
public class PrefabSpawner : MonoBehaviour {
    public Collider2D spawnArea; // Bereich zum Spawnen der Prefabs
    public Tilemap targetTilemap; // Referenz zur bestehenden Tilemap
    public List<SpawnablePrefab> prefabsToSpawn; // Liste der Prefabs mit Spawn-Wahrscheinlichkeiten
    public int minSpawnCount = 1; // Minimale Anzahl zu spawnender Prefabs
    public int maxSpawnCount = 10; // Maximale Anzahl zu spawnender Prefabs

    private void Start() {
        SpawnPrefabs();
    }

    public void SpawnPrefabs() {
        // Zufällige Anzahl an Prefabs bestimmen
        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);

        // Grenzen des Collider2D erhalten
        Bounds spawnBounds = spawnArea.bounds;

        // Tilemap-Koordinaten innerhalb der Collider-Grenzen ermitteln
        Vector3Int min = targetTilemap.WorldToCell(spawnBounds.min);
        Vector3Int max = targetTilemap.WorldToCell(spawnBounds.max);

        List<Vector3Int> validTilePositions = new List<Vector3Int>();

        // Über alle Tiles innerhalb der Grenzen iterieren
        for (int x = min.x; x <= max.x; x++) {
            for (int y = min.y; y <= max.y; y++) {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);

                // Prüfen, ob ein Tile an dieser Position existiert
                if (targetTilemap.HasTile(tilePosition)) {
                    Vector3 worldPosition = targetTilemap.GetCellCenterWorld(tilePosition);

                    // Überprüfen, ob die Weltposition innerhalb des Collider2D liegt
                    if (spawnArea.OverlapPoint(worldPosition)) {
                        validTilePositions.Add(tilePosition);
                    }
                }
            }
        }

        // Liste der gültigen Positionen mischen
        Shuffle(validTilePositions);

        // Anzahl der zu verwendenden Positionen begrenzen
        int positionsToUse = Mathf.Min(spawnCount, validTilePositions.Count);

        for (int i = 0; i < positionsToUse; i++) {
            Vector3Int tilePosition = validTilePositions[i];
            GameObject selectedPrefab = GetRandomPrefab();

            if (selectedPrefab != null) {
                Vector3 spawnPosition = targetTilemap.GetCellCenterWorld(tilePosition);
                Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }

    private GameObject GetRandomPrefab() {
        float totalProbability = 0f;
        foreach (var prefab in prefabsToSpawn) {
            totalProbability += prefab.spawnProbability;
        }

        float randomPoint = Random.value * totalProbability;

        foreach (var prefab in prefabsToSpawn) {
            if (randomPoint < prefab.spawnProbability) {
                return prefab.prefab;
            } else {
                randomPoint -= prefab.spawnProbability;
            }
        }
        return null;
    }

    private void Shuffle<T>(IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
