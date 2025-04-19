using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class HayDistributor : PlaceableObject, IInteractable {
    [SerializeField] private ItemSO _hayItem;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    public override void Interact(PlayerController player) {
        var slot = player.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (slot.ItemId == _hayItem.ItemId && slot.Amount > 0) {
            foreach (var silo in Silo.AllSilos) {
                if (silo.HayStored < silo.MaxHay) {
                    silo.AddHayServerRpc(1);
                    player.PlayerInventoryController.InventoryContainer.RemoveItem(new ItemSlot(_hayItem.ItemId, 1, 0));
                    return;
                }
            }
        } else {
            foreach (var silo in Silo.AllSilos) {
                if (silo.HayStored > 0) {
                    silo.RemoveHayServerRpc(1);
                    player.PlayerInventoryController.InventoryContainer.AddItem(new ItemSlot(_hayItem.ItemId, 1, 0), false);
                    return;
                }
            }
        }
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) { }
    public override void InitializePreLoad(int itemId) { }
    public override void InitializePostLoad() { }
    public override void OnStateReceivedCallback(string callbackName) { }
    public override string SaveObject() { return ""; }
    public override void LoadObject(string data) { }
}
