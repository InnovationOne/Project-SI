using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the production of items based on recipes and timed processes.
/// This class is a server-side PlaceableObject that saves its own state.
/// </summary>
[RequireComponent(typeof(TimeAgent))]
public class ItemProducer : PlaceableObject {
    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _notifySpriteRenderer;
    private ItemProducerSO _itemProducerSO;

    private readonly NetworkVariable<int> _timer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _notifySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _notifySpriteRenderer.enabled = false;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) GetComponent<TimeAgent>().OnMinuteTimeTick += ItemProducerProcess;
        _timer.OnValueChanged += (_, _) => UpdateVisual();
        _timer.OnValueChanged += (oldVal, newVal) => {
            if (newVal == 0 && !IsServer) {
                _notifySpriteRenderer.enabled = true;
            }
        };
        UpdateVisual();
    }

    private void OnDestroy() {
        if (IsServer) GetComponent<TimeAgent>().OnMinuteTimeTick -= ItemProducerProcess;
    }

    public override void InitializePreLoad(int itemId) {
        _itemProducerSO = ItemManager.Instance.ItemDatabase[itemId] as ItemProducerSO;

        _spriteRenderer.sprite = _itemProducerSO.InactiveSprite;

        // Notify-Icon setzen (z. B. das Icon des ersten Produkts)
        if (_itemProducerSO.Recipe.ItemsToProduce.Count > 0) {
            int productId = _itemProducerSO.Recipe.ItemsToProduce[0].ItemId;
            _notifySpriteRenderer.sprite = GameManager.Instance.ItemManager.ItemDatabase[productId].ItemIcon;
        }

        if (IsServer) ResetTimer();
    }

    public override void InitializePostLoad() {
        UpdateVisual();
    }

    private void ItemProducerProcess() {
        if (_timer.Value > 0) _timer.Value--;
        else ResetTimer();
    }


    public override void Interact(PlayerController player) {
        if (_timer.Value <= 0) {
            ProduceItems();
            ResetTimerServerRpc();
        }
    }

    private void ProduceItems() {
        _notifySpriteRenderer.enabled = false;

        foreach (var itemSlot in _itemProducerSO.Recipe.ItemsToProduce) {
            var scaledSlot = new ItemSlot(itemSlot.ItemId, itemSlot.Amount * _itemProducerSO.AmountMultiply, itemSlot.RarityId);
            int leftover = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(scaledSlot, false);

            if (leftover > 0) {
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    new ItemSlot(itemSlot.ItemId, leftover, itemSlot.RarityId),
                    transform.position,
                    PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetTimerServerRpc() {
        ResetTimer();
    }

    private void ResetTimer() {
        _timer.Value = Mathf.Max(1, _itemProducerSO.Recipe.TimeToProduce / Mathf.Max(1, _itemProducerSO.SpeedMultiply));
    }

    private void UpdateVisual() {
        _spriteRenderer.sprite = _timer.Value > 0 ? _itemProducerSO.ActiveSprite : _itemProducerSO.InactiveSprite;
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) {
        if (_timer.Value <= 0) ProduceItems();
    }

    #region Save & Load

    public override string SaveObject() {
        return JsonConvert.SerializeObject(_timer.Value);
    }

    public override void LoadObject(string data) {
        if (int.TryParse(data, out int value)) _timer.Value = value; 
        else _timer.Value = 0;
    }

    #endregion

    public override void OnStateReceivedCallback(string callbackName) { }
}
