using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// This script creates a list of item slots e.g. inventory
[CreateAssetMenu(menuName = "Container/Item Container SO")]
public class ItemContainerSO : ScriptableObject {
    public event Action OnItemsUpdated;

    public List<ItemSlot> ItemSlots = new();
    // public IReadOnlyList<ItemSlot> ItemSlots => itemSlots.AsReadOnly(); TODO: Maybe use this and make ItemSlots private serializefield?
    // TODO: Maybe use a dict instead of a list for faster access?

    /// <summary>
    /// Initializes the item container with the specified number of slots.
    /// </summary>
    /// <param name="slotsAmount">The number of slots to initialize.</param>
    public void Initialize(int slotsAmount) {
        ItemSlots = new List<ItemSlot>(Enumerable.Repeat(new ItemSlot(), slotsAmount));
    }

    #region Add Item
    /// <summary>
    /// Adds an item to the container.
    /// </summary>
    /// <param name="itemId">The ID of the item to add.</param>
    /// <param name="amount">The amount of the item to add.</param>
    /// <param name="rarityId">The rarity ID of the item.</param>
    /// <param name="skipToolbelt">A flag indicating whether to skip adding the item to the toolbelt.</param>
    /// <returns>The remaining amount of the item after adding.</returns>
    public int AddItem(int itemId, int amount, int rarityId, bool skipToolbelt) {
        ValidateItemParameters(itemId, amount);
        var itemSO = GetItemSO(itemId);

        int remainingAmount = itemSO.IsStackable ?
            AddToExisting(itemSO, amount, rarityId, skipToolbelt) :
            AddToEmpty(itemSO, amount, rarityId, skipToolbelt);

        UIUpdate();
        return remainingAmount;
    }

    /// <summary>
    /// Adds the specified item to an existing slot in the item container.
    /// </summary>
    /// <param name="itemSO">The item to add.</param>
    /// <param name="amount">The amount of the item to add.</param>
    /// <param name="rarityId">The rarity ID of the item.</param>
    /// <param name="skipToolbelt">A flag indicating whether to skip the toolbelt slots.</param>
    /// <returns>
    /// The remaining amount of the item that could not be added to any existing slot.
    /// </returns>
    private int AddToExisting(ItemSO itemSO, int amount, int rarityId, bool skipToolbelt) {
        var relevantSlots = GetRelevantSlots(skipToolbelt).ToList();
        foreach (var slot in relevantSlots) {
            if (slot.Item == itemSO && slot.Amount < itemSO.MaxStackableAmount && slot.RarityId == rarityId) {
                int addable = Math.Min(itemSO.MaxStackableAmount - slot.Amount, amount);
                slot.Amount += addable;
                amount -= addable;
                if (amount == 0) {
                    return 0;
                }
            }
        }
        return AddToEmpty(itemSO, amount, rarityId, skipToolbelt);
    }

    /// <summary>
    /// Adds an item to an empty slot in the item container.
    /// </summary>
    /// <param name="itemSO">The item to add.</param>
    /// <param name="amount">The amount of the item to add.</param>
    /// <param name="rarityId">The rarity ID of the item.</param>
    /// <param name="skipToolbelt">A flag indicating whether to skip the toolbelt slots.</param>
    /// <returns>The remaining amount of the item that could not be added.</returns>
    private int AddToEmpty(ItemSO itemSO, int amount, int rarityId, bool skipToolbelt) {
        var relevantSlots = GetRelevantSlots(skipToolbelt).ToList();
        foreach (var slot in relevantSlots.Where(x => x.Item == null)) {
            slot.Item = itemSO;
            slot.RarityId = rarityId;
            int addable = Math.Min(itemSO.MaxStackableAmount, amount);
            slot.Amount = addable;
            amount -= addable;

            if (amount == 0) {
                break;
            }
        }
        return amount;
    }
    #endregion


    #region Check To Add Item
    /// <summary>
    /// Determines whether an item can be added to the container.
    /// </summary>
    /// <param name="itemId">The ID of the item to add.</param>
    /// <param name="amount">The amount of the item to add.</param>
    /// <param name="rarityId">The rarity ID of the item to add.</param>
    /// <param name="skipToolbelt">Optional. Indicates whether to skip checking the toolbelt. Default is false.</param>
    /// <returns>True if the item can be added, false otherwise.</returns>
    public bool CanAddItem(int itemId, int amount, int rarityId, bool skipToolbelt = false) {
        ValidateItemParameters(itemId, amount);
        var itemSO = GetItemSO(itemId);

        int remaining = itemSO.IsStackable ?
            CheckExisting(itemSO, amount, rarityId, skipToolbelt) :
            CheckEmpty(amount, skipToolbelt);
        return remaining <= 0;
    }

    /// <summary>
    /// Checks if an item already exists in the container and calculates the remaining amount needed.
    /// </summary>
    /// <param name="itemSO">The item to check.</param>
    /// <param name="amount">The desired amount of the item.</param>
    /// <param name="rarityId">The rarity ID of the item.</param>
    /// <param name="skipToolbelt">Flag indicating whether to skip the toolbelt slots.</param>
    /// <returns>The remaining amount needed after checking the container.</returns>
    private int CheckExisting(ItemSO itemSO, int amount, int rarityId, bool skipToolbelt = false) {
        var relevantSlots = GetRelevantSlots(skipToolbelt).ToList();
        foreach (var slot in relevantSlots) {
            if (slot.Item == itemSO && slot.Amount < itemSO.MaxStackableAmount && slot.RarityId == rarityId) {
                amount -= itemSO.MaxStackableAmount - slot.Amount;
                if (amount <= 0) {
                    break;
                }
            }
        }
        return amount;
    }

    /// <summary>
    /// Checks if there are empty slots in the item container.
    /// </summary>
    /// <param name="amount">The amount to check.</param>
    /// <param name="skipToolbelt">Whether to skip checking the toolbelt slots.</param>
    /// <returns>Zero if there are empty slots, otherwise the specified amount.</returns>
    private int CheckEmpty(int amount, bool skipToolbelt = false) => GetRelevantSlots(skipToolbelt).Any(x => x.Item == null) ? 0 : amount;

    #endregion


    #region Remove Item
    /// <summary>
    /// Removes a specified amount of an item with a given ID and rarity from the item container.
    /// </summary>
    /// <param name="itemId">The ID of the item to remove.</param>
    /// <param name="amount">The amount of the item to remove.</param>
    /// <param name="rarityId">The rarity ID of the item to remove.</param>
    /// <returns>True if the item was successfully removed, false otherwise.</returns>
    public bool RemoveItem(int itemId, int amount, int rarityId) {
        var combinedItems = CombineItemsByTypeAndRarity(ItemSlots);
        var targetItemSlot = combinedItems.FirstOrDefault(x => x.Item.ItemId == itemId && x.RarityId == rarityId);

        if (targetItemSlot == null || targetItemSlot.Amount < amount) {
            return false;
        }

        RemoveItemAmount(itemId, amount, rarityId);
        UIUpdate();
        return true;
    }

    /// <summary>
    /// Combines the items in the provided list by type and rarity.
    /// </summary>
    /// <param name="itemSlots">The list of item slots to combine.</param>
    /// <returns>A new list of item slots with combined items.</returns>
    public List<ItemSlot> CombineItemsByTypeAndRarity(List<ItemSlot> itemSlots) {
        return itemSlots
            .Where(slot => slot.Item != null)
            .GroupBy(slot => new { slot.Item.ItemId, slot.RarityId })
            .Select(g => new ItemSlot {
                Item = g.First().Item,
                Amount = g.Sum(x => x.Amount),
                RarityId = g.Key.RarityId
            })
            .ToList();
    }

    /// <summary>
    /// Removes a specified amount of items with a given item ID and rarity ID from the item container.
    /// </summary>
    /// <param name="itemId">The ID of the item to remove.</param>
    /// <param name="amount">The amount of items to remove.</param>
    /// <param name="rarityId">The rarity ID of the items to remove.</param>
    private void RemoveItemAmount(int itemId, int amount, int rarityId) {
        foreach (var itemSlot in ItemSlots.Where(x => x.Item.ItemId == itemId && x.RarityId == rarityId).ToList()) {
            int removalAmount = Math.Min(amount, itemSlot.Amount);
            itemSlot.Amount -= removalAmount;
            amount -= removalAmount;

            if (itemSlot.Amount <= 0) {
                itemSlot.Clear();
            }

            if (amount == 0) {
                break;
            }
        }
    }
    #endregion

    /// <summary>
    /// Retrieves the relevant item slots from the container, skipping the toolbelt if specified.
    /// </summary>
    /// <param name="skipToolbelt">Determines whether to skip the toolbelt slots.</param>
    /// <returns>An enumerable collection of relevant item slots.</returns>
    private IEnumerable<ItemSlot> GetRelevantSlots(bool skipToolbelt) {
        int skip = skipToolbelt ? PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1] : 0;
        return ItemSlots.Skip(skip);
    }

    /// <summary>
    /// Validates the parameters for an item.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="amount">The amount of the item.</param>
    /// <exception cref="ArgumentException">Thrown when the itemId is less than 0 or the amount is less than 1.</exception>
    private void ValidateItemParameters(int itemId, int amount) {
        if (itemId < 0 || amount < 1) {
            throw new ArgumentException("Invalid itemId or amount");
        }
    }

    private ItemSO GetItemSO(int itemId) {
        var itemSO = ItemManager.Instance.ItemDatabase[itemId];
        if (itemSO == null) {
            throw new ArgumentException($"Item with ID {itemId} not found in database.");
        }
        return itemSO;
    }

    /// <summary>
    /// Sorts the items in the item container.
    /// </summary>
    public void SortItems() {
        int toolbeltSize = PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1];
        var itemTuples = ItemSlots
            .Skip(toolbeltSize)
            .Where(slot => slot.Item != null)
            .Select(slot => (slot.Item, slot.Amount, slot.RarityId))
            .OrderBy(tuple => tuple.Item.ItemType)
            .ThenBy(tuple => tuple.RarityId)
            .ToList();

        foreach (var slot in ItemSlots.Skip(toolbeltSize)) {
            slot.Clear();
        }

        int index = toolbeltSize;
        foreach (var (Item, Amount, RarityId) in itemTuples) {
            if (index < ItemSlots.Count) {
                ItemSlots[index++].Set(Item.ItemId, Amount, RarityId);
            }
        }
        UIUpdate();
    }


    /// <summary>
    /// Shifts the slots in the item container by the specified amount.
    /// </summary>
    /// <param name="shiftAmount">The amount by which to shift the slots. Positive values shift the slots to the right, while negative values shift the slots to the left.</param>
    public void ShiftSlots(int shiftAmount) {
        var shiftedSlots = new List<ItemSlot>(ItemSlots.Count);
        int slotsCount = ItemSlots.Count;
        for (int i = 0; i < slotsCount; i++) {
            int newIndex = (i + shiftAmount) % slotsCount;
            newIndex = newIndex < 0 ? newIndex + slotsCount : newIndex;
            shiftedSlots.Add(ItemSlots[newIndex]); // Correctly add instead of insert
        }
        ItemSlots = shiftedSlots;

        UIUpdate();
    }

    /// <summary>
    /// Shoots an item from the specified slot in the item container.
    /// </summary>
    /// <param name="itemSlotIndex">The index of the slot to shoot the item from.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the slot index is out of range.</exception>
    public void ShootItem(int itemSlotIndex) {
        if (itemSlotIndex < 0 || itemSlotIndex >= ItemSlots.Count) {
            throw new ArgumentOutOfRangeException(nameof(itemSlotIndex), "Slot index is out of range.");
        }

        var slot = ItemSlots[itemSlotIndex];

        if (--slot.Amount <= 0) {
            slot.Clear(); 
        }

        UIUpdate();
    }

    /// <summary>
    /// Updates the UI and invokes the OnItemsUpdated event.
    /// </summary>
    public void UIUpdate() => OnItemsUpdated?.Invoke();
}