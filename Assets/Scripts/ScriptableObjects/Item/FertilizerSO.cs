using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Fertilizer")]
public class FertilizerSO : ItemSO {
    public enum FertilizerTypes {
        GrowthTime, Quality, Quantity, RegrowthTime, Water,
    }

    [Header("Fertilizer Settings")]
    public FertilizerTypes FertilizerType;
    public float FertilizerBonusValue;
    public Color FertilizerCropTileColor;
}
