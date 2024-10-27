using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ConverterSO")]
public class ConverterSO : ObjectSO {
    [Header("Recipe Settings")]
    public RecipeSO.RecipeTypes RecipeType;
    public int ProduceTimeInPercent;
    public List<RecipeSO> Recipes;

    public Sprite ActiveSprite;
    public Sprite InactiveSprite;

    [Header("UI-Settings")]
    public bool CloseUIAndObjectOnPlayerLeave;
}
