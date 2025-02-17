using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Manages the conversion of items based on recipes and timed processes.
/// </summary>
[RequireComponent(typeof(TimeAgent))]
public class ItemConverter : PlaceableObject {
    private SelectRecipeUI _selectRecipeUI;
    private SpriteRenderer _visual;

    public override float MaxDistanceToPlayer => 1.5f;

    private int _recipeId;
    private int _timer;
    private int _itemId;
    private List<ItemSlot> _storedItemSlots = new();

    private void Awake() {
        _visual = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Initializes and subscribes to time-based events.
    /// </summary>
    private void Start() {
        _selectRecipeUI = SelectRecipeUI.Instance;
        GetComponent<TimeAgent>().OnMinuteTimeTick += ItemConverterProcess;
    }

    private new void OnDestroy() {
        GetComponent<TimeAgent>().OnMinuteTimeTick -= ItemConverterProcess;
        base.OnDestroy();
    }

    /// <summary>
    /// Initializes the item producer with a specific item identifier.
    /// </summary>
    /// <param name="itemId">The item identifier used to fetch recipe details.</param>
    public override void InitializePreLoad(int itemId) {
        _itemId = itemId;
        ResetTimer();
        _visual.sprite = ConverterSO.InactiveSprite;
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
        _visual.sprite = ConverterSO.InactiveSprite;
    }

    /// <summary>
    /// Interacts with the player.
    /// If the item converter can process items, it spawns the items, clears the stored items, and returns.
    /// If the item converter is eligible for a new recipe and has all the needed items, it selects a recipe and starts item processing.
    /// </summary>
    /// <param name="player">The player interacting with the item converter.</param>
    public override void Interact(PlayerController player) {
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
    => !PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().IsEmpty
       && _storedItemSlots != null
       && _storedItemSlots.Any();


    private int SelectRecipe() => ConverterSO.Recipes.Count == 0 ? SelectRecipeAutomatically() : _selectRecipeUI.SelectRecipe(ConverterSO.Recipes);
    

    /// <summary>
    /// Selects a recipe automatically based on the provided toolbelt item slot.
    /// </summary>
    /// <param name="toolbeltItemSlot">The item slot in the toolbelt.</param>
    /// <returns>The ID of the selected recipe, or -1 if no recipe is found.</returns>
    private int SelectRecipeAutomatically() {
        ItemSlot toolbeltItemSlot = PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        foreach (var recipe in GameManager.Instance.RecipeManager.RecipeContainer.Recipes) {
            RecipeSO recipeSO = GameManager.Instance.RecipeManager.RecipeDatabase[recipe];

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
        List<ItemSlot> inventory = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.CombineItemsByTypeAndRarity();

        var matchingNum = GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsNeededToConvert
            .Concat(GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsToConvert)
            .Count(recipe => inventory.Any(inventoryItemSlot =>
                GameManager.Instance.ItemManager.ItemDatabase[inventoryItemSlot.ItemId] != null &&
                inventoryItemSlot.ItemId == recipe.ItemId &&
                inventoryItemSlot.Amount >= recipe.Amount));

        return matchingNum == GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsNeededToConvert.Count + GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsToConvert.Count;
    }

    /// <summary>
    /// Starts the item processing by deducting required items from the inventory, resetting the timer, and setting the active sprite.
    /// </summary>
    private void StartItemProcessing() {
        DeductRequiredItemsFromInventory();
        ResetTimer();
        _visual.sprite = ConverterSO.ActiveSprite;
    }

    /// <summary>
    /// Deducts the required items from the player's inventory and stores them in the converter.
    /// </summary>
    private void DeductRequiredItemsFromInventory() {
        foreach (ItemSlot itemSlot in GetCombinedItemSlots()) {
            PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.RemoveItem(itemSlot);
            _storedItemSlots.Add(itemSlot);
        }
    }

    /// <summary>
    /// Retrieves the recipe items needed to produce the desired item.
    /// </summary>
    /// <returns>An enumerable collection of ItemSlot representing the recipe items.</returns>
    private IEnumerable<ItemSlot> GetRecipeItemsToProduce() => GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsToProduce;

    /// <summary>
    /// Retrieves the combined item slots required for conversion.
    /// </summary>
    /// <returns>An enumerable collection of ItemSlot objects representing the combined item slots.</returns>
    private IEnumerable<ItemSlot> GetCombinedItemSlots() => GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsNeededToConvert.Concat(GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsToConvert);

    /// <summary>
    /// Picks up items in the placed object and spawns them.
    /// </summary>
    /// <param name="player">The player who is picking up the items.</param>
    public override void PickUpItemsInPlacedObject(PlayerController player) {
        if (_storedItemSlots.Count > 0) {
            SpawnItems();
        }
    }

    /// <summary>
    /// Spawns the items stored in the item slots.
    /// </summary>
    private void SpawnItems() {
        foreach (ItemSlot itemSlot in _storedItemSlots) {
            int remainingAmount = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(itemSlot, false);
            if (remainingAmount > 0) {
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    itemSlot: itemSlot,
                    initialPosition: transform.position,
                    motionDirection: PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    /// <summary>
    /// Resets the production timer based on the recipe's production time.
    /// </summary>
    private void ResetTimer() => _timer = GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].TimeToProduce * ConverterSO.ProduceTimeInPercent / 100;

    /// <summary>
    /// Fetches the ObjectSO associated with the current item ID.
    /// </summary>
    /// <returns>The ObjectSO associated with the current item.</returns>
    private ConverterSO ConverterSO => GameManager.Instance.ItemManager.ItemDatabase[_itemId] as ConverterSO;

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

    public override FixedString4096Bytes SaveObject() {
        var itemConverterJson = new ItemConverterData {
            RecipeId = _recipeId,
            Timer = _timer,
        };

        foreach (var slot in _storedItemSlots) {
            itemConverterJson.StoredItemSlots.Add(JsonConvert.SerializeObject(slot));
        }

        return new FixedString4096Bytes(JsonConvert.SerializeObject(itemConverterJson));
    }

    public override void LoadObject(FixedString4096Bytes data) {
        string jsonData = data.ToString();
        if (!string.IsNullOrEmpty(jsonData)) {
            var itemConverterData = JsonConvert.DeserializeObject<ItemConverterData>(jsonData);
            _storedItemSlots.Clear();
            _recipeId = itemConverterData.RecipeId;
            _timer = itemConverterData.Timer;
            foreach (var slot in itemConverterData.StoredItemSlots) {
                _storedItemSlots.Add(JsonConvert.DeserializeObject<ItemSlot>(slot));
            }
        }
    }

    #endregion


    public override void InitializePostLoad() { }
}
