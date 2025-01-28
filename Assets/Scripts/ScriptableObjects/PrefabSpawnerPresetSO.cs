using System;
using UnityEngine;

[Serializable]
public class SpawnablePrefab {
    public GameObject Prefab;
    public bool IsGrowableTree;
    [Range(0, 1)] public float SpawnProbability;

    [ConditionalHide("IsGrowableTree", true)]
    [Header("Optional Tree Growth Settings")]
    public bool UseInitialGrowthRange;
    [ConditionalHide("IsGrowableTree", true)]
    public Vector2Int InitialGrowthTimeRange;
}

[CreateAssetMenu(menuName = "ScriptableObjects/PrefabSpawnerPreset")]
public class PrefabSpawnerPresetSO : ScriptableObject {
    public SpawnablePrefab[] SpawnablePrefabs;
    public int MinSpawnCount = 10;
    public int MaxSpawnCount = 50;
}
