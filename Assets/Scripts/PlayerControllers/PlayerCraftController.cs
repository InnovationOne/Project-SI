using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerCraftController : NetworkBehaviour {
    public static PlayerCraftController LocalInstance { get; private set; }

    [Header("Recipe Container")]
    [SerializeField] private RecipeContainer _recipeContainer;


    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerCraftController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    public void ShowCraftableItems() {
        CraftVisual.Instance.ClearContentBox();

        foreach (var recipe in RecipeManager.Instance.RecipeContainer.Recipes) {
            CraftVisual.Instance.SpawnCraftItemSlot(recipe);
        }
    }

    public void OnLeftClick(int buttonIndex) {
        RecipeSO recipe = RecipeManager.Instance.RecipeDatabase[RecipeManager.Instance.RecipeContainer.Recipes[buttonIndex]];

        // Check if the player has the required auxiliary items
        if (!HasAllNeededItems(recipe)) {
            return;
        }

        var combinedItemSlots = recipe.ItemsNeededToConvert.Concat(recipe.ItemsToConvert);

        // Remove the items from the players inventory
        foreach (ItemSlot itemSlot in combinedItemSlots) {
            GetComponent<PlayerInventoryController>().InventoryContainer.RemoveItem(itemSlot);
        }

        // Add the items to the inventory or spawn the item at the player position
        foreach (ItemSlot itemSlot in recipe.ItemsToProduce) {
            if (GetComponent<PlayerInventoryController>().InventoryContainer.AddItem(itemSlot, false) > 0) {
                ItemSpawnManager.Instance.SpawnItemServerRpc(
                    itemSlot: itemSlot,
                    initialPosition: transform.position,
                    motionDirection: PlayerMovementController.LocalInstance.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    private bool HasAllNeededItems(RecipeSO recipe) {
        List<ItemSlot> inventory = GetComponent<PlayerInventoryController>().InventoryContainer.CombineItemsByTypeAndRarity();

        // Check if combinedItems have all the items and amounts required by the recipe
        var matchingNum = recipe.ItemsNeededToConvert
            .Concat(recipe.ItemsToConvert)
            .Count(recipe => inventory.Any(inventoryItemSlot =>
                ItemManager.Instance.ItemDatabase[inventoryItemSlot.ItemId] != null &&
                inventoryItemSlot.ItemId == recipe.ItemId &&
                inventoryItemSlot.Amount >= recipe.Amount));

        // Return true if all required items and amounts are met
        return matchingNum == recipe.ItemsNeededToConvert.Count + recipe.ItemsToConvert.Count;
    }
}
