using UnityEngine;
using Unity.Netcode;
using Newtonsoft.Json;

/// <summary>
/// Manages recipes and provides methods for retrieving and saving recipe data.
/// </summary>
public class RecipeManager : NetworkBehaviour, IDataPersistance {
    /// <summary>
    /// Singleton instance of the RecipeManager.
    /// </summary>
    public static RecipeManager Instance { get; private set; }

    private RecipeContainer _recipeContainer;
    public RecipeContainer RecipeContainer => _recipeContainer;
    [SerializeField] private RecipeDatabaseSO _recipeDatabase;
    /// <summary>
    /// Container for all receipes.
    /// </summary>
    public RecipeDatabaseSO RecipeDatabase => _recipeDatabase;


    /// <summary>
    /// This method is called when the script instance is being loaded.
    /// It checks if an instance of the script already exists and destroys the game object if it does.
    /// Otherwise, it sets the instance to this script and initializes the components.
    /// </summary>
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of RecipeManager in the scene!");
            return;
        }

        Instance = this;
        _recipeContainer = new RecipeContainer();
        _recipeDatabase.InitializeRecipes();
    }

    /// <summary>
    /// Called when the object is spawned on the network.
    /// </summary>
    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnected;
        }
    }


    #region Network Sync
    /// <summary>
    /// Event handler for when a client is connected to the network.
    /// </summary>
    /// <param name="clientId">The ID of the connected client.</param>
    private void NetworkManager_OnClientConnected(ulong clientId) => NetworkManager_OnClientConnectedClientRpc(clientId, JsonConvert.SerializeObject(_recipeContainer.SerializeRecipeContainer()));

    /// <summary>
    /// This method is a client RPC (Remote Procedure Call) that is invoked on the server when a client connects.
    /// It receives the client ID and a JSON string representing the recipe container.
    /// If the client ID matches the local client ID and the current instance is not a server, it unboxes the recipe container JSON.
    /// </summary>
    /// <param name="clientId">The ID of the client that connected.</param>
    /// <param name="recipeContainerJson">The JSON string representing the recipe container.</param>
    [ClientRpc]
    private void NetworkManager_OnClientConnectedClientRpc(ulong clientId, string recipeContainerJson) {
        if (clientId == NetworkManager.Singleton.LocalClientId && !IsServer) {
            UnboxRecipeContainerJson(recipeContainerJson);
        }
    }

    /// <summary>
    /// Unboxes the recipe container JSON and deserializes it.
    /// </summary>
    /// <param name="recipeContainerJson">The JSON string representing the recipe container.</param>
    private void UnboxRecipeContainerJson(string recipeContainerJson) => _recipeContainer.DeserializeRecipeContainer(recipeContainerJson);
    
    #endregion


    #region Save & Load
    public void SaveData(GameData data) => data.Recipes = _recipeContainer.SerializeRecipeContainer();

    public void LoadData(GameData data) {
        if (!string.IsNullOrEmpty(data.Recipes)) {
            UnboxRecipeContainerJson(data.Recipes);
        }
    }
    #endregion
}
