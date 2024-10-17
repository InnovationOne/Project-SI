using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the conversion of items based on recipes and timed processes.
/// </summary>
[RequireComponent(typeof(TimeAgent))]
public class ItemConverter : MonoBehaviour, IObjectDataPersistence, IInteractable {
    private ObjectVisual _visual;
    private SelectRecipeUI _selectRecipeUI;

    private const float MAX_DISTANCE_TO_PLAYER = 1.5f;
    public float MaxDistanceToPlayer { get => MAX_DISTANCE_TO_PLAYER; }

    private int _recipeId;
    private int _timer;
    private int _itemId;
    private List<ItemSlot> _storedItemSlots = new();

    /// <summary>
    /// Initializes and subscribes to time-based events.
    /// </summary>
    private void Start() {
        _selectRecipeUI = SelectRecipeUI.Instance;
        GetComponent<TimeAgent>().OnMinuteTimeTick += ItemConverterProcess;
    }

    private void OnDestroy() {
        GetComponent<TimeAgent>().OnMinuteTimeTick -= ItemConverterProcess;
    }

    /// <summary>
    /// Initializes the item producer with a specific item identifier.
    /// </summary>
    /// <param name="itemId">The item identifier used to fetch recipe details.</param>
    public void InitializePreLoad(int itemId) {
        _itemId = itemId;
        ResetTimer();
        _visual = GetComponentInChildren<ObjectVisual>();
        _visual.SetSprite(ConverterSO.InactiveSprite);
    }

    /// <summary>
    /// Performs the item conversion process.
    /// </summary>
    private void ItemConverterProcess() {
        if (_timer > 0 && --_timer == 0) {
            ProcessConversion();
        }
    }

    /// <summary>
    /// Processes the conversion of items.
    /// </summary>
    private void ProcessConversion() {
        _storedItemSlots.Clear();
        _storedItemSlots.AddRange(GetRecipeItemsToProduce());
        _visual.SetSprite(ConverterSO.InactiveSprite);
    }

    /// <summary>
    /// Interacts with the player.
    /// If the item converter can process items, it spawns the items, clears the stored items, and returns.
    /// If the item converter is eligible for a new recipe and has all the needed items, it selects a recipe and starts item processing.
    /// </summary>
    /// <param name="player">The player interacting with the item converter.</param>
    public void Interact(Player player) {
        if (CanProcessItems()) {
            SpawnItems();
            ClearStoredItems();
            return;
        }

        if (IsEligibleForNewRecipe()) {
            _recipeId = SelectRecipe();
            if (_recipeId != -1 && HasAllNeededItems()) {
                StartItemProcessing();
            }
        }
    }

    /// <summary>
    /// Checks if the item processing can be performed.
    /// </summary>
    /// <returns>True if the item processing can be performed, false otherwise.</returns>
    private bool CanProcessItems() => _timer <= 0 && _storedItemSlots.Any();

    /// <summary>
    /// Checks if the item is eligible for a new recipe.
    /// </summary>
    /// <returns>True if the item is eligible for a new recipe, false otherwise.</returns>
    private bool IsEligibleForNewRecipe()
    => !PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().IsEmpty
       && _storedItemSlots != null
       && _storedItemSlots.Any();


    private int SelectRecipe() => ConverterSO.Recipes.Count == 0 ? SelectRecipeAutomatically() : _selectRecipeUI.SelectRecipe(ConverterSO.Recipes);
    

    /// <summary>
    /// Selects a recipe automatically based on the provided toolbelt item slot.
    /// </summary>
    /// <param name="toolbeltItemSlot">The item slot in the toolbelt.</param>
    /// <returns>The ID of the selected recipe, or -1 if no recipe is found.</returns>
    private int SelectRecipeAutomatically() {
        ItemSlot toolbeltItemSlot = PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot();
        foreach (var recipe in RecipeManager.Instance.RecipeContainer.Recipes) {
            RecipeSO recipeSO = RecipeManager.Instance.RecipeDatabase[recipe];

            for (int i = 0; i < recipeSO.ItemsToConvert.Count; i++) {
                if (recipeSO.ItemsToConvert[i].ItemId == toolbeltItemSlot.ItemId &&
                    recipeSO.ItemsToConvert[i].RarityId == toolbeltItemSlot.RarityId &&
                    recipeSO.RecipeType == ConverterSO.RecipeType) {

                    return recipeSO.RecipeId;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Checks if the player has all the needed items to perform the conversion.
    /// </summary>
    /// <returns>True if the player has all the needed items, false otherwise.</returns>
    private bool HasAllNeededItems() {
        List<ItemSlot> inventory = PlayerInventoryController.LocalInstance.InventoryContainer.CombineItemsByTypeAndRarity();

        var matchingNum = RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsNeededToConvert
            .Concat(RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsToConvert)
            .Count(recipe => inventory.Any(inventoryItemSlot =>
                ItemManager.Instance.ItemDatabase[inventoryItemSlot.ItemId] != null &&
                inventoryItemSlot.ItemId == recipe.ItemId &&
                inventoryItemSlot.Amount >= recipe.Amount));

        return matchingNum == RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsNeededToConvert.Count + RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsToConvert.Count;
    }

    /// <summary>
    /// Starts the item processing by deducting required items from the inventory, resetting the timer, and setting the active sprite.
    /// </summary>
    private void StartItemProcessing() {
        DeductRequiredItemsFromInventory();
        ResetTimer();
        _visual.SetSprite(ConverterSO.ActiveSprite);
    }

    /// <summary>
    /// Deducts the required items from the player's inventory and stores them in the converter.
    /// </summary>
    private void DeductRequiredItemsFromInventory() {
        foreach (ItemSlot itemSlot in GetCombinedItemSlots()) {
            PlayerInventoryController.LocalInstance.InventoryContainer.RemoveItem(itemSlot);
            _storedItemSlots.Add(itemSlot);
        }
    }

    /// <summary>
    /// Retrieves the recipe items needed to produce the desired item.
    /// </summary>
    /// <returns>An enumerable collection of ItemSlot representing the recipe items.</returns>
    private IEnumerable<ItemSlot> GetRecipeItemsToProduce() => RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsToProduce;

    /// <summary>
    /// Retrieves the combined item slots required for conversion.
    /// </summary>
    /// <returns>An enumerable collection of ItemSlot objects representing the combined item slots.</returns>
    private IEnumerable<ItemSlot> GetCombinedItemSlots() => RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsNeededToConvert.Concat(RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsToConvert);

    /// <summary>
    /// Picks up items in the placed object and spawns them.
    /// </summary>
    /// <param name="player">The player who is picking up the items.</param>
    public void PickUpItemsInPlacedObject(Player player) {
        if (_storedItemSlots.Count > 0) {
            SpawnItems();
        }
    }

    /// <summary>
    /// Spawns the items stored in the item slots.
    /// </summary>
    private void SpawnItems() {
        foreach (ItemSlot itemSlot in _storedItemSlots) {
            int remainingAmount = PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(itemSlot, false);
            if (remainingAmount > 0) {
                ItemSpawnManager.Instance.SpawnItemServerRpc(
                    itemSlot: itemSlot,
                    initialPosition: transform.position,
                    motionDirection: PlayerMovementController.LocalInstance.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    /// <summary>
    /// Resets the production timer based on the recipe's production time.
    /// </summary>
    private void ResetTimer() => _timer = RecipeManager.Instance.RecipeDatabase[_recipeId].TimeToProduce * ConverterSO.ProduceTimeInPercent / 100;

    /// <summary>
    /// Fetches the ObjectSO associated with the current item ID.
    /// </summary>
    /// <returns>The ObjectSO associated with the current item.</returns>
    private ConverterSO ConverterSO => ItemManager.Instance.ItemDatabase[_itemId] as ConverterSO;

    private void ClearStoredItems() {
        _storedItemSlots.Clear();
        _recipeId = -1;
    }


    #region Save & Load
    [Serializable]
    public class ItemConverterData {
        public int RecipeId;
        public int Timer;
        public List<string> StoredItemSlots = new();
    }

    public string SaveObject() {
        var itemConverterJson = new ItemConverterData {
            RecipeId = _recipeId,
            Timer = _timer,
        };

        foreach (var slot in _storedItemSlots) {
            itemConverterJson.StoredItemSlots.Add(JsonConvert.SerializeObject(slot));
        }

        return JsonConvert.SerializeObject(itemConverterJson);
    }

    public void LoadObject(string data) {
        if (!string.IsNullOrEmpty(data)) {
            var itemConverterData = JsonConvert.DeserializeObject<ItemConverterData>(data);
            _storedItemSlots.Clear();
            _recipeId = itemConverterData.RecipeId;
            _timer = itemConverterData.Timer;
            foreach (var slot in itemConverterData.StoredItemSlots) {
                _storedItemSlots.Add(JsonConvert.DeserializeObject<ItemSlot>(slot));
            }
        }
    }
    #endregion
}
