using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ProducerSO")]
public class ProducerSO : ObjectSO {
    [Header("Recipe Settings")]
    public RecipeSO.RecipeTypes RecipeType;
    public int ProduceTimeInPercent;
    public RecipeSO Recipe;

    [Header("UI-Settings")]
    public bool CloseUIAndObjectOnPlayerLeave;
}
