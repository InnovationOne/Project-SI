using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerCraftController : NetworkBehaviour {
    [Header("Recipe Container")]
    [SerializeField] private RecipeContainer _recipeContainer;

    // Cached references to frequently accessed components
    private PlayerInventoryController _inventoryController;
    private PlayerMovementController _movementController;
    private RecipeManager _recipeManager;
    private CraftVisual _craftVisual;
    private ItemSpawnManager _itemSpawnManager;

    // Ensure singleton pattern is correctly implemented
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Cache component references
        _inventoryController = GetComponent<PlayerInventoryController>();
        _movementController = GetComponent<PlayerMovementController>();
        _recipeManager = GameManager.Instance.RecipeManager;
        _craftVisual = CraftVisual.Instance;
        _itemSpawnManager = GameManager.Instance.ItemSpawnManager;
    }


    /// <summary>
    /// Displays all craftable items in the UI.
    /// </summary>
    public void ShowCraftableItems() {
        _craftVisual.ClearContentBox();

        foreach (var recipe in _recipeContainer.Recipes) {
            _craftVisual.SpawnCraftItemSlot(recipe);
        }
    }

    /// <summary>
    /// Handles left-click events on crafting buttons.
    /// </summary>
    /// <param name="buttonIndex">Index of the clicked button.</param>
    public void OnLeftClick(int buttonIndex) {
        if (buttonIndex < 0 || buttonIndex >= _recipeContainer.Recipes.Count) {
            Debug.LogWarning($"Invalid button index: {buttonIndex}");
            return;
        }

        RecipeSO selectedRecipe = _recipeManager.RecipeDatabase[_recipeContainer.Recipes[buttonIndex]];
        if (selectedRecipe == null) {
            Debug.LogError($"Recipe not found for index: {buttonIndex}");
            return;
        }

        if (!HasAllNeededItems(selectedRecipe)) {
            Debug.Log("Player does not have all required items for crafting.");
            return;
        }

        // Combine required items
        var requiredItems = selectedRecipe.ItemsNeededToConvert.Concat(selectedRecipe.ItemsToConvert).ToList();

        // Remove required items from inventory
        foreach (ItemSlot itemSlot in requiredItems) {
            _inventoryController.InventoryContainer.RemoveItem(itemSlot);
        }

        // Add produced items to inventory or spawn them in the world
        foreach (ItemSlot producedItem in selectedRecipe.ItemsToProduce) {
            int remaining = _inventoryController.InventoryContainer.AddItem(producedItem, false);
            if (remaining > 0) {
                // Spawn the remaining items in the world
                _itemSpawnManager.SpawnItemServerRpc(
                    itemSlot: producedItem,
                    initialPosition: transform.position,
                    motionDirection: _movementController.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    /// <summary>
    /// Checks if the player has all required items for a recipe.
    /// </summary>
    /// <param name="recipe">The recipe to check.</param>
    /// <returns>True if all required items are present; otherwise, false.</returns>
    private bool HasAllNeededItems(RecipeSO recipe) {
        // Combine required items once to avoid multiple concatenations
        var requiredItems = recipe.ItemsNeededToConvert.Concat(recipe.ItemsToConvert).ToList();

        // Create a dictionary for faster lookup: ItemId -> TotalAmount
        var inventoryDict = _inventoryController.InventoryContainer.CombineItemsByTypeAndRarity()
            .GroupBy(item => item.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(item => item.Amount));

        foreach (var required in requiredItems) {
            if (!inventoryDict.TryGetValue(required.ItemId, out int availableAmount) || availableAmount < required.Amount) {
                return false;
            }
        }

        return true;
    }
}
