using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerCraftController : NetworkBehaviour {
    public static PlayerCraftController LocalInstance { get; private set; }

    [Header("Recipe Container")]
    [SerializeField] private RecipeContainerSO _recipeContainer;


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

        foreach (RecipeSO recipe in _recipeContainer.Recipes) {
            CraftVisual.Instance.SpawnCraftItemSlot(recipe);
        }
    }

    public void OnLeftClick(int buttonIndex) {
        RecipeSO recipe = _recipeContainer.Recipes[buttonIndex];

        // Check if the player has the required auxiliary items
        if (!HasAllNeededItems(recipe)) {
            return;
        }

        var combinedItemSlots = recipe.ItemsNeededToConvert.Concat(recipe.ItemsToConvert);

        // Remove the items from the players inventory
        foreach (ItemSlot itemSlot in combinedItemSlots) {
            GetComponent<PlayerInventoryController>().InventoryContainer.RemoveAnItemFromTheItemContainer(itemSlot.Item.ItemID, itemSlot.Amount, itemSlot.RarityID);
        }

        // Add the items to the inventory or spawn the item at the player position
        foreach (ItemSlot itemSlot in recipe.ItemsToProduce) {
            if (GetComponent<PlayerInventoryController>().InventoryContainer.AddItemToItemContainer(itemSlot.Item.ItemID, itemSlot.Amount, itemSlot.RarityID, false) > 0) {
                ItemSpawnManager.Instance.SpawnItemAtPosition(transform.position, GetComponent<PlayerMovementController>().LastMotionDirection, itemSlot.Item, itemSlot.Amount, itemSlot.RarityID, SpreadType.Circle);
            }
        }
    }

    private bool HasAllNeededItems(RecipeSO recipe) {
        List<ItemSlot> inventory = GetComponent<PlayerInventoryController>().InventoryContainer
            .AddAllItemsTogether(GetComponent<PlayerInventoryController>().InventoryContainer.ItemSlots);

        // Check if combinedItems have all the items and amounts required by the recipe
        var matchingNum = recipe.ItemsNeededToConvert
            .Concat(recipe.ItemsToConvert)
            .Count(recipe => inventory.Any(inventoryItemSlot =>
                inventoryItemSlot.Item != null &&
                inventoryItemSlot.Item.ItemID == recipe.Item.ItemID &&
                inventoryItemSlot.Amount >= recipe.Amount));

        // Return true if all required items and amounts are met
        return matchingNum == recipe.ItemsNeededToConvert.Count + recipe.ItemsToConvert.Count;
    }
}
