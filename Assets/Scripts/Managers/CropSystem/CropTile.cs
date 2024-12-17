using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct CropTile : INetworkSerializable, IEquatable<CropTile> {
    public int CropId;
    public Vector3Int CropPosition;
    public float CurrentGrowthTimer;
    public bool IsRegrowing;
    public bool IsWatered;
    public int Damage;
    public Vector2 SpriteRendererOffset;
    public int SpriteRendererXScale;
    public bool InGreenhouse;
    public bool IsStruckByLightning;
    public int SeedItemId;

    public float GrowthTimeScaler;
    public float RegrowthTimeScaler;
    public float QualityScaler;
    public float QuantityScaler;
    public float WaterScaler;

    public ulong PrefabNetworkObjectId;

    public const int MAX_DAMAGE = 100;

    public enum CropStage {
        None,
        Seeded,         // Seeds are planted
        Sprouting,      // Seeds are starting to grow
        Growing,        // Plant is increasing in size and developing
        Flowering,      // Plant starts to flower
        FullyGrown,     // Plant has reached full size
        Regrowth        // Plant grows again after harvesting
    }

    // Returns the current growth stage of the crop.
    public CropStage GetCropStage(CropDatabaseSO cropDatabase) {
        if (CropId < 0 || cropDatabase == null) {
            return CropStage.None;
        }

        var cropSO = cropDatabase[CropId];
        float requiredTime = cropSO.DaysToGrow * GrowthTimeScaler;
        float growthProgress = CurrentGrowthTimer / requiredTime;

        // Check regrowth first
        if (IsRegrowing && CurrentGrowthTimer < requiredTime) return CropStage.Regrowth;
        if (CurrentGrowthTimer >= requiredTime) return CropStage.FullyGrown;
        if (growthProgress >= 0.66f) return CropStage.Flowering;
        if (growthProgress >= 0.33f) return CropStage.Growing;
        if (CurrentGrowthTimer > 0f) return CropStage.Sprouting;

        return CropStage.Seeded;
    }

    // Returns true if the crop is dead.
    public bool IsDead() => Damage >= MAX_DAMAGE;

    // Returns true if the crop is fully grown or in regrowth stage.
    public bool IsCropDoneGrowing(CropDatabaseSO cropDatabase) => GetCropStage(cropDatabase) == CropStage.FullyGrown || GetCropStage(cropDatabase) == CropStage.Regrowth;

    // Returns true if the crop is harvestable.
    public bool IsCropHarvestable(CropDatabaseSO cropDatabase) => GetCropStage(cropDatabase) == CropStage.FullyGrown && !IsDead();

    // Initializes the crop tile with default values.
    public CropTile(bool initialize = true) {
        CropId = -1;
        CropPosition = Vector3Int.zero;
        CurrentGrowthTimer = 0f;
        IsRegrowing = false;
        IsWatered = false;
        Damage = 0;
        SpriteRendererOffset = Vector2.zero;
        SpriteRendererXScale = 1;
        InGreenhouse = false;
        IsStruckByLightning = false;
        SeedItemId = -1;

        GrowthTimeScaler = 1f;
        RegrowthTimeScaler = 1f;
        QualityScaler = 1f;
        QuantityScaler = 1f;
        WaterScaler = 0f;

        PrefabNetworkObjectId = 0;
    }

    // Serializes the crop tile for network use.
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref CropId);
        serializer.SerializeValue(ref CropPosition);
        serializer.SerializeValue(ref CurrentGrowthTimer);
        serializer.SerializeValue(ref IsRegrowing);
        serializer.SerializeValue(ref IsWatered);
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref SpriteRendererOffset);
        serializer.SerializeValue(ref SpriteRendererXScale);
        serializer.SerializeValue(ref InGreenhouse);
        serializer.SerializeValue(ref IsStruckByLightning);
        serializer.SerializeValue(ref SeedItemId);
        serializer.SerializeValue(ref GrowthTimeScaler);
        serializer.SerializeValue(ref RegrowthTimeScaler);
        serializer.SerializeValue(ref QualityScaler);
        serializer.SerializeValue(ref QuantityScaler);
        serializer.SerializeValue(ref WaterScaler);
        serializer.SerializeValue(ref PrefabNetworkObjectId);
    }

    public bool Equals(CropTile other) {
        return CropId == other.CropId &&
               CropPosition.Equals(other.CropPosition) &&
               CurrentGrowthTimer.Equals(other.CurrentGrowthTimer) &&
               IsRegrowing == other.IsRegrowing &&
               IsWatered == other.IsWatered &&
               Damage == other.Damage &&
               SpriteRendererOffset.Equals(other.SpriteRendererOffset) &&
               SpriteRendererXScale == other.SpriteRendererXScale &&
               InGreenhouse == other.InGreenhouse &&
               IsStruckByLightning == other.IsStruckByLightning &&
               SeedItemId == other.SeedItemId &&
               GrowthTimeScaler.Equals(other.GrowthTimeScaler) &&
               RegrowthTimeScaler.Equals(other.RegrowthTimeScaler) &&
               QualityScaler.Equals(other.QualityScaler) &&
               QuantityScaler.Equals(other.QuantityScaler) &&
               WaterScaler.Equals(other.WaterScaler) &&
               PrefabNetworkObjectId == other.PrefabNetworkObjectId;
    }

    public override bool Equals(object obj) => obj is CropTile other && Equals(other);

    public override int GetHashCode() {
        var hash = new HashCode();
        hash.Add(CropId);
        hash.Add(CropPosition);
        hash.Add(CurrentGrowthTimer);
        hash.Add(IsRegrowing);
        hash.Add(IsWatered);
        hash.Add(Damage);
        hash.Add(SpriteRendererOffset);
        hash.Add(SpriteRendererXScale);
        hash.Add(InGreenhouse);
        hash.Add(IsStruckByLightning);
        hash.Add(SeedItemId);
        hash.Add(GrowthTimeScaler);
        hash.Add(RegrowthTimeScaler);
        hash.Add(QualityScaler);
        hash.Add(QuantityScaler);
        hash.Add(WaterScaler);
        hash.Add(PrefabNetworkObjectId);
        return hash.ToHashCode();
    }
}
