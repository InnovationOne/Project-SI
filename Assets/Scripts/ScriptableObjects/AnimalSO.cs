using UnityEngine;

public enum AnimalSize {
    None, Small, Large
}

public enum AnimalBase {
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
    public AnimalSize AnimalSize;
    public AnimalBase AnimalBase;
    public AnimalType AnimalType;

    [Header("Food / Items")]
    public ItemSO FeedItem;
    public ItemSO PetItem;
    public bool HasQualityLevels = true;
    public ItemSO ProductItem;

    [Header("Visuals & Animation Sprites")]
    public Sprite[] WalkUp;
    public Sprite[] WalkRight;
    public Sprite[] WalkDown;
    public Sprite[] WalkLeft;
    // Additional arrays for standing, sleeping and eating could be added here.

    [Header("Friendship & growth")]
    public int InitialFriendship = 0;
    public int MaxFriendship = 1000;
    public int GrowthDays = 4;

    [Header("Mating / Breeding")]
    public bool CanLayEggs = false;
    public bool CanBeIncubated = false;
}
