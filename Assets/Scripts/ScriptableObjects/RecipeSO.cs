using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Recipe")]
public class RecipeSO : ScriptableObject {
    public enum RecipeTypes {
        Furnace,
        SeedExtractor,
        Compressor,
        Crafting,
        Juicer,
        JamKettle,
        CheeseMachine,
        MaturationBarrel,
        MayonnaiseMachine,
        Loom,
        SewingMachine,
        Smoker,
        Beehive,
        Tap,
        Rose,
    }

    [HideInInspector] public int RecipeId;

    public RecipeTypes RecipeType;
    public List<ItemSlot> ItemsToConvert;
    public List<ItemSlot> ItemsNeededToConvert;
    public List<ItemSlot> ItemsToProduce;
    public int TimeToProduce;
}
