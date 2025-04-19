using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Incubator : PlaceableObject, IInteractable {
    [SerializeField] private ItemSO[] _hatchableEggs;
    [SerializeField] private AnimalBuilding _parentBuilding;

    private NetworkVariable<int> _eggItemId = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _daysRemaining = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _isRunning = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private const int INCUBATION_DAYS = 4;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    public override void OnNetworkSpawn() {
        if (IsServer) TimeManager.Instance.OnNextDayStarted += OnNextDay;
    }
    public override void OnNetworkDespawn() {
        if (IsServer) TimeManager.Instance.OnNextDayStarted -= OnNextDay;
    }

    public override void Interact(PlayerController player) {
        var slot = player.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (!_isRunning.Value && _eggItemId.Value < 0) {
            foreach (var egg in _hatchableEggs) {
                if (slot.ItemId == egg.ItemId && slot.Amount > 0) {
                    SetEggServerRpc(egg.ItemId);
                    player.PlayerInventoryController.InventoryContainer.RemoveItem(new ItemSlot(egg.ItemId, 1, 0));
                    return;
                }
            }
        } else if (!_isRunning.Value && _eggItemId.Value >= 0) {
            player.PlayerInventoryController.InventoryContainer.AddItem(new ItemSlot(_eggItemId.Value, 1, 0), false);
            ResetEggServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetEggServerRpc(int itemId) {
        _eggItemId.Value = itemId;
        _daysRemaining.Value = INCUBATION_DAYS;
        _isRunning.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetEggServerRpc() {
        _eggItemId.Value = -1;
        _isRunning.Value = false;
    }

    private void OnNextDay() {
        if (!_isRunning.Value) return;
        if (--_daysRemaining.Value <= 0) TryHatchEgg();
    }

    private void TryHatchEgg() {
        if (_parentBuilding.HousedAnimalIdsList.Count >= _parentBuilding.AnimalBuildingSO.Capacity) {
            Debug.Log("Kein Platz in Coop. Inkubation pausiert.");
            _isRunning.Value = false;
            return;
        }
        // TODO: Tier-Prefab ermitteln anhand EggItemId und instanziieren
        Debug.Log($"Egg (ItemId {_eggItemId.Value}) ausgebrütet!");
        ResetEggServerRpc();
    }

    public override string SaveObject() {
        throw new System.NotImplementedException();
    }

    public override void LoadObject(string data) {
        throw new System.NotImplementedException();
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) { }
    public override void InitializePreLoad(int itemId) { }
    public override void InitializePostLoad() { }
    public override void OnStateReceivedCallback(string callbackName) { }    
}
