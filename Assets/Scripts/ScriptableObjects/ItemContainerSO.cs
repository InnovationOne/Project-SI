using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// This script creates a list of item slots e.g. inventory
[CreateAssetMenu(menuName = "Container/Item Container SO")]
public class ItemContainerSO : ScriptableObject {
    public List<ItemSlot> ItemSlots;

    public static bool ToolbeltSizeUpdated { private get; set; } = true;
    public static bool InventorySizeUpdated { private get; set; } = true;
    public bool ItemContainerNeedsToBeUpdated;

    private int _maxToolbeltSize;
    private int _currentToolbeltSize;
    private int _currentInventorySize;


    // This function is used to initialize the inventory
    public void InitializeItemContainer(int slotsAmount) {
        ItemSlots = new List<ItemSlot>();

        for (int i = 0; i < slotsAmount; i++) {
            ItemSlots.Add(new ItemSlot());
        }
    }

    // This region is used to add items to the item container
    #region Add Item To Item Container
    public int AddItemToItemContainer(int itemID, int amount, int rarityID, bool skipToolbelt) {
        UpdateToolbeltSize();
        UpdateInventorySize();

        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);
        int remainingAmount;

        if (item.IsStackable) {
            // The item is stackable
            remainingAmount = AddItemToExistingItemSlot(itemID, amount, rarityID, skipToolbelt);
        } else {
            // Item is not stackable
            remainingAmount = AddItemToEmptyItemSlot(itemID, amount, rarityID, skipToolbelt);
        }

        ItemContainerNeedsToBeUpdated = true;

        return remainingAmount;
    }

    private int AddItemToExistingItemSlot(int itemID, int amount, int rarityID, bool skipToolbelt) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        while (amount > 0) {
            // Look for existing item slots
            var stackableItemSlot = skipToolbelt ?
            ItemSlots.Skip(_maxToolbeltSize).FirstOrDefault(x => x.Item == item && x.Amount < item.MaxStackableAmount && x.RarityID == rarityID) :
            ItemSlots.FirstOrDefault(x => x.Item == item && x.Amount < item.MaxStackableAmount && x.RarityID == rarityID);

            if (stackableItemSlot != null) {
                int remainingAmount = item.MaxStackableAmount - stackableItemSlot.Amount;

                _ = amount > remainingAmount ? stackableItemSlot.Amount += remainingAmount : stackableItemSlot.Amount += amount;
                amount -= remainingAmount;
            } else {
                // No more existing stacks, add to empty slots
                return AddItemToEmptyItemSlot(itemID, amount, rarityID, skipToolbelt);
            }
        }

        return 0;
    }

    private int AddItemToEmptyItemSlot(int itemID, int amount, int rarityID, bool skipToolbelt) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        while (amount > 0) {
            int emptyItemSlotIndex = -1;
            ItemSlot emptyItemSlot = null;

            // Look for empty slots, as long as they are not blocked by the toolbelt or inventory size
            while (!((emptyItemSlotIndex >= 0 && emptyItemSlotIndex < _currentToolbeltSize) || (emptyItemSlotIndex >= _maxToolbeltSize && emptyItemSlotIndex < _currentInventorySize + _maxToolbeltSize))) {
                emptyItemSlot = skipToolbelt ? 
                    ItemSlots.Skip(_maxToolbeltSize).FirstOrDefault(x => x.Item == null && ItemSlots.IndexOf(x) > emptyItemSlotIndex) : 
                    ItemSlots.FirstOrDefault(x => x.Item == null && ItemSlots.IndexOf(x) > emptyItemSlotIndex);
                emptyItemSlotIndex = ItemSlots.IndexOf(emptyItemSlot);

                // If no empty slot was found
                if (emptyItemSlot == null) {
                    return amount;
                }
            }

            // If an itemSlot was found
            emptyItemSlot.Item = item;
            emptyItemSlot.RarityID = rarityID;

            _ = amount > item.MaxStackableAmount ? emptyItemSlot.Amount = item.MaxStackableAmount : emptyItemSlot.Amount = amount;
            amount -= item.MaxStackableAmount;
        }

        return 0;
    }
    #endregion


    // This region is used to check if an item can be added to the item container
    #region Check To Add Item To Item Container
    public bool CheckToAddItemToItemContainer(int itemID, int amount, int rarityID, bool skipToolbelt = false) {
        UpdateToolbeltSize();
        UpdateInventorySize();

        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);
        int remainingAmount;

        if (item.IsStackable) {
            // The item is stackable
            remainingAmount = CheckToAddItemToExistingItemSlot(itemID, amount, rarityID, skipToolbelt);
        } else {
            // Item is not stackable
            remainingAmount = CheckToAddItemToEmptyItemSlot(itemID, amount, rarityID, skipToolbelt);
        }

        ItemContainerNeedsToBeUpdated = true;

        return remainingAmount <= 0;
    }

    private int CheckToAddItemToExistingItemSlot(int itemID, int amount, int rarityID, bool skipToolbelt = false) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        while (amount > 0) {
            // Look for existing item slots
            var stackableItemSlot = skipToolbelt ?
            ItemSlots.Skip(_maxToolbeltSize).FirstOrDefault(x => x.Item == item && x.Amount < item.MaxStackableAmount && x.RarityID == rarityID) :
            ItemSlots.FirstOrDefault(x => x.Item == item && x.Amount < item.MaxStackableAmount && x.RarityID == rarityID);

            if (stackableItemSlot != null) {
                int remainingAmount = item.MaxStackableAmount - stackableItemSlot.Amount;
                amount -= remainingAmount;
            } else {
                // No more existing stacks, add to empty slots
                return CheckToAddItemToEmptyItemSlot(itemID, amount, rarityID, skipToolbelt);
            }
        }

        return 0;
    }

    private int CheckToAddItemToEmptyItemSlot(int itemID, int amount, int rarityID, bool skipToolbelt = false) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        while (amount > 0) {
            int emptyItemSlotIndex = -1;
            ItemSlot emptyItemSlot = null;

            // Look for empty slots, as long as they are not blocked by the toolbelt or inventory size
            while (!((emptyItemSlotIndex >= 0 && emptyItemSlotIndex < _currentToolbeltSize) || (emptyItemSlotIndex >= _maxToolbeltSize && emptyItemSlotIndex < _currentInventorySize + _maxToolbeltSize))) {
                emptyItemSlot = skipToolbelt ?
                    ItemSlots.Skip(_maxToolbeltSize).FirstOrDefault(x => x.Item == null && ItemSlots.IndexOf(x) > emptyItemSlotIndex) :
                    ItemSlots.FirstOrDefault(x => x.Item == null && ItemSlots.IndexOf(x) > emptyItemSlotIndex);
                emptyItemSlotIndex = ItemSlots.IndexOf(emptyItemSlot);

                // If no empty slot was found
                if (emptyItemSlot == null) {
                    return amount;
                }
            }

            // If an itemSlot was found
            amount -= item.MaxStackableAmount;
        }

        return 0;
    }
    #endregion

    // Remove the specified item from this container, with the option to specify the amount to remove
    #region Remove Item From Item Container
    public bool RemoveAnItemFromTheItemContainer(int itemID, int amount, int rarityID) {
        ItemSlot foundItemSlot = AddAllItemsTogether(ItemSlots).FirstOrDefault(x => x.Item.ItemID == itemID && x.RarityID == rarityID);

        // No item slot was found or the needed amount is greater than the amount in the item slot
        if (foundItemSlot == null || foundItemSlot.Amount < amount) {
            return false;
        }

        // Remove the item slots from the item container
        RemoveTheItemAmount(itemID, amount, rarityID);

        ItemContainerNeedsToBeUpdated = true;

        return true;
    }

    // Groups all items with the same itemID and rarity together
    public List<ItemSlot> AddAllItemsTogether(List<ItemSlot> itemSlotList) {
        var itemTuples = itemSlotList
            .Where(slot => slot.Item != null)
            .ToList();

        var groupedItemSlots = itemTuples.GroupBy(i => new { i.Item.ItemID, i.RarityID });

        List<ItemSlot> combinedItemSlots = new();
        foreach (var group in groupedItemSlots) {
            // Add the amount of all the item slots in the group
            int combinedAmount = group.Sum(i => i.Amount);

            // Add the combined item slot to the list
            combinedItemSlots.Add(new ItemSlot {
                Item = group.First().Item,
                Amount = combinedAmount,
                RarityID = group.First().RarityID,
            });
        }

        return combinedItemSlots;
    }

    // This function removes the amount of an item from the item container
    private void RemoveTheItemAmount(int itemID, int amount, int rarityID) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        while (amount > 0) {
            // Find the item to remove
            ItemSlot itemSlot = ItemSlots.FirstOrDefault(x => x.Item == item && x.RarityID == rarityID);

            int transferAmount = itemSlot.Amount;
            if ((itemSlot.Amount -= amount) <= 0) {
                itemSlot.Clear();
            }
            amount -= transferAmount;
        }
    }
    #endregion

    // Sorts the items in the container by item type, item type ID, and rarity ID
    public void SortList() {
        UpdateToolbeltSize();
        UpdateInventorySize();

        // Create a list of tuples containing the item and amount for each non-empty slot
        List<(ItemSO item, int amount, int rarityID)> itemTuples = ItemSlots
            .Skip(_maxToolbeltSize)
            .Where(slot => slot.Item != null)
            .Select(slot => (slot.Item, slot.Amount, slot.RarityID))
            .ToList();

        // Clear all slots
        ItemSlots.Skip(_maxToolbeltSize).ToList().ForEach(slot => slot.Clear());

        // Re-add the items in the sorted order
        itemTuples
            .OrderBy(i => i.item.ItemType)
            .ThenBy(i => i.item.ItemTypeID)
            .ThenBy(i => i.rarityID)
            .ToList()
            .ForEach(i => AddItemToItemContainer(i.item.ItemID, i.amount, i.rarityID, true));
    }

    // Shift the item slots in the inventory and toolbelt
    public void ShiftItemSlots(int shiftAmount) {
        var shiftedSlots = new List<ItemSlot>(ItemSlots.Count);

        for (int i = 0; i < ItemSlots.Count; i++) {
            int newIndex = (i + shiftAmount) % ItemSlots.Count;
            if (newIndex < 0) {
                newIndex += ItemSlots.Count;
            }

            shiftedSlots.Add(ItemSlots[newIndex]);
        }

        ItemSlots = shiftedSlots;
    }

    public void ShootOneItemOut(int currentItemContainerSlot) {
        if ((ItemSlots[currentItemContainerSlot].Amount -= 1) <= 0) {
            ItemSlots[currentItemContainerSlot].Clear();
        }

        ItemContainerNeedsToBeUpdated = true;
    }

    // Updates the toolbelt size when it has changed
    private void UpdateToolbeltSize() {
        if (ToolbeltSizeUpdated) {
            var playerToolbeltController = PlayerToolbeltController.LocalInstance;
            _maxToolbeltSize = playerToolbeltController.ToolbeltSizes[^1];
            _currentToolbeltSize = playerToolbeltController.CurrentToolbeltSize;
        }
    }

    // Updates the inventory size when it has changed
    private void UpdateInventorySize() {
        if (InventorySizeUpdated) {
            _currentInventorySize = PlayerInventoryController.LocalInstance.CurrentInventorySize;
        }
    }
}