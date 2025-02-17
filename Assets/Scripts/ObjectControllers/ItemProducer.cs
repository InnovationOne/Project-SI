using Newtonsoft.Json;
using System;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Manages the production of items based on recipes and timed processes.
/// </summary>
[RequireComponent(typeof(TimeAgent))]
public class ItemProducer : PlaceableObject {
    private SpriteRenderer _visual;
    private int _recipeId;
    private int _timer;
    private int _itemId;

    public override float MaxDistanceToPlayer => 0f;

    /// <summary>
    /// Initializes and subscribes to time-based events.
    /// </summary>
    private void Start() => GetComponent<TimeAgent>().OnMinuteTimeTick += ItemProducerProcess;

    private new void OnDestroy() { 
        GetComponent<TimeAgent>().OnMinuteTimeTick -= ItemProducerProcess; 
        base.OnDestroy();
    }

    /// <summary>
    /// Initializes the item producer with a specific item identifier.
    /// </summary>
    /// <param name="itemId">The item identifier used to fetch recipe details.</param>
    public override void InitializePreLoad(int itemId) {
        _itemId = itemId;
        _recipeId = ProducerSO.Recipe != null ? ProducerSO.Recipe.RecipeId : throw new NotImplementedException("Recipe is not set for this item producer");
        ResetTimer();
        _visual = GetComponent<SpriteRenderer>();
        _visual.sprite = ProducerSO.InactiveSprite;
    }


    /// <summary>
    /// Processes the conversion of items based on the recipe timer.
    /// </summary>
    private void ItemProducerProcess() {
        if (_timer > 0f) {
            _timer--;

            if (_timer == 0f) {
                _visual.sprite = ProducerSO.InactiveSprite;
            }
        } else {
            ResetTimer();
        }
    }

    /// <summary>
    /// Handles interactions with the player, typically used to trigger item production.
    /// </summary>
    /// <param name="player">The player interacting with the item producer.</param>
    public override void Interact(PlayerController player) {
        if (_timer <= 0f) {
            ProduceItems();
            ResetTimer();
            _visual.sprite = ProducerSO.ActiveSprite;
        }
    }

    /// <summary>
    /// Produces items based on the current recipe and handles inventory updates.
    /// </summary>
    /// <param name="player">The player for whom items are being produced.</param>
    private void ProduceItems() {
        foreach (ItemSlot itemSlot in GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].ItemsToProduce) {
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
    private void ResetTimer() => _timer = GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId].TimeToProduce * ProducerSO.ProduceTimeInPercent / 100;

    /// <summary>
    /// Fetches the ObjectSO associated with the current item ID.
    /// </summary>
    /// <returns>The ObjectSO associated with the current item.</returns>
    private ProducerSO ProducerSO => GameManager.Instance.ItemManager.ItemDatabase[_itemId] as ProducerSO;


    /// <summary>
    /// Handles the collection of produced items when the object is dismanteled with.
    /// </summary>
    /// <param name="player">The player interacting with the placed object.</param>
    public override void PickUpItemsInPlacedObject(PlayerController player) {
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

    public override FixedString4096Bytes SaveObject() {
        var itemProducerJson = new ItemProducerData {
            RecipeId = _recipeId,
            Timer = _timer,
            ItemId = _itemId
        };

        return new FixedString4096Bytes(JsonConvert.SerializeObject(itemProducerJson));
    }

    public override void LoadObject(FixedString4096Bytes data) {
        string jsonData = data.ToString();

        if (!string.IsNullOrEmpty(jsonData)) {
            var itemProducerData = JsonConvert.DeserializeObject<ItemProducerData>(jsonData);
            _recipeId = itemProducerData.RecipeId;
            _timer = itemProducerData.Timer;
            _itemId = itemProducerData.ItemId;
        }
    }

    #endregion

    public override void InitializePostLoad() { }
}
