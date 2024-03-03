using UnityEngine;

[RequireComponent(typeof(TimeAgent))]
public class ItemProducer : Interactable {
    [Header("Settings")]
    [SerializeField] private RecipeTypes _recipeType;
    [SerializeField] private RecipeSO _recipe;
    [SerializeField] private int _timeInPercent;

    [Header("ItemProducer Visual")]
    [SerializeField] private SpriteRenderer _itemProducerHighlight;
    [SerializeField] private SpriteRenderer _itemProducerVisual;

    [Header("ItemProducer Sprites")]
    [SerializeField] private Sprite _itemProducerOut;
    [SerializeField] private Sprite _itemProducerOn;


    private int _localTimer;


    private void Awake() {
        _itemProducerHighlight.gameObject.SetActive(false);
    }

    private void Start() {
        TimeAgent timeAgent = GetComponent<TimeAgent>();
        timeAgent.onMinuteTimeTick += ItemConvertProcess;
    }

    // This function is called every time TimeAgent is called
    private void ItemConvertProcess() {
        if (_localTimer <= 0f) {
            return;
        }

        if (_localTimer > 0f) {
            _localTimer--;

            if (_localTimer == 0f) {
                _itemProducerVisual.sprite = _itemProducerOut;
            }
        }
    }

    public override void Interact(Player player) {
        if (_localTimer <= 0f) {
            foreach (ItemSlot itemSlot in _recipe.ItemsToProduce) {
                int remainingAmount = player.GetComponent<PlayerInventoryController>().InventoryContainer.AddItemToItemContainer(itemSlot.Item.ItemID, itemSlot.Amount, itemSlot.RarityID, false);


                if (remainingAmount > 0) {
                    ItemSpawnManager.Instance.SpawnItemAtPosition(transform.position, player.GetComponent<PlayerMovementController>().LastMotionDirection, itemSlot.Item, remainingAmount, itemSlot.RarityID, SpreadType.Circle);
                }
            }

            _localTimer = _recipe.TimeToProduce / 100 * _timeInPercent;
            _itemProducerVisual.sprite = _itemProducerOn;
        }
    }

    // This function spawns the items that are left in the item converter when it's picked up
    public override void PickUpItemsInPlacedObject(Player player) {
        // If there are no items to spawn, return
        if (_localTimer > 0f) {
            return;
        }

        // Spawn every item in the list
        foreach (ItemSlot itemSlot in _recipe.ItemsToProduce) {
            ItemSpawnManager.Instance.SpawnItemAtPosition(transform.position, player.GetComponent<PlayerMovementController>().LastMotionDirection, itemSlot.Item, itemSlot.Amount, itemSlot.RarityID, SpreadType.Circle);
        }
    }

    public override void ShowPossibleInteraction(bool show) {
        _itemProducerHighlight.gameObject.SetActive(show);
    }
}
