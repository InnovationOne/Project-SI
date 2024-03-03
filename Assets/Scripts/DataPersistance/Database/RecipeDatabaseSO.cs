using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Database/Recipe Database")]
public class RecipeDatabaseSO : ScriptableObject {
    [Header("All recipes in the game, recipe id = place in list")]
    public List<RecipeSO> Recipes;

    public void SetRecipeId() {
        Recipes = Recipes.Select((recipe, index) => {
            recipe.recipeID = index;
            return recipe;
        }).ToList();
    }
}
