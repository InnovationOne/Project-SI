using System;
using UnityEngine;

[Serializable]
public class CropTile {
    public int CropId = -1;
    public Vector3Int CropPosition;
    public float CurrentGrowthTimer = 0;
    public bool IsRegrowing = false;
    public bool IsWatered = false;
    public int Damage = 0;
    public GameObject Prefab;
    public Vector2 SpriteRendererOffset;
    public int SpriteRendererXScale = 1;
    public bool InGreenhouse = false;

    // Fertilizer
    public float GrowthTimeScaler = 1f;
    public float RegrowthTimeScaler = 1f;
    public float QualityScaler = 1f;
    public float QuantityScaler = 1f;
    public float WaterScaler = 0f;

    public enum CropStage {
        Seeded,         // Seeds are planted
        Sprouting,      // Seeds are starting to grow
        Growing,        // Plant is increasing in size and developing
        Flowering,      // Plant starts to flower
        FullyGrown,     // Plant has reached full size
        Regrowth        // Plant grows again after harvesting
    }

    /// <summary>
    /// Represents a crop tile in the game.
    /// </summary>
    public CropTile() {
        ResetCropTile();
    }

    /// <summary>
    /// Represents the growth stages of a crop.
    /// </summary>
    public CropStage GetCropStage(CropSO crop) {
        float growthProgress = CurrentGrowthTimer / (crop.DaysToGrow * GrowthTimeScaler);
        if (IsRegrowing && !IsCropDoneGrowing(crop)) {
            return CropStage.Regrowth;
        } else if (IsCropDoneGrowing(crop)) {
            return CropStage.FullyGrown;
        } else if (growthProgress >= 0.66f) {
            return CropStage.Flowering;
        } else if (growthProgress >= 0.33f) {
            return CropStage.Growing;
        } else if (CurrentGrowthTimer > 0f) {
            return CropStage.Sprouting;
        } else {
            return CropStage.Seeded;
        }
    }

    /// <summary>
    /// Checks if the crop tile is dead based on the maximum damage allowed.
    /// </summary>
    /// <param name="maxDamage">The maximum damage allowed.</param>
    /// <returns>True if the crop tile is dead, false otherwise.</returns>
    public bool IsDead(int maxDamage) => Damage >= maxDamage;

    /// <summary>
    /// Checks if the crop is done growing based on the current growth timer and the crop's growth time.
    /// </summary>
    /// <param name="crop">The crop to check.</param>
    /// <returns>True if the crop is done growing, false otherwise.</returns>
    public bool IsCropDoneGrowing(CropSO crop) => CurrentGrowthTimer >= crop.DaysToGrow * GrowthTimeScaler;

    /// <summary>
    /// Resets the crop tile to its default state.
    /// </summary>
    public void ResetCropTile() {
        CropId = -1;
        CurrentGrowthTimer = 0f;
        IsRegrowing = false;
        IsWatered = false;
        Damage = 0;
        Prefab = null;
        SpriteRendererOffset = Vector2.zero;
        SpriteRendererXScale = 1;
        InGreenhouse = false;
        GrowthTimeScaler = 1f;
        RegrowthTimeScaler = 1f;
        QualityScaler = 1f;
        QuantityScaler = 1f;
        WaterScaler = 0f;
    }
}

// Needed for load and save because Prefab is not serializable
[Serializable]
public class CropTileData {
    public int CropId;
    public Vector3 CropPosition;
    public float CurrentGrowTimer;
    public bool IsRegrowing;
    public int Damage;
    public float SpriteRendererXPosition;
    public float SpriteRendererYPosition;
    public bool InGreenhouse;
    public int SpriteRendererXScale;
    public float GrowthTimeScaler;
    public float RegrowthTimeScaler;
    public float QualityScaler;
    public float QuantityScaler;
    public float WaterScaler;
}
