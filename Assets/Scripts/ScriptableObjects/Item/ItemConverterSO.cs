using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ConverterSO")]
public class ItemConverterSO : ObjectSO {
    [Header("Recipe")]
    public RecipeSO.RecipeTypes RecipeType;
    public List<RecipeSO> Recipes;

    [Header("Advanced")]
    public int SpeedMultiply = 1;
    public int AmountMultiply = 1;

    [Header("Sprites")]
    public Sprite ActiveSprite;
    public Sprite InactiveSprite;

    [Header("UI")]
    public bool UseUI;
}
