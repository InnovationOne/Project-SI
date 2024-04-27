using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This script is for item converters e.g. semelter to be interacted
[RequireComponent(typeof(TimeAgent))]
public class ItemConverterInteract : Interactable, IObjectDataPersistence {
    [Header("Settings")]
    [SerializeField] private RecipeTypes _recipeType;
    [SerializeField] private int _timeInPercent;

    [Header("Seed Extractor Settings")]
    [SerializeField] private bool _isSeedExtractor;

    [Header("ItemConverter Visual")]
    [SerializeField] private SpriteRenderer _itemConverterHighlight;
    [SerializeField] private SpriteRenderer _itemConverterVisual;

    [Header("ItemConverter Sprites")]
    [SerializeField] private Sprite _itemConverterOut;
    [SerializeField] private Sprite _itemConverterOn;

    private List<ItemSlot> _storedLocalItemSlot;
    private int _localTimer;
    private RecipeSO _currentRecipe;


    private void Awake() {
        _itemConverterHighlight.gameObject.SetActive(false);
    }

    private void Start() {
        TimeAgent timeAgent = GetComponent<TimeAgent>();
        timeAgent.onMinuteTimeTick += ItemConvertProcess;
    }

    // This function is called every time TimeAgent is called
    private void ItemConvertProcess() {
        if (_storedLocalItemSlot.Count == 0 || _localTimer <= 0f) {
            return;
        }

        if (_localTimer > 0) {
            _localTimer--;

            if (_localTimer == 0) {
                _storedLocalItemSlot.Clear();
                _currentRecipe.ItemsToProduce.ForEach((itemSlot) => _storedLocalItemSlot.Add(itemSlot));
                _itemConverterVisual.sprite = _itemConverterOut;
            }
        }
    }

    public override void Interact(Player player) {
        ItemSlot toolbeltItemSlot = player.GetComponent<PlayerToolbeltController>().GetCurrentlySelectedToolbeltItemSlot();

        // When the process has finished, add all the items to the inventory
        if (_localTimer <= 0f && _storedLocalItemSlot.Count > 0) {
            foreach (ItemSlot itemSlot in _currentRecipe.ItemsToProduce) {
                int remainingAmount;
                if (_isSeedExtractor) {
                    // Seed Extractor only, with variable amount
                    remainingAmount = player.GetComponent<PlayerInventoryController>().InventoryContainer.AddItem(
                        itemSlot.Item.ItemId,
                        UnityEngine.Random.Range(1, itemSlot.Amount + 1),
                        itemSlot.RarityId,
                        false);
                } else {
                    // Everything else, with fix amount
                    remainingAmount = player.GetComponent<PlayerInventoryController>().InventoryContainer.AddItem(
                        itemSlot.Item.ItemId,
                        itemSlot.Amount,
                        itemSlot.RarityId,
                        false);
                }

                if (remainingAmount > 0) {
                    ItemSpawnManager.Instance.SpawnItemAtPosition(transform.position, player.GetComponent<PlayerMovementController>().LastMotionDirection, itemSlot.Item, remainingAmount, itemSlot.RarityId, SpreadType.Circle);
                }
            }

            _storedLocalItemSlot.Clear();
            _currentRecipe = null;
            return;
        }

        // Check if the item slot is empty, an item is beeing processed or if the item is not convertable
        if (toolbeltItemSlot.Item == null || _storedLocalItemSlot.Count > 0 || !SelectRecipe(toolbeltItemSlot)) {
            return;
        }

        // Check if the player has the required auxiliary items
        if (!HasAllNeededItems(player)) {
            return;
        }

        StartItemProcessing(player);
    }

    // Check if the item slot contains a convertable item
    private bool SelectRecipe(ItemSlot toolbeltItemSlot) {
        foreach (RecipeSO recipeSO in PlaceableObjectsManager.Instance.GetRecipeDatabase().Recipes) {
            for (int i = 0; i < recipeSO.ItemsToConvert.Count; i++) {
                if (recipeSO.ItemsToConvert[i].Item.ItemId == toolbeltItemSlot.Item.ItemId &&
                    recipeSO.ItemsToConvert[i].RarityId == toolbeltItemSlot.RarityId &&
                    recipeSO.RecipeType == _recipeType) {
                    _currentRecipe = recipeSO;

                    return true;
                }
            }
        }

        return false;
    }

    // Check if the player has the required auxiliary items
    private bool HasAllNeededItems(Player player) {
        List<ItemSlot> inventory = player.GetComponent<PlayerInventoryController>().InventoryContainer
            .CombineItemsByTypeAndRarity(player.GetComponent<PlayerInventoryController>().InventoryContainer.ItemSlots.ToList());

        // Check if combinedItems have all the items and amounts required by the recipe
        var matchingNum = _currentRecipe.ItemsNeededToConvert
            .Concat(_currentRecipe.ItemsToConvert)
            .Count(recipe => inventory.Any(inventoryItemSlot =>
                inventoryItemSlot.Item != null &&
                inventoryItemSlot.Item.ItemId == recipe.Item.ItemId &&
                inventoryItemSlot.Amount >= recipe.Amount));

        // Return true if all required items and amounts are met
        return matchingNum == _currentRecipe.ItemsNeededToConvert.Count + _currentRecipe.ItemsToConvert.Count;
    }

    // This function starts the item processing
    private void StartItemProcessing(Player player) {
        var combinedItemSlots = _currentRecipe.ItemsNeededToConvert.Concat(_currentRecipe.ItemsToConvert);

        // Remove the items from the players inventory and add the taken item into the item converter
        foreach (ItemSlot itemSlot in combinedItemSlots) {
            player.GetComponent<PlayerInventoryController>().InventoryContainer.RemoveItem(itemSlot.Item.ItemId, itemSlot.Amount, itemSlot.RarityId);
            _storedLocalItemSlot.Add(itemSlot);
        }

        // Set the timer to the time this conversion needs
        _localTimer = _currentRecipe.TimeToProduce / 100 * _timeInPercent;
        _itemConverterVisual.sprite = _itemConverterOn;
    }

    // This function spawns the items that are left in the item converter when it's picked up
    public override void PickUpItemsInPlacedObject(Player player) {
        // If there are no items to spawn, return
        if (_storedLocalItemSlot.Count <= 0) {
            return;
        }

        // Spawn every item in the list
        foreach (ItemSlot itemSlot in _storedLocalItemSlot) {
            ItemSpawnManager.Instance.SpawnItemAtPosition(transform.position, player.GetComponent<PlayerMovementController>().LastMotionDirection, itemSlot.Item, itemSlot.Amount, itemSlot.RarityId, SpreadType.Circle);
        }
    }

    public override void ShowPossibleInteraction(bool show) {
        _itemConverterHighlight.gameObject.SetActive(show);
    }



    #region Save & Load
    [Serializable]
    public class SaveObjectData {
        public int itemID;
        public int amount;
        public int rarityID;

        public SaveObjectData(int itemID, int amount, int rarityID) {
            this.itemID = itemID;
            this.amount = amount;
            this.rarityID = rarityID;
        }
    }

    [Serializable]
    public class ToSaveObjectData {
        public List<SaveObjectData> storedItemSlotData;
        public int timer;
        public int recipeID;

        public ToSaveObjectData() {
            storedItemSlotData = new List<SaveObjectData>();
        }
    }

    public void LoadObject(string data) {
        if (string.IsNullOrEmpty(data)) {
            return;
        }

        ToSaveObjectData toLoadObjectData = JsonUtility.FromJson<ToSaveObjectData>(data);

        for (int i = 0; i < toLoadObjectData.storedItemSlotData.Count; i++) {
            ItemSlot test = new() {
                Item = ItemManager.Instance.ItemDatabase[toLoadObjectData.storedItemSlotData[i].itemID],
                Amount = toLoadObjectData.storedItemSlotData[i].amount,
                RarityId = toLoadObjectData.storedItemSlotData[i].rarityID
            };
            _storedLocalItemSlot.Add(test);
        }

        _localTimer = toLoadObjectData.timer;
        _currentRecipe = PlaceableObjectsManager.Instance.GetRecipeDatabase().Recipes[toLoadObjectData.recipeID];
    }

    public string SaveObject() {
        ToSaveObjectData toSaveObjectData = new ToSaveObjectData();

        foreach (var slot in _storedLocalItemSlot) {
            toSaveObjectData.storedItemSlotData.Add(new SaveObjectData(slot.Item.ItemId, slot.Amount, slot.RarityId));
        }

        toSaveObjectData.timer = _localTimer;
        if (_currentRecipe != null) {
            toSaveObjectData.recipeID = _currentRecipe.recipeID;
        }

        return JsonUtility.ToJson(toSaveObjectData);
    }
    #endregion
}
