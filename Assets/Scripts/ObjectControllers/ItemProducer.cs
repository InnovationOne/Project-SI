using Newtonsoft.Json;
using System;
using UnityEngine;
using static ItemConverter;

/// <summary>
/// Manages the production of items based on recipes and timed processes.
/// </summary>
[RequireComponent(typeof(TimeAgent))]
public class ItemProducer : Interactable, IObjectDataPersistence {
    [SerializeField] private ObjectVisual _visual;

    private int _recipeId;
    private int _timer;
    private int _itemId;

    /// <summary>
    /// Initializes and subscribes to time-based events.
    /// </summary>
    private void Start() => GetComponent<TimeAgent>().onMinuteTimeTick += ItemProducerProcess;

    /// <summary>
    /// Initializes the item producer with a specific item identifier.
    /// </summary>
    /// <param name="itemId">The item identifier used to fetch recipe details.</param>
    public override void Initialize(int itemId) {
        _itemId = itemId;
        _recipeId = GetProducerSO().Recipe != null ? GetProducerSO().Recipe.RecipeId : throw new NotImplementedException("Recipe is not set for this item producer");
        ResetTimer();
        _visual.SetSprite(GetProducerSO().InactiveSprite);
    }

    /// <summary>
    /// Processes the conversion of items based on the recipe timer.
    /// </summary>
    private void ItemProducerProcess() {
        if (_timer > 0f) {
            _timer--;

            if (_timer == 0f) {
                GetComponent<ObjectVisual>().SetSprite(GetProducerSO().InactiveSprite);
            }
        }
    }

    /// <summary>
    /// Handles interactions with the player, typically used to trigger item production.
    /// </summary>
    /// <param name="player">The player interacting with the item producer.</param>
    public override void Interact(Player player) {
        if (_timer <= 0f) {
            ProduceItems();
            ResetTimer();
            _visual.SetSprite(GetProducerSO().ActiveSprite);
        }
    }

    /// <summary>
    /// Produces items based on the current recipe and handles inventory updates.
    /// </summary>
    /// <param name="player">The player for whom items are being produced.</param>
    private void ProduceItems() {
        foreach (ItemSlot itemSlot in RecipeManager.Instance.RecipeDatabase[_recipeId].ItemsToProduce) {
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
    private void ResetTimer() => _timer = RecipeManager.Instance.RecipeDatabase[_recipeId].TimeToProduce * GetProducerSO().ProduceTimeInPercent / 100;

    /// <summary>
    /// Fetches the ObjectSO associated with the current item ID.
    /// </summary>
    /// <returns>The ObjectSO associated with the current item.</returns>
    private ProducerSO GetProducerSO() => ItemManager.Instance.ItemDatabase[_itemId] as ProducerSO;

    /// <summary>
    /// Handles the collection of produced items when the object is dismanteled with.
    /// </summary>
    /// <param name="player">The player interacting with the placed object.</param>
    public override void PickUpItemsInPlacedObject(Player player) {
        if (_timer <= 0) {
            ProduceItems();
        }
    }

    #region Save & Load
    [Serializable]
    public class ItemProducerData {
        public int RecipeId;
        public int Timer;
        public int ItemId;
    }

    public string SaveObject() {
        var itemProducerJson = new ItemProducerData {
            RecipeId = _recipeId,
            Timer = _timer,
            ItemId = _itemId
        };

        return JsonConvert.SerializeObject(itemProducerJson);
    }

    public void LoadObject(string data) {
        if (!string.IsNullOrEmpty(data)) {
            var itemProducerData = JsonConvert.DeserializeObject<ItemProducerData>(data);
            _recipeId = itemProducerData.RecipeId;
            _timer = itemProducerData.Timer;
            _itemId = itemProducerData.ItemId;
        }
    }
    #endregion
}
