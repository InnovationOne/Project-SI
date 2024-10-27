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

    /// <summary>
    /// Represents the growth stages of a crop.
    /// </summary>
    public readonly CropStage GetCropStage() {
        if (CropsManager.Instance == null) {
            return CropStage.None;
        }
        CropSO cropSO = CropsManager.Instance.CropDatabase[CropId];

        float growthProgress = CurrentGrowthTimer / (cropSO.DaysToGrow * GrowthTimeScaler);

        if (IsRegrowing && !(CurrentGrowthTimer >= cropSO.DaysToGrow * GrowthTimeScaler)) {
            return CropStage.Regrowth;
        } else if (CurrentGrowthTimer >= cropSO.DaysToGrow * GrowthTimeScaler) {
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
    /// <returns>True if the crop tile is dead, false otherwise.</returns>
    public readonly bool IsDead() => Damage >= MAX_DAMAGE;

    /// <summary>
    /// Checks if the crop is done growing based on the current growth timer and the crop's growth time.
    /// </summary>
    /// <returns>True if the crop is done growing, false otherwise.</returns>
    public readonly bool IsCropDoneGrowing() => GetCropStage() == CropStage.FullyGrown || GetCropStage() == CropStage.Regrowth;

    /// <summary>
    /// Checks if the crop is harvestable.
    /// </summary>
    /// <returns>True if harvestable, false otherwise.</returns>
    public readonly bool IsCropHarvestable() => GetCropStage() == CropStage.FullyGrown && !IsDead();

    /// <summary>
    /// Initializes the CropTile to its default state.
    /// </summary>
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

    /// <summary>
    /// Serializes the CropTile for network transmission.
    /// </summary>
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

    /// <summary>
    /// Serializes the CropTile for network transmission.
    /// </summary>
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

    public override bool Equals(object obj) {
        return obj is CropTile other && Equals(other);
    }

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
