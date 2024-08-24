using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/FishSO")]
public class FishSO : ScriptableObject {
    public enum FishLocation {
        Sea, River, Lake,
    }

    public enum CatchingMethod {
        FishingRod, FishTrap,
    }

    public enum FishType {
        VerySmall, Small, Medium, Large, VeryLarge, Leviathan,
    }

    [HideInInspector] public int FishId;
    [Header("Fish Params")]
    public Sprite Sprite;
    public FishLocation[] Locations;
    public CatchingMethod Method;
    public TimeAndWeatherManager.SeasonName[] Seasons;
    public TimeAndWeatherManager.TimeOfDay[] TimeOfDay;
    [TextArea]
    public string[] CatchText; // funny, pun, normal

    public FishType FishSize;
    public int FishSizeMin;
    public int FishSizeMax;

    //[Header("Breeding Pond Params")]
    //public ItemSO BreedingPondProduct;
    //public int BreedingPondProductProbability;

    //public ItemSlot Quest1;
    //public ItemSlot Quest2;
    //public ItemSlot Quest3;

    public float CalculateFishSize() {
        float mean = (FishSizeMin + FishSizeMax) / 2;
        float stdDev = (FishSizeMax - FishSizeMin) / 6; // 99.7% of values are within 3 std devs in a normal distribution

        float u1 = 1.0f - Random.Range(0.0f, 1.0f);
        float u2 = 1.0f - Random.Range(0.0f, 1.0f);

        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        float size = mean + stdDev * randStdNormal;

        return Mathf.Round(Mathf.Clamp(size, FishSizeMin, FishSizeMax) * 100f) / 100f;
    }
}
