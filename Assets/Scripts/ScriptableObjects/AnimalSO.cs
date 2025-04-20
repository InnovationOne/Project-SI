using UnityEngine;

public enum AnimalSize {
    None, Small, Large
}

public enum AnimalCategory {
    None, Chicken, Goose, Duck, Cow, Sheep, Goat, Pig, Alpaca
}

public enum AnimalType {
    None,
    WhiteChicken, BrownChicken, BlueChicken, GreenChicken, GoldenChicken,
    WhiteGoose, BlackGoose, ColorfulGoose,
    Duck, MandarinDuck, BlackHeadDuck,
    BlackWhiteCow, BlackCow, ReddishCow, RainbowCow,
    WhiteSheep, BlackSheep, ColorfulSheep,
    BrownGoat, GoldenGoat,
    PinkPig, BlackPig, GalacticPig,
    Alpaca, SilverAlpaca, GoldenAlpaca
}

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/Animal")]
public class AnimalSO : ItemSO {
    [HideInInspector] public int AnimalId;
    public AnimalSize AnimalSize;
    public AnimalCategory AnimalBase;
    public AnimalType AnimalType;

    [Header("Food / Items")]
    public bool HasQualityLevels = true;
    public ItemSO PrimaryProductItem;
    public ItemSO SecondaryProductItem;

    [Header("Production Tools")]
    public ItemSO FeedItem;
    public ItemSO PetItem;
    public ItemSO ProductTool;

    [Header("Friendship & growth")]
    public int InitialFriendship = 0;
    public int MaxFriendship = 1000;
    public int GrowthDays = 4;

    // Positive Effects
    public int FeedAmount = 5;
    public int PetAmount = 5;
    public int BrushAmount = 10;
    public int ProductAmount = 5;
    public int AteOutsideAmount = 10;
    public int AnimalIsWarmAmount = 5;

    // Negative Effects
    public int NotFedAmount = -25;
    public int NotPettedAmount = -5;
    public int OutsideInRainAmount = -10;
    public int OutsideByNightAmount = -15;
    public int InColdAmount = -5;

    [Header("Mating / Breeding")]
    public bool CanLayEggs = false;
    public bool CanBeIncubated = false;

    [Header("Visuals & Animation Sprites")]
    public Sprite[] WalkUp;
    public Sprite[] WalkRight;
    public Sprite[] WalkDown;
    public Sprite[] WalkLeft;
    // Additional arrays for standing, sleeping and eating could be added here.    
}
