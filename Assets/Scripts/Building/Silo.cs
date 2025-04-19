using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Silo : Building, IInteractable {
    private static readonly List<Silo> _allSilos = new();
    private NetworkVariable<int> _hayStored = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private int _maxHay = 200;
    [SerializeField] private ItemSO _hayItem;

    public static IReadOnlyList<Silo> AllSilos => _allSilos;
    public int HayStored => _hayStored.Value;
    public int MaxHay => _maxHay;

    public float MaxDistanceToPlayer => 2f;
    public bool CircleInteract => false;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) _allSilos.Add(this);
    }
    public override void OnNetworkDespawn() {
        if (IsServer) _allSilos.Remove(this);
        base.OnNetworkDespawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddHayServerRpc(int amount) {
        _hayStored.Value = Mathf.Clamp(_hayStored.Value + amount, 0, _maxHay);
    }
    [ServerRpc(RequireOwnership = false)]
    public void RemoveHayServerRpc(int amount) {
        _hayStored.Value = Mathf.Max(_hayStored.Value - amount, 0);
    }

    public void Interact(PlayerController player) {
        var slot = player.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (slot.ItemId == _hayItem.ItemId && _hayStored.Value < _maxHay) {
            AddHayServerRpc(1);
            player.PlayerInventoryController.InventoryContainer.RemoveItem(new ItemSlot(slot.ItemId, 1, 0));
        } else if (_hayStored.Value > 0) {
            player.PlayerInventoryController.InventoryContainer.AddItem(new ItemSlot(_hayItem.ItemId, 1, 0), false);
            RemoveHayServerRpc(1);
        }
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) {
        for (int i = 0; i < _hayStored.Value; i++) {
            ItemSpawnManager.Instance.SpawnItemServerRpc(
                new ItemSlot(_hayItem.ItemId, 1, 0),
                transform.position,
                Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle
            );
        }
    }
    
    public override string SaveObject(){ 
        return JsonUtility.ToJson(_hayStored.Value); 
    }

    public override void LoadObject(string data) {
        if (string.IsNullOrEmpty(data)) return;
        _hayStored.Value = JsonUtility.FromJson<int>(data);
    }

    public override void InitializePreLoad(int itemId) { }
    public override void InitializePostLoad() { }
    public override void OnStateReceivedCallback(string callbackName) { }
}
