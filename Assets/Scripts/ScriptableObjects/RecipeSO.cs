using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Objects/Recipe")]
public class RecipeSO : ScriptableObject {
    [HideInInspector] public int recipeID;

    public RecipeTypes RecipeType;
    public List<ItemSlot> ItemsToConvert;
    public List<ItemSlot> ItemsNeededToConvert;
    public List<ItemSlot> ItemsToProduce;
    public int TimeToProduce;
}
