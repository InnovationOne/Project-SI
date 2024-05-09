using UnityEngine;


// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/FoodSO")]
public class FoodSO : ItemSO {
    // The player uses a item to heal
    [Header("Use Settings")]
    public bool CanRestoreHpOrEnergy;
    [ConditionalHide("CanRestoreHpOrEnergy", true)]
    public int LowestRarityRestoringHpAmount;
    [ConditionalHide("CanRestoreHpOrEnergy", true)]
    public int LowestRarityRestoringEnergyAmount;

    /* For Fish
    [TextArea(6, 6)]
    public string FunnyText;
    */

    /* For Museum Objects
     [Header("Museum Settings")]
    public bool CanBeMuseum;
    */
}
