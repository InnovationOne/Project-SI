using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This script creates a list of item slots e.g. inventory
[CreateAssetMenu(menuName = "Container/Item Container SO")]
public class ItemContainerSO : ScriptableObject {
    // Constants for clarity
    private const int EmptyItemId = ItemSlot.EmptyItemId;

    // Events
    public event Action OnItemsUpdated;

    // Serialized Fields
    [SerializeField] private List<ItemSlot> _itemSlots = new();

    // Public Properties
    public IReadOnlyList<ItemSlot> ItemSlots => _itemSlots.AsReadOnly();

    // Cached References
    private ItemManager _itemManager;
    private PlayerToolbeltController _playerToolbeltController;

    /// <summary>
    /// Initializes the item container with the specified number of slots.
    /// </summary>
    /// <param name="slotsAmount">The number of slots to initialize.</param>
    public void Initialize(int slotsAmount) {
        _itemManager = ItemManager.Instance;
        _playerToolbeltController = PlayerToolbeltController.LocalInstance;

        _itemSlots = new List<ItemSlot>(slotsAmount);
        for (int i = 0; i < slotsAmount; i++) {
            _itemSlots.Add(new ItemSlot());
        }
    }

    #region Add Item
    /// <summary>
    /// Adds an item to the container.
    /// </summary>
    /// <param name="itemSlot">The item slot containing item data to add.</param>
    /// <param name="skipToolbelt">A flag indicating whether to skip adding the item to the toolbelt.</param>
    /// <returns>The remaining amount of the item after adding.</returns>
    public int AddItem(ItemSlot itemSlot, bool skipToolbelt) {
        if (itemSlot.IsEmpty) {
            Debug.LogError("Invalid itemId or amount.");
        }

        var itemSO = _itemManager.ItemDatabase[itemSlot.ItemId];

        int remainingAmount = ItemManager.Instance.ItemDatabase[itemSlot.ItemId].IsStackable
            ? AddToExisting(itemSlot, itemSO, skipToolbelt)
            : AddToEmpty(itemSlot, itemSO, skipToolbelt);

        UpdateUI();
        return remainingAmount;
    }

    /// <summary>
    /// Adds the specified stackable item to existing slots that can accommodate it.
    /// </summary>
    /// <param name="itemSlot">The item slot to add.</param>
    /// <param name="itemData">Cached item data.</param>
    /// <param name="skipToolbelt">Flag to skip toolbelt slots.</param>
    /// <returns>The remaining amount after attempting to add.</returns>
    private int AddToExisting(ItemSlot itemSlot, ItemSO itemSO, bool skipToolbelt) {
        var relevantSlots = GetRelevantSlots(skipToolbelt);

        foreach (var slot in relevantSlots) {
            if (slot.CanStackWith(itemSlot, _itemManager)) {
                int maxStackable = _itemManager.GetMaxStackableAmount(slot.ItemId);
                int addable = Mathf.Min(maxStackable - slot.Amount, itemSlot.Amount);
                slot.AddAmount(addable, maxStackable);
                itemSlot.RemoveAmount(addable);

                if (itemSlot.IsEmpty) {
                    break;
                }
            }
        }

        // Attempt to add remaining items to empty slots
        return AddToEmpty(itemSlot, itemSO, skipToolbelt);
    }

    /// <summary>
    /// Adds the specified item to empty slots.
    /// </summary>
    /// <param name="itemSlot">The item slot to add.</param>
    /// <param name="itemData">Cached item data.</param>
    /// <param name="skipToolbelt">Flag to skip toolbelt slots.</param>
    /// <returns>The remaining amount after attempting to add.</returns>
    private int AddToEmpty(ItemSlot itemSlot, ItemSO itemSO, bool skipToolbelt) {
        var relevantSlots = GetRelevantSlots(skipToolbelt);

        foreach (var slot in relevantSlots.Where(x => x.ItemId == -1)) {
            slot.Set(new ItemSlot(itemSlot.ItemId, 0, itemSlot.RarityId));
            int addable = Mathf.Min(ItemManager.Instance.GetMaxStackableAmount(itemSlot.ItemId), itemSlot.Amount);
            int actualAdded = slot.AddAmount(addable, ItemManager.Instance.GetMaxStackableAmount(itemSlot.ItemId));
            itemSlot.RemoveAmount(actualAdded);

            if (itemSlot.IsEmpty) {
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
    /// <param name="itemSlot">The item slot containing item data to check.</param>
    /// <param name="skipToolbelt">Optional. Indicates whether to skip checking the toolbelt. Default is false.</param>
    /// <returns>True if the item can be added, false otherwise.</returns>
    public bool CanAddItem(ItemSlot itemSlot, bool skipToolbelt = false) {
        if (itemSlot.IsEmpty) {
            Debug.LogError("Invalid itemId or amount.");
        }

        var itemSO = _itemManager.ItemDatabase[itemSlot.ItemId];

        int remainingAmount = ItemManager.Instance.ItemDatabase[itemSlot.ItemId].IsStackable
            ? CheckExisting(itemSlot, itemSO, skipToolbelt)
            : CheckEmpty(itemSlot, itemSO, skipToolbelt);

        return remainingAmount <= 0;
    }

    /// <summary>
    /// Checks if an item can be added to existing stackable slots.
    /// </summary>
    /// <param name="itemSlot">The item slot to check.</param>
    /// <param name="itemData">Cached item data.</param>
    /// <param name="skipToolbelt">Flag to skip toolbelt slots.</param>
    /// <returns>The remaining amount that cannot be accommodated.</returns>
    private int CheckExisting(ItemSlot itemSlot, ItemSO itemSO, bool skipToolbelt = false) {
        int remainingAmount = itemSlot.Amount;
        var relevantSlots = GetRelevantSlots(skipToolbelt);

        foreach (var slot in relevantSlots) {
            if (slot.CanStackWith(itemSlot, _itemManager)) {
                int addable = Math.Min(ItemManager.Instance.ItemDatabase[itemSlot.ItemId].MaxStackableAmount - slot.Amount, itemSlot.Amount);
                remainingAmount -= addable;

                if (remainingAmount == 0) {
                    break;
                }
            }
        }

        if (remainingAmount > 0 && itemSO.IsStackable) {
            remainingAmount = CheckEmpty(new ItemSlot(itemSlot.ItemId, remainingAmount, itemSlot.RarityId), itemSO, skipToolbelt);
        }

        return remainingAmount;
    }

    /// <summary>
    /// Checks if there are empty slots available for the item.
    /// </summary>
    /// <param name="itemSlot">The item slot to check.</param>
    /// <param name="itemData">Cached item data.</param>
    /// <param name="skipToolbelt">Flag to skip toolbelt slots.</param>
    /// <returns>The remaining amount that cannot be accommodated.</returns>
    private int CheckEmpty(ItemSlot itemSlot, ItemSO itemSO, bool skipToolbelt = false) {
        int remainingAmount = itemSlot.Amount;
        var relevantSlots = GetRelevantSlots(skipToolbelt);

        foreach (var slot in relevantSlots.Where(s => s.IsEmpty)) {
            int addable = Mathf.Min(itemSO.MaxStackableAmount, remainingAmount);
            remainingAmount -= addable;

            if (remainingAmount == 0) {
                break;
            }
        }

        return remainingAmount;
    }
    #endregion


    #region Remove Item
    /// <summary>
    /// Removes a specified amount of an item from the container.
    /// </summary>
    /// <param name="itemSlot">The item slot containing item data to remove.</param>
    /// <returns>True if the item was successfully removed, false otherwise.</returns>
    public bool RemoveItem(ItemSlot itemSlot) {
        if (itemSlot.IsEmpty) {
            Debug.LogWarning("Attempted to remove an empty item slot.");
            return false;
        }

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
    /// Combines the items in the container by type and rarity.
    /// </summary>
    /// <returns>A new list of item slots with combined items.</returns>
    public List<ItemSlot> CombineItemsByTypeAndRarity() {
        return _itemSlots
            .Where(slot => !slot.IsEmpty)
            .GroupBy(slot => new { slot.ItemId, slot.RarityId })
            .Select(g => new ItemSlot(g.Key.ItemId, g.Sum(x => x.Amount), g.Key.RarityId))
            .ToList();
    }

    /// <summary>
    /// Removes a specified amount of items from the inventory.
    /// </summary>
    /// <param name="itemSlot">The item slot containing item data to remove.</param>
    private void RemoveItemAmount(ItemSlot itemSlot) {
        var filteredItemSlots = _itemSlots
            .Where(x => !itemSlot.IsEmpty &&
                        x.ItemId == itemSlot.ItemId &&
                        x.RarityId == itemSlot.RarityId)
            .ToList();

        foreach (var filteredItemSlot in filteredItemSlots) {
            int removalAmount = Math.Min(itemSlot.Amount, filteredItemSlot.Amount);
            filteredItemSlot.RemoveAmount(removalAmount);
            itemSlot.RemoveAmount(removalAmount);

            if (filteredItemSlot.IsEmpty) {
                filteredItemSlot.Clear();
            }

            if (itemSlot.IsEmpty) {
                break;
            }
        }
    }
    #endregion


    #region Sorting and Shifting
    /// <summary>
    /// Sorts the items in the container based on item type and rarity.
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
                index++;
            } else {
                Debug.LogWarning("Not enough slots to accommodate all sorted items.");
                break;
            }

        }
        UpdateUI();
    }

    /// <summary>
    /// Shifts the slots in the container by the specified amount.
    /// </summary>
    /// <param name="shiftAmount">The amount by which to shift the slots. Positive values shift right; negative shift left.</param>
    public void ShiftSlots(int shiftAmount) {
        int slotsCount = _itemSlots.Count;
        if (slotsCount == 0) {
            return;
        }

        shiftAmount %= slotsCount;
        if (shiftAmount == 0) {
            return;
        }

        var shiftedSlots = new List<ItemSlot>(slotsCount);

        for (int i = 0; i < slotsCount; i++) {
            int newIndex = (i + shiftAmount) % slotsCount;
            if (newIndex < 0)
                newIndex += slotsCount;

            shiftedSlots.Add(_itemSlots[newIndex]);
        }

        _itemSlots = shiftedSlots;
        UpdateUI();
    }

    /// <summary>
    /// Clears the item container by clearing all the slots except for the toolbelt slots.
    /// </summary>
    public void ClearItemContainer() {
        int toolbeltSize = _playerToolbeltController.ToolbeltSizes[^1];
        foreach (var slot in _itemSlots.Skip(toolbeltSize)) {
            slot.Clear();
        }
    }

    /// <summary>
    /// Clears the item slot at the specified index.
    /// </summary>
    /// <param name="id">The index of the item slot to clear.</param>
    public void ClearItemSlot(int id) {
        if (id < 0 || id >= _itemSlots.Count) {
            Debug.LogWarning($"Invalid slot ID: {id}. Cannot clear.");
            return;
        }

        _itemSlots[id].Clear();
        UpdateUI();
    }
    #endregion


    #region Serialization    
    public string SaveItemContainer() {
        var itemContainerJson = new List<string>();
        foreach (var itemSlot in ItemSlots) {
            itemContainerJson.Add(JsonConvert.SerializeObject(itemSlot));
        }

        return JsonConvert.SerializeObject(itemContainerJson);
    }

    public void LoadItemContainer(string data) {
        if (!string.IsNullOrEmpty(data)) {
            var itemContainerJson = JsonConvert.DeserializeObject<List<string>>(data);
            foreach (var itemSlot in itemContainerJson) {
                AddItem(JsonConvert.DeserializeObject<ItemSlot>(itemSlot), false);
            }
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Retrieves the relevant item slots from the container, skipping the toolbelt if specified.
    /// </summary>
    /// <param name="skipToolbelt">Determines whether to skip the toolbelt slots.</param>
    /// <returns>An enumerable collection of relevant item slots.</returns>
    private IEnumerable<ItemSlot> GetRelevantSlots(bool skipToolbelt) {
        if (skipToolbelt) {
            int toolbeltSize = _playerToolbeltController.ToolbeltSizes[^1];
            return _itemSlots.Skip(toolbeltSize);
        }
        return _itemSlots;
    }

    /// <summary>
    /// Updates the UI and invokes the OnItemsUpdated event.
    /// </summary>
    public void UpdateUI() => OnItemsUpdated?.Invoke();
    #endregion
}