using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Container/Recipe Container")]
public class RecipeContainerSO : ScriptableObject {

    public List<RecipeSO> Recipes;

    public void AddRecipe(RecipeSO recipeSO) {
        Recipes.Add(recipeSO);
    }

    public void SortRecipes() {
        Recipes.Sort();
    }

    public void ClearRecipes() {
        Recipes.Clear();
    }
}
