using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Represents a container holding multiple ItemSlots (e.g., player inventory).
/// Supports adding, removing, sorting, and saving/loading items.
/// Optimized for clarity and mod-friendly extension.
/// </summary>
[CreateAssetMenu(menuName = "Container/Item Container SO")]
public class ItemContainerSO : ScriptableObject {
#if UNITY_EDITOR
    [CustomEditor(typeof(ItemContainerSO))]
    public class ItemContainerEditor : Editor {
        public override void OnInspectorGUI() {
            var itemContainer = target as ItemContainerSO;
            if (GUILayout.Button("Clear container")) {
                foreach (var slot in itemContainer._itemSlots) {
                    slot.Clear();
                }
            }

            DrawDefaultInspector();
        }
    }
#endif

    // Invoked whenever the item list is updated, allowing UI to refresh.
    public event Action OnItemsUpdated;

    // Serialized Fields
    [SerializeField] List<ItemSlot> _itemSlots = new();

    // Provides a read-only view of the current item slots.
    public IReadOnlyList<ItemSlot> ItemSlots => _itemSlots.AsReadOnly();

    /// <summary>
    /// Sets up the container with a specified number of empty slots.
    /// </summary>
    public void Initialize(int slotsAmount) {
        _itemSlots = new List<ItemSlot>(slotsAmount);
        for (int i = 0; i < slotsAmount; i++) {
            _itemSlots.Add(new ItemSlot());
        }
    }

    #region Add Item
    /// <summary>
    /// Attempts to add the given item to the container.
    /// If the item is stackable, it tries to stack it onto existing slots first, then into empty slots.
    /// </summary>
    public int AddItem(ItemSlot itemSlot, bool skipToolbelt) {
        if (itemSlot == null || itemSlot.IsEmpty) {
            Debug.LogError("Invalid itemSlot in AddItem.");
            return 0;
        }

        var itemSO = ItemManager.Instance.ItemDatabase[itemSlot.ItemId];
        if (itemSO == null) {
            Debug.LogError($"No valid ItemSO found for itemId {itemSlot.ItemId} in AddItem.");
            return 0;
        }

        int remaining = itemSO.IsStackable ? AddToExisting(itemSlot, skipToolbelt) : AddToEmpty(itemSlot, skipToolbelt);
        UpdateUI();
        return remaining;
    }

    int AddToExisting(ItemSlot itemSlot, bool skipToolbelt) {
        var slots = GetRelevantSlots(skipToolbelt);

        // First try to stack on existing similar items.
        foreach (var slot in slots) {
            if (slot.CanStackWith(itemSlot)) {
                int maxStack = ItemManager.Instance.GetMaxStackableAmount(slot.ItemId);
                int addable = Mathf.Min(maxStack - slot.Amount, itemSlot.Amount);
                slot.AddAmount(addable, maxStack);
                itemSlot.RemoveAmount(addable);
                if (itemSlot.Amount <= 0) return 0;
            }
        }

        // If there's still remainder, try placing in empty slots.
        return AddToEmpty(itemSlot, skipToolbelt);
    }

    int AddToEmpty(ItemSlot itemSlot, bool skipToolbelt) {
        var slots = GetRelevantSlots(skipToolbelt);
        int maxStack = ItemManager.Instance.GetMaxStackableAmount(itemSlot.ItemId);

        foreach (var slot in slots) {
            if (slot.IsEmpty) {
                slot.Set(new ItemSlot(itemSlot.ItemId, 0, itemSlot.RarityId));
                int addable = Mathf.Min(maxStack, itemSlot.Amount);
                slot.AddAmount(addable, maxStack);
                itemSlot.RemoveAmount(addable);
                if (itemSlot.Amount <= 0) break;
            }
        }

        return itemSlot.Amount; // Return any leftover.
    }

    #endregion


    #region Check To Add Item
    /// <summary>
    /// Checks if the given item can be fully added to the container.
    /// Returns true if all items fit, false otherwise.
    /// </summary>
    public bool CanAddItem(ItemSlot itemSlot, bool skipToolbelt = false) {
        if (itemSlot.IsEmpty) {
            Debug.LogError("Invalid itemId or amount.");
        }

        var itemSO = ItemManager.Instance.ItemDatabase[itemSlot.ItemId];
        int remaining = itemSO.IsStackable ? CheckExisting(itemSlot, itemSO, skipToolbelt) : CheckEmpty(itemSlot, itemSO, skipToolbelt);

        return remaining <= 0;
    }

    private int CheckExisting(ItemSlot itemSlot, ItemSO itemSO, bool skipToolbelt = false) {
        int remaining = itemSlot.Amount;
        var slots = GetRelevantSlots(skipToolbelt);

        // Check stacking possibilities.
        int maxStack = itemSO.MaxStackableAmount;
        foreach (var slot in slots) {
            if (slot.CanStackWith(itemSlot)) {
                int canFit = maxStack - slot.Amount;
                int addable = Math.Min(canFit, remaining);
                remaining -= addable;
                if (remaining == 0) break;
            }
        }

        if (remaining > 0 && itemSO.IsStackable) {
            // Check if empty slots can hold the rest.
            remaining = CheckEmpty(new ItemSlot(itemSlot.ItemId, remaining, itemSlot.RarityId), itemSO, skipToolbelt);
        }

        return remaining;
    }

    int CheckEmpty(ItemSlot itemSlot, ItemSO itemSO, bool skipToolbelt = false) {
        int remaining = itemSlot.Amount;
        var slots = GetRelevantSlots(skipToolbelt);

        foreach (var slot in slots) {
            if (slot.IsEmpty) {
                int addable = Mathf.Min(itemSO.MaxStackableAmount, remaining);
                remaining -= addable;
                if (remaining == 0) break;
            }
        }

        return remaining;
    }

    #endregion


    #region Remove Item
    /// <summary>
    /// Removes a specified amount of a particular item from the container if available.
    /// Returns true if successfully removed, false otherwise.
    /// </summary>
    public bool RemoveItem(ItemSlot itemSlot) {
        if (itemSlot.IsEmpty) {
            Debug.LogWarning("Attempted to remove an empty item slot.");
            return false;
        }

        // Combine for easy checking if enough items exist.
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

            if (filteredItemSlot.IsEmpty || filteredItemSlot.Amount <= 0) {
                filteredItemSlot.Clear();
            }

            if (itemSlot.IsEmpty || itemSlot.Amount <= 0) {
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
        int toolbeltSize = PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1];
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
            int toolbeltSize = PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1];
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