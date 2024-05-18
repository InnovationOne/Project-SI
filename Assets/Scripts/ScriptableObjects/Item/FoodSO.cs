using UnityEngine;


// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/FoodSO")]
public class FoodSO : ItemSO {
    // The player uses a item to heal
    [Header("Eat Settings")]
    public bool CanRestoreHp;
    [ConditionalHide("CanRestoreHp", true)]
    public int LowestHpAmount;

    public bool CanRestoreEnergy;
    [ConditionalHide("CanRestoreEnergy", true)]
    public int LowestEnergyAmount;

    /* For Museum Objects
     [Header("Museum Settings")]
    public bool CanBeMuseum;
    */
}
