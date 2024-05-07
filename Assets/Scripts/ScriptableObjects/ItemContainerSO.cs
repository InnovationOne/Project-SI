using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// This script creates a list of item slots e.g. inventory
[CreateAssetMenu(menuName = "Container/Item Container SO")]
public class ItemContainerSO : ScriptableObject {
    public event Action OnItemsUpdated;

    [SerializeField] private List<ItemSlot> _itemSlots = new();
    public IReadOnlyList<ItemSlot> ItemSlots => _itemSlots.AsReadOnly();

    /// <summary>
    /// Initializes the item container with the specified number of slots.
    /// </summary>
    /// <param name="slotsAmount">The number of slots to initialize.</param>
    public void Initialize(int slotsAmount) {
        _itemSlots = new List<ItemSlot>(Enumerable.Repeat(new ItemSlot(), slotsAmount));
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
    public int AddItem(ItemSlot itemSlot, bool skipToolbelt) {
        ValidateItemParameters(itemSlot.ItemId, itemSlot.Amount);
        var itemSO = GetItemSO(itemSlot.ItemId);

        int remainingAmount = ItemManager.Instance.ItemDatabase[itemSlot.ItemId].IsStackable ?
            AddToExisting(itemSlot, skipToolbelt) :
            AddToEmpty(itemSlot, skipToolbelt);

        UpdateUI();
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
    private int AddToExisting(ItemSlot itemSlot, bool skipToolbelt) {
        var relevantSlots = GetRelevantSlots(skipToolbelt).ToList();
        foreach (var slot in relevantSlots) {
            if (slot.ItemId == itemSlot.ItemId && slot.Amount < ItemManager.Instance.ItemDatabase[itemSlot.ItemId].MaxStackableAmount && slot.RarityId == itemSlot.RarityId) {
                int addable = Math.Min(ItemManager.Instance.ItemDatabase[itemSlot.ItemId].MaxStackableAmount - slot.Amount, itemSlot.Amount);
                slot.Amount += addable;
                itemSlot.Amount -= addable;
                if (itemSlot.Amount == 0) {
                    return 0;
                }
            }
        }
        return AddToEmpty(itemSlot, skipToolbelt);
    }

    /// <summary>
    /// Adds an item to an empty slot in the item container.
    /// </summary>
    /// <param name="itemSO">The item to add.</param>
    /// <param name="amount">The amount of the item to add.</param>
    /// <param name="rarityId">The rarity ID of the item.</param>
    /// <param name="skipToolbelt">A flag indicating whether to skip the toolbelt slots.</param>
    /// <returns>The remaining amount of the item that could not be added.</returns>
    private int AddToEmpty(ItemSlot itemSlot, bool skipToolbelt) {
        var relevantSlots = GetRelevantSlots(skipToolbelt).ToList();
        foreach (var slot in relevantSlots.Where(x => x.ItemId == -1)) {
            slot.ItemId = itemSlot.ItemId;
            slot.RarityId = itemSlot.RarityId;
            int addable = Math.Min(ItemManager.Instance.ItemDatabase[itemSlot.ItemId].MaxStackableAmount, itemSlot.Amount);
            slot.Amount = addable;
            itemSlot.Amount -= addable;

            if (itemSlot.Amount == 0) {
                break;
            }
        }
        return itemSlot.Amount;
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
    public bool CanAddItem(ItemSlot itemSlot, bool skipToolbelt = false) {
        ValidateItemParameters(itemSlot.ItemId, itemSlot.Amount);
        var itemSO = GetItemSO(itemSlot.ItemId);

        int remaining = ItemManager.Instance.ItemDatabase[itemSlot.ItemId].IsStackable ?
            CheckExisting(itemSlot, skipToolbelt) :
            CheckEmpty(itemSlot.Amount, skipToolbelt);
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
    private int CheckExisting(ItemSlot itemSlot, bool skipToolbelt = false) {
        var relevantSlots = GetRelevantSlots(skipToolbelt).ToList();
        foreach (var slot in relevantSlots) {
            if (slot.ItemId == itemSlot.ItemId && slot.Amount < ItemManager.Instance.ItemDatabase[itemSlot.ItemId].MaxStackableAmount && slot.RarityId == itemSlot.RarityId) {
                itemSlot.Amount -= ItemManager.Instance.ItemDatabase[itemSlot.ItemId].MaxStackableAmount - slot.Amount;
                if (itemSlot.Amount <= 0) {
                    break;
                }
            }
        }
        return itemSlot.Amount;
    }

    /// <summary>
    /// Checks if there are empty slots in the item container.
    /// </summary>
    /// <param name="amount">The amount to check.</param>
    /// <param name="skipToolbelt">Whether to skip checking the toolbelt slots.</param>
    /// <returns>Zero if there are empty slots, otherwise the specified amount.</returns>
    private int CheckEmpty(int amount, bool skipToolbelt = false) => GetRelevantSlots(skipToolbelt).Any(x => x.ItemId == -1) ? 0 : amount;
    #endregion


    #region Remove Item
    /// <summary>
    /// Removes a specified amount of an item with a given ID and rarity from the item container.
    /// </summary>
    /// <param name="itemId">The ID of the item to remove.</param>
    /// <param name="amount">The amount of the item to remove.</param>
    /// <param name="rarityId">The rarity ID of the item to remove.</param>
    /// <returns>True if the item was successfully removed, false otherwise.</returns>
    public bool RemoveItem(ItemSlot itemSlot) {
        var combinedItems = CombineItemsByTypeAndRarity();
        var targetItemSlot = combinedItems.FirstOrDefault(x => x.ItemId == itemSlot.ItemId && x.RarityId == itemSlot.RarityId);

        if (targetItemSlot == null || targetItemSlot.Amount < itemSlot.Amount) {
            return false;
        }

        RemoveItemAmount(itemSlot);
        UpdateUI();
        return true;
    }

    /// <summary>
    /// Combines the items in the provided list by type and rarity.
    /// </summary>
    /// <param name="itemSlots">The list of item slots to combine.</param>
    /// <returns>A new list of item slots with combined items.</returns>
    public List<ItemSlot> CombineItemsByTypeAndRarity() {
        return _itemSlots
            .Where(slot => ItemManager.Instance.ItemDatabase[slot.ItemId] != null)
            .GroupBy(slot => new { slot.ItemId, slot.RarityId })
            .Select(g => new ItemSlot {
                ItemId = g.First().ItemId,
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
    private void RemoveItemAmount(ItemSlot itemSlot) {
        var filteredItemSlots = _itemSlots
            .Where(x => x != null && ItemManager.Instance.ItemDatabase[x.ItemId] != null && x.ItemId == itemSlot.ItemId && x.RarityId == itemSlot.RarityId)
            .ToList();

        foreach (var filteredItemSlot in filteredItemSlots) {
            int removalAmount = Math.Min(itemSlot.Amount, filteredItemSlot.Amount);
            filteredItemSlot.Amount -= removalAmount;
            itemSlot.Amount -= removalAmount;

            if (filteredItemSlot.Amount <= 0) {
                filteredItemSlot.Clear();
            }

            if (itemSlot.Amount == 0) {
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
    private IEnumerable<ItemSlot> GetRelevantSlots(bool skipToolbelt) => _itemSlots.Skip(skipToolbelt ? PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1] : 0);
    
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
        return itemSO == null ? throw new ArgumentException($"Item with ID {itemId} not found in database.") : itemSO;
    }

    /// <summary>
    /// Sorts the items in the item container.
    /// </summary>
    public void SortItems() {
        int toolbeltSize = PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1];
        var itemSlots = _itemSlots
            .Skip(toolbeltSize)
            .Where(slot => ItemManager.Instance.ItemDatabase[slot.ItemId] != null)
            .Select(slot => new ItemSlot(slot.ItemId, slot.Amount, slot.RarityId))
            .OrderBy(slot => ItemManager.Instance.ItemDatabase[slot.ItemId].ItemType)
            .ThenBy(slot => slot.RarityId)
            .ToList();

        ClearItemContainer();

        int index = toolbeltSize;
        foreach (var itemSlot in itemSlots) {
            if (index < _itemSlots.Count) {
                _itemSlots[index].Set(itemSlot);
            }
            index++;
        }
        UpdateUI();
    }

    /// <summary>
    /// Shifts the slots in the item container by the specified amount.
    /// </summary>
    /// <param name="shiftAmount">The amount by which to shift the slots. Positive values shift the slots to the right, while negative values shift the slots to the left.</param>
    public void ShiftSlots(int shiftAmount) {
        var shiftedSlots = new List<ItemSlot>(_itemSlots.Count);
        int slotsCount = _itemSlots.Count;
        for (int i = 0; i < slotsCount; i++) {
            int newIndex = (i + shiftAmount) % slotsCount;
            newIndex = newIndex < 0 ? newIndex + slotsCount : newIndex;
            shiftedSlots.Add(_itemSlots[newIndex]); // Correctly add instead of insert
        }
        _itemSlots = shiftedSlots;

        UpdateUI();
    }

    /// <summary>
    /// Clears the item container by clearing all the slots except for the toolbelt slots.
    /// </summary>
    public void ClearItemContainer() => _itemSlots.Skip(PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1]).ToList().ForEach(slot => slot.Clear());


    /// <summary>
    /// Clears the item slot at the specified ID.
    /// </summary>
    /// <param name="id">The ID of the item slot to clear.</param>
    public void ClearItemSlot(int id) => _itemSlots[id].Clear();

    /// <summary>
    /// Updates the UI and invokes the OnItemsUpdated event.
    /// </summary>
    public void UpdateUI() => OnItemsUpdated?.Invoke();
}