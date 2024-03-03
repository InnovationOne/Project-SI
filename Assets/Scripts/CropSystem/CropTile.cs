using System;
using UnityEngine;

[Serializable]
public class CropTile {
    public int CropId;
    public Vector3Int CropPosition;
    public int CurrentGrowTimer;
    public bool IsRegrowing;
    public bool IsWatered;
    public int Damage;
    public HarvestCrop Prefab;

    // ### Maybe delete later, since its not needed at runtime ###
    public SpriteRenderer SpriteRenderer;
    public Vector3 SpriteRendererPosition;
    public Vector3Int SpriteRendererScale;


    // Constructor to initialize a CropTile
    public CropTile() {
        ResetCropTile();
    }

    // Get the current growth stage of the crop
    public int GetCropStage(CropSO crop) {
        if (IsRegrowing && !IsCropDoneGrowing(crop)) {
            return 5;
        } else if (CurrentGrowTimer == 0) {
            return 0; // Seeded
        } else {
            for (int i = 0; i < crop.TimeGrowthStages.Count; i++) {
                if (CurrentGrowTimer < crop.TimeGrowthStages[i]) {
                    return i + 1;
                }
            }

            return 4;
        }
    }

    public bool IsDead(CropTile cropTile, int maxDamage) {
        return cropTile.Damage >= maxDamage;
    }

    // Check if the crop is done growing
    public bool IsCropDoneGrowing(CropSO crop) {
        return CurrentGrowTimer >= crop.DaysToGrow;
    }

    // Reset the CropTile to its initial state
    public void ResetCropTile() {
        CropId = -1;
        CurrentGrowTimer = 0;
        IsRegrowing = false;
        Damage = 0;
        SpriteRendererPosition = Vector3.zero;
        SpriteRendererScale = Vector3Int.one;
    }
}

// Needed for load and save because Prefab and SpriteRenderer are not serializable
[Serializable]
public class CropTileData {
    public int CropId;
    public Vector3Int CropPosition;
    public int CurrentGrowTimer;
    public bool IsRegrowing;
    public int Damage;
    public float SpriteRendererXPosition;
    public float SpriteRendererYPosition;
    public int SpriteRendererXScale;
}
