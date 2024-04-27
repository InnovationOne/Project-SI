using System;
using System.Collections.Generic;
using UnityEngine;

public class ChestBehaviour : Interactable, IObjectDataPersistence {
    [Header("Settings")]
    [SerializeField] private int _itemSlots;

    [Header("Images")]
    [SerializeField] private SpriteRenderer _chestVisual;
    [SerializeField] private Sprite _chestClosed;
    [SerializeField] private Sprite _chestOpened;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer _chestHighlight;
    [SerializeField] private Sprite _chestClosedHighlight;
    [SerializeField] private Sprite _chestOpenedHighlight;

    private const float MAX_DISTANCE_TO_PLAYER = 1.5f;
    private ItemContainerSO _itemContainer;    
    private bool _opened = false;
    private static bool _canOpenChest = true;
    private Player _player;


    private void Awake() {
        _chestVisual.sprite = _chestClosed;
        _chestHighlight.gameObject.SetActive(false);
    }

    private void Start() {
        // When no itemContainer is initialized, initialise a new one
        if (_itemContainer == null) {
            Init();
        }
    }

    private void Update() {
        if (_player != null && Vector2.Distance(_player.transform.position, transform.position) > MAX_DISTANCE_TO_PLAYER) {
            Interact(_player);
            _player = null;
        }
    }

    private void Init() {
        _itemContainer = (ItemContainerSO)ScriptableObject.CreateInstance(typeof(ItemContainerSO));
        _itemContainer.Initialize(_itemSlots);
    }

    public override void Interact(Player player) {
        if (_opened) {
            InventoryMasterVisual.Instance.HideChestPanel();
            CloseChest();
        } else if (!_opened && _canOpenChest) {
            _chestVisual.sprite = _chestOpened;
            InventoryMasterVisual.Instance.ShowChestPanel();
            ChestPanel.Instance.ShowChest(_itemContainer, this);
            _player = player;
            _canOpenChest = false;
            _opened = true;
        } else if (!_canOpenChest) {
            if (player.GetComponent<PlayerInteractController>().LastInteractable == null) { 
                Debug.LogWarning("No LastInteractedChest found!");
                return;
            }
            player.GetComponent<PlayerInteractController>().LastInteractable.GetComponent<ChestBehaviour>().CloseChest();
            InventoryMasterVisual.Instance.HideChestPanel();
        }
    }

    public override void ShowPossibleInteraction(bool show) {
        if (_opened) {
            _chestHighlight.sprite = _chestOpenedHighlight;
        } else {
            _chestHighlight.sprite = _chestClosedHighlight;
        }

        _chestHighlight.gameObject.SetActive(show);
    }

    public override void PickUpItemsInPlacedObject(Player player) {
        foreach (var itemSlot in _itemContainer.ItemSlots) {
            ItemSpawnManager.Instance.SpawnItemAtPosition(transform.position, player.GetComponent<PlayerMovementController>().LastMotionDirection, itemSlot.Item, itemSlot.Amount, itemSlot.RarityId, SpreadType.Circle);
        }
        
    }

    public void CloseChest() {
        _chestVisual.sprite = _chestClosed;
        _player = null;
        _opened = false;
        _canOpenChest = true;
    }


    #region Save & Load
    [Serializable]
    public class SaveItemData {
        public int itemId;
        public int amount;
        public int rarityID;

        public SaveItemData(int itemId, int amount, int rarityID) {
            this.itemId = itemId;
            this.amount = amount;
            this.rarityID = rarityID;
        }
    }

    [Serializable]
    public class ToSave {
        public List<SaveItemData> itemData;

        public ToSave() {
            itemData = new List<SaveItemData>();
        }
    }

    public void LoadObject(string data) {
        if (string.IsNullOrEmpty(data)) {
            return;
        }

        if (_itemContainer == null) {
            Init();
        }

        ToSave toLoadItemContainer = JsonUtility.FromJson<ToSave>(data);

        // Iterate over the items in the ToSaveItemContainer object
        for (int i = 0; i < toLoadItemContainer.itemData.Count; i++) {
            if (toLoadItemContainer.itemData[i].itemId == -1) {
                _itemContainer.ItemSlots[i].Clear();
            } else {
                // Set the item and amount in the inventory slot
                _itemContainer.ItemSlots[i].Item = ItemManager.Instance.ItemDatabase[toLoadItemContainer.itemData[i].itemId];
                _itemContainer.ItemSlots[i].Amount = toLoadItemContainer.itemData[i].amount;
                _itemContainer.ItemSlots[i].RarityId = toLoadItemContainer.itemData[i].rarityID;
            }
        }
    }

    public string SaveObject() {
        // Save the inventory container to a string
        ToSave toSaveItemContainer = new();

        foreach (var slot in _itemContainer.ItemSlots) {
            // Check if the slot has an item
            if (slot.Item == null) {
                toSaveItemContainer.itemData.Add(new SaveItemData(-1, -1, -1));
            } else {
                toSaveItemContainer.itemData.Add(new SaveItemData(slot.Item.ItemId, slot.Amount, slot.RarityId));
            }
        }

        return JsonUtility.ToJson(toSaveItemContainer);
    }
    #endregion
}
