using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TimeAgent))]
public class ItemConverter : PlaceableObject {
    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _notifySpriteRenderer;
    private SelectRecipeUI _selectRecipeUI;
    private ItemConverterSO _itemConverterSO;

    private readonly NetworkVariable<int> _timer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _recipeId = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly List<ItemSlot> _storedItemSlots = new();

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _notifySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _notifySpriteRenderer.enabled = false;
    }

    private void Start() {
        _selectRecipeUI = SelectRecipeUI.Instance;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _timer.OnValueChanged += (_, __) => {
            UpdateVisual();
        };
        _timer.OnValueChanged += (old, newVal) => {
            if (newVal == 0) _notifySpriteRenderer.enabled = true;
        };
        if (IsServer) GetComponent<TimeAgent>().OnMinuteTimeTick += ServerTick;
        UpdateVisual();
    }

    private new void OnDestroy() {
        if (IsServer) GetComponent<TimeAgent>().OnMinuteTimeTick -= ServerTick;
    }

    private void ServerTick() {
        if (_timer.Value > 0) {
            _timer.Value--;
            if (_timer.Value == 0) {
                var recipe = RecipeManager.Instance.RecipeDatabase[_recipeId.Value];
                _storedItemSlots.Clear();
                foreach (var slot in recipe.ItemsToProduce) {
                    _storedItemSlots.Add(new ItemSlot(slot.ItemId, slot.Amount * _itemConverterSO.AmountMultiply, slot.RarityId));
                }
                
                PlaceableObjectsManager.Instance.UpdateObjectStateServerRpc(
                    NetworkObject.NetworkObjectId, 
                    JsonConvert.SerializeObject(_storedItemSlots));
                _notifySpriteRenderer.enabled = true;
            }
        }
    }

    public override void InitializePreLoad(int itemId) {
        _itemConverterSO = ItemManager.Instance.ItemDatabase[itemId] as ItemConverterSO;
        _spriteRenderer.sprite = _itemConverterSO.InactiveSprite;
        _notifySpriteRenderer.enabled = false;
        if (IsServer) ResetTimer();
    }

    public override void InitializePostLoad() {
        UpdateVisual();
    }

    public override void Interact(PlayerController player) {
        PlaceableObjectsManager.Instance.RequestObjectStateServerRpc(NetworkObject.NetworkObjectId, "Interact");
    }

    private int SelectRecipe() {
        return _itemConverterSO.UseUI
            ? _selectRecipeUI.SelectRecipe(_itemConverterSO.Recipes)
            : SelectRecipeAutomatically();
    }

    private int SelectRecipeAutomatically() {
        var toolbeltItem = PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        foreach (var recipe in _itemConverterSO.Recipes) {
            if (recipe.RecipeType != _itemConverterSO.RecipeType) continue;
            bool matches = recipe.ItemsToConvert.Any(r => r.ItemId == toolbeltItem.ItemId && r.RarityId == toolbeltItem.RarityId);
            if (matches) return recipe.RecipeId;
        }
        return -1;
    }
    private bool HasAllNeededItems(int recipeId) {
        var recipe = GameManager.Instance.RecipeManager.RecipeDatabase[recipeId];
        var inventory = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.CombineItemsByTypeAndRarity();
        return recipe.ItemsNeededToConvert.Concat(recipe.ItemsToConvert)
            .All(req => inventory.Any(inv => inv.ItemId == req.ItemId && inv.RarityId == req.RarityId && inv.Amount >= req.Amount));
    }

    private void RemoveItems(int recipeId) {
        var recipe = GameManager.Instance.RecipeManager.RecipeDatabase[recipeId];
        foreach (var need in recipe.ItemsNeededToConvert.Concat(recipe.ItemsToConvert)) {
            PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.RemoveItem(
                new ItemSlot(need.ItemId, need.Amount, need.RarityId));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartProcessServerRpc(int recipeId) {
        _recipeId.Value = recipeId;
        ResetTimer();
        _notifySpriteRenderer.enabled = false;
    }

    private void SpawnItems() {
        PlaceableObjectsManager.Instance.RequestObjectStateServerRpc(NetworkObject.NetworkObjectId, "PickUpItems");
    }

    private void ResetTimer() {
        if (_recipeId.Value < 0) return;
        var recipe = GameManager.Instance.RecipeManager.RecipeDatabase[_recipeId.Value];
        _timer.Value = Mathf.Max(1, recipe.TimeToProduce / Mathf.Max(1, _itemConverterSO.SpeedMultiply));
    }

    private void UpdateVisual() {
        _spriteRenderer.sprite = _timer.Value > 0 ? _itemConverterSO.ActiveSprite : _itemConverterSO.InactiveSprite;
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) {
        SpawnItems();
    }

    public override void OnStateReceivedCallback(string callbackName) {
        switch (callbackName) {
            case "Interact":
                if (_timer.Value <= 0 && _storedItemSlots.Count > 0) {
                    SpawnItems();
                    _storedItemSlots.Clear();

                } else if (_timer.Value <= 0) {
                    int newId = SelectRecipe();
                    if (newId >= 0 && HasAllNeededItems(newId)) {
                        RemoveItems(newId);
                        StartProcessServerRpc(newId);
                    }
                }
                break;
            case "PickUpItems":
                foreach (var slot in _storedItemSlots) {
                    int leftover = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(slot, false);
                    if (leftover > 0) {
                        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                            slot,
                            transform.position,
                            PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                            spreadType: ItemSpawnManager.SpreadType.Circle);
                    }
                }
                break;
        }
    }


    #region Save & Load

    [Serializable]
    public class ItemConverterData {
        public int RecipeId;
        public int Timer;
        public List<string> StoredItemSlots;
    }

    public override string SaveObject() {
        return JsonConvert.SerializeObject(new ItemConverterData {
            RecipeId = _recipeId.Value,
            Timer = _timer.Value,
            StoredItemSlots = _storedItemSlots.Select(s => JsonConvert.SerializeObject(s)).ToList()
        });
    }

    public override void LoadObject(string json) {
        if (string.IsNullOrEmpty(json)) return;
        var data = JsonConvert.DeserializeObject<ItemConverterData>(json);
        _recipeId.Value = data.RecipeId;
        _timer.Value = data.Timer;
        _storedItemSlots.Clear();
        foreach (var slotJson in data.StoredItemSlots) {
            _storedItemSlots.Add(JsonConvert.DeserializeObject<ItemSlot>(slotJson));
        }
    }

    #endregion
}
