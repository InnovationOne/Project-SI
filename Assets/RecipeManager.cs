using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Netcode;

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
}

public class RecipeManager : NetworkBehaviour, IDataPersistance {
    public static RecipeManager Instance { get; private set; }

    [SerializeField] private RecipeContainerSO _recipeContainer;
    [SerializeField] private RecipeDatabaseSO _recipeDatabase;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of RecipeManager in the scene!");
            return;
        }
        Instance = this;
    }

    #region Save & Load
    [Serializable]
    public class RecipeData {
        public int RecipeID;

        public RecipeData(int recipeID) {
            RecipeID = recipeID;
        }
    }

    [Serializable]
    public class RecipesData {
        public List<RecipeData> RecipeDataList;

        public RecipesData() {
            RecipeDataList = new List<RecipeData>();
        }
    }

    public void SaveData(GameData data) {
        RecipesData recipesData = new();

        foreach (var recipe in _recipeContainer.Recipes) {
            recipesData.RecipeDataList.Add(new RecipeData(recipe.recipeID));
        }

        data.Recipes = JsonUtility.ToJson(recipesData);
    }

    public void LoadData(GameData data) {
        if (!string.IsNullOrEmpty(data.Recipes)) {
            // Clear the current items in the container
            _recipeContainer.ClearRecipes();
            var recipesData = JsonUtility.FromJson<RecipesData>(data.Recipes);

            // Add the items from the ToSave object to the container
            foreach (var recipe in recipesData.RecipeDataList) {
                _recipeContainer.AddRecipe(_recipeDatabase.Recipes[recipe.RecipeID]);
            }
        }
    }
    #endregion
}
