using Newtonsoft.Json;
using System.Collections.Generic;

public class RecipeContainer {
    private List<int> _recipes = new();
    public IReadOnlyList<int> Recipes => _recipes.AsReadOnly();

    /// <summary>
    /// Adds a recipe to the container.
    /// </summary>
    /// <param name="recipeId">The ID of the recipe to add.</param>
    public void Add(int recipeId) {
        _recipes.Add(recipeId);
    }

    /// <summary>
    /// Serializes the recipe container into a list of JSON strings.
    /// </summary>
    /// <returns>A JSON string representing the serialized recipes.</returns>
    public string SerializeRecipeContainer() {
        var recipeContainerJSON = new List<string>();
        foreach (var recipe in _recipes) {
            recipeContainerJSON.Add(JsonConvert.SerializeObject(recipe));
        }

        return JsonConvert.SerializeObject(recipeContainerJSON);
    }

    /// <summary>
    /// Deserializes a JSON string into the recipe container.
    /// </summary>
    /// <param name="jsonString">The JSON string to deserialize.</param>
    public void DeserializeRecipeContainer(string jsonString) {
        var deserialized_recipes = JsonConvert.DeserializeObject<List<string>>(jsonString);
        foreach (var recipe in deserialized_recipes) {
            _recipes.Add(JsonConvert.DeserializeObject<int>(recipe));
        }
    }
}

