using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/FishSO")]
public class FishSO : FoodSO {
    public enum FishLocation {
        SurfaceLake, SurfaceRiver, Coast, Ocean, DeepOcean, none,
    }

    public enum CatchingMethod {
        FishingRod, FishTrap, none,
    }

    public enum FishType {
        VerySmall, Small, Medium, Large, VeryLarge, Leviathan, none,
    }


    [Header("Fish Params")]
    public FishLocation Location;
    public CatchingMethod Method;
    public TimeAndWeatherManager.SeasonName[] Seasons;
    public TimeAndWeatherManager.TimeOfDay[] TimeOfDay;
    [TextArea]
    public string CatchingText;

    public FishType FishSize;
    public int FishSizeMin;
    public int FishSizeMax;

    [Header("Breeding Pond Params")]
    public ItemSO BreedingPondProduct;
    public int BreedingPondProductProbability;

    public ItemSlot Quest1;
    public ItemSlot Quest2;
    public ItemSlot Quest3;

    public float CalculateFishSize() {
        float mean = (FishSizeMin + FishSizeMax) / 2;
        float stdDev = (FishSizeMax - FishSizeMin) / 6; // 99.7% of values are within 3 std devs in a normal distribution

        float u1 = 1.0f - Random.Range(0.0f, 1.0f);
        float u2 = 1.0f - Random.Range(0.0f, 1.0f);

        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        float size = mean + stdDev * randStdNormal;

        return Mathf.Clamp(size, FishSizeMin, FishSizeMax);
    }
}
