using System;
using UnityEngine;

public class Store : MonoBehaviour, IInteractable {
    [Header("Store Container")]
    [SerializeField] public ItemDatabaseSO _storeContainer;

    [SerializeField] public StoreVisual _storeVisual;

    [NonSerialized] private float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

    public virtual void InitializePreLoad(int itemId) { }

    /*
    public void OnLeftClick(ItemSO itemSO) {
        for (int i = 0; i < _storeContainer.Items.Count; i++) {
            if (itemSO.ItemID == _storeContainer.Items[i].ItemID) {
                // Set the amount to 1 or 10 depending on shift pressed.
                int rarity = 0, amount;
                if (Input.GetKey(KeyCode.LeftShift)) {
                    amount = 10;
                } else {
                    amount = 1;
                }

                if (PlayerInventoryController.LocalInstance.InventoryContainer.CheckToAddItemToItemContainer(itemSO.ItemID, amount, rarity, false)) {
                    StartCoroutine(FinanceManager.Instance.PerformRemoveMoney(itemSO.BuyPrice, itemSO.ItemID, amount, rarity));
                } else {
                    // No space in the backpack
                }
            }
        }
    }
    */
    public virtual void Interact(Player player) {
        // Call the store UI
    }

    public virtual void PickUpItemsInPlacedObject(Player player) { }
}
