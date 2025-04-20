using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(TimeAgent))]
public class FeedingTrough : PlaceableObject, IInteractable {
    [SerializeField] private ItemSO _hayItem;
    [SerializeField] private int _maxHayCount;
    [SerializeField] private bool _autoFeed;
    private NetworkVariable<int> _hayCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool HasHay => _hayCount.Value > 0;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    public override void OnNetworkSpawn() {
        if (IsServer) GetComponent<TimeAgent>().OnMinuteTimeTick += Tick;
    }

    public override void OnNetworkDespawn() {
        if (IsServer) GetComponent<TimeAgent>().OnMinuteTimeTick -= Tick;
        base.OnNetworkDespawn();
    }

    private void Tick() {
        if (!_autoFeed) return;
        int needed = _maxHayCount - _hayCount.Value;
        if (needed <= 0) return;
        foreach (var silo in Silo.AllSilos) {
            int avail = silo.HayStored;
            if (avail > 0) {
                int take = Mathf.Min(avail, needed);
                silo.RemoveHayServerRpc(take);
                _hayCount.Value += take;
                needed -= take;
                if (needed <= 0) break;
            }
        }
    }

    public override void Interact(PlayerController player) {
        var item = player.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (item.ItemId == _hayItem.ItemId && _hayCount.Value < _maxHayCount) {
            if (InputManager.Instance.GetShiftPressed()) {
                int missing = _maxHayCount - _hayCount.Value;
                int canAdd = Mathf.Min(item.Amount, missing);
                AddHayServerRpc(canAdd);
                player.PlayerInventoryController.InventoryContainer.RemoveItem(new ItemSlot(_hayItem.ItemId, canAdd, 0));
            } else {
                AddHayServerRpc(1);
                player.PlayerInventoryController.InventoryContainer.RemoveItem(new ItemSlot(_hayItem.ItemId, 1, 0));
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddHayServerRpc(int amount) {
        _hayCount.Value = Mathf.Clamp(_hayCount.Value + amount, 0, _maxHayCount);
    }

    public override string SaveObject() {
        return JsonUtility.ToJson(_hayCount.Value);
}
    public override void LoadObject(string data) {
        if (string.IsNullOrEmpty(data)) return;
        _hayCount.Value = JsonUtility.FromJson<int>(data);
    }

    public override void InitializePreLoad(int itemId) { }
    public override void PickUpItemsInPlacedObject(PlayerController player) { }
    public override void InitializePostLoad() { }
    public override void OnStateReceivedCallback(string callbackName) { }
}
