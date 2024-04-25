using System;
using UnityEngine;

[Serializable]
public class CropTile {
    public int CropId;
    public Vector3Int CropPosition;
    public float CurrentGrowthTimer;
    public bool IsRegrowing;
    public bool IsWatered;
    public int Damage;
    public HarvestCrop Prefab; // GameObject with HarvestCrop script and SpriteRenderer etc.
    public Vector2 SpriteRendererOffset;
    public int SpriteRendererXScale;
    public bool InGreenhouse;

    // Fertilizer
    public float GrowthTimeScaler;
    public float RegrowthTimeScaler;
    public float QualityScaler;
    public float QuantityScaler;
    public float WaterScaler;

    // Constructor to initialize a CropTile
    public CropTile() {
        ResetCropTile();
    }

    // Get the current growth stage of the crop
    public int GetCropStage(CropSO crop) {
        float growthProgress = CurrentGrowthTimer / (crop.DaysToGrow * GrowthTimeScaler);
        if (IsRegrowing && !IsCropDoneGrowing(crop)) {
            return 5;
        } else if (IsCropDoneGrowing(crop)) {
            return 4;
        } else if (growthProgress >= 0.66f) {
            return 3;
        } else if (growthProgress >= 0.33f) {
            return 2;
        } else if (CurrentGrowthTimer > 0f) {
            return 1;
        } else if (CurrentGrowthTimer == 0f) {
            return 0; // Sprouted
        } else {
            Debug.LogError($"Crop growth stage not found! CurrentGrowTimer {CurrentGrowthTimer}");
            return -1;
        }
    }

    public bool IsDead(CropTile cropTile, int maxDamage) {
        return cropTile.Damage >= maxDamage;
    }

    public bool IsCropDoneGrowing(CropSO crop) {
        return CurrentGrowthTimer >= crop.DaysToGrow * GrowthTimeScaler;
    }

    public void ResetCropTile() {
        CropId = -1;
        CurrentGrowthTimer = 0;
        IsRegrowing = false;
        Damage = 0;
        Prefab = null;
        InGreenhouse = false;
        GrowthTimeScaler = 1f;
        RegrowthTimeScaler = 1f;
        QualityScaler = 1f;
        QuantityScaler = 1f;
        WaterScaler = 0f;
    }
}

// Needed for load and save because Prefab and SpriteRenderer are not serializable
[Serializable]
public class CropTileData {
    public int CropId;
    public Vector3Int CropPosition;
    public float CurrentGrowTimer;
    public bool IsRegrowing;
    public int Damage;
    public float SpriteRendererXPosition;
    public float SpriteRendererYPosition;
    public int SpriteRendererXScale;
    public float GrowthTimeScaler;
    public float RegrowthTimeScaler;
    public float QualityScaler;
    public float QuantityScaler;
    public float WaterScaler;
}
