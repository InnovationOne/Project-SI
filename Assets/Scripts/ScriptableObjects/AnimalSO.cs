using UnityEngine;

public enum AnimalType {
    none,
    // Small
    WhiteChicken,
    BrownChicken,
    CemaniChicken,
    BeryllChicken,
    Goose,
    BlueGoose,
    Bunny,
    Turkey,

    // Big
    Cow,
    SpottedCow,
    EringerCow,
    BeryllCow,
    Buffalo,
    Sheep,
    Goat,
    ValaisGoat,
    Lama,
}

// This class contains information used in an animal
[CreateAssetMenu(menuName = "Scriptable Objects/Animal")]
public class AnimalSO : ScriptableObject {
    [HideInInspector] public int AnimalID;

    [Header("Animal Settings")]
    public AnimalType AnimalType;

    [Header("Produktion Settings")]
    public ItemSlot ItemToFeed; // e.g. hay
    public ItemSlot ItemToPet; // e.g. brush
    public ItemSlot ItemToGetProducedItem; // e.g. milk bucket
    public ItemSlot ProducedItem;

    [Header("Visual Settings")]
    public Sprite[] WalkUp;
    public Sprite[] WalkRight;
    public Sprite[] WalkDown;    
    public Sprite[] WalkLeft;

    //Standing.
    //Sleeping.
    //Eating.
    //Drinking.
}
