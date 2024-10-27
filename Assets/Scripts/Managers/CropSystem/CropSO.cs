using System.Collections.Generic;
using UnityEngine;

// This script contains information used in a crop
[CreateAssetMenu(menuName = "Scriptable Objects/Crop")]
public class CropSO : ScriptableObject {
    [HideInInspector] public int CropID;

    [Header("Params")]
    public bool IsTree;
    public int DaysToGrow;
    [ConditionalHide("IsTree", true)]
    public SeedSO ItemForSeeding;
    public ItemSO ItemToGrowAndSpawn;
    public bool IsHarvestedByScythe;
    public bool CanRegrow;
    [ConditionalHide("CanRegrow", true)]
    public int DaysToRegrow;

    public List<TimeManager.SeasonName> SeasonsToGrow;

    [Header("Item min and max amount")]
    public int MinItemAmountToSpawn;
    public int MaxItemAmountToSpawn;

    [Header("Grow Stages")]
    public List<Sprite> DeadSpritesGrowthStages;
    public List<Sprite> SpritesGrowthStages;
}

