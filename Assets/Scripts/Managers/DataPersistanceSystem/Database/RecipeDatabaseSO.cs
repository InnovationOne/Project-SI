using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Database/Recipe Database")]
public class RecipeDatabaseSO : ScriptableObject {
    // List of recipes in the database
    [SerializeField] private List<RecipeSO> _recipes = new();
    // Cache to store recipes by their IDs for fast lookup
    private Dictionary<int, RecipeSO> _cache = new();

    /// <summary>
    /// Initializes the recipes in the recipe database on Start() and cache all recipes.
    /// </summary>
    public void InitializeRecipes() {
        for (int i = 0; i < _recipes.Count; i++) {
            _recipes[i].RecipeId = i;
            _cache[i] = _recipes[i]; // Populate the cache
        }
    }

    /// <summary>
    /// Indexer to access recipes by their IDs from the cache
    /// </summary>
    public RecipeSO this[int recipeId] {
        get {
            if (_cache.TryGetValue(recipeId, out var recipe)) {
                return recipe;
            } else {
                throw new KeyNotFoundException($"Recipe with ID {recipeId} does not exist in the cache.");
            }
        }
    }
}
