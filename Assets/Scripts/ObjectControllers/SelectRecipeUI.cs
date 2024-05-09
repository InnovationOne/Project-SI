using Ink.Parsed;
using System.Collections.Generic;
using UnityEngine;

public class SelectRecipeUI : MonoBehaviour {
    public static SelectRecipeUI Instance { get; private set; }

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of SelectRecipeUI in the scene!");
            return;
        }
        Instance = this;
    }

    public int SelectRecipe(List<RecipeSO> recipes) {
        throw new System.NotImplementedException();
    }
}
