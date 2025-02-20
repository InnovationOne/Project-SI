using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    bool _ignoreToolbelt = false;

    // Provides a read-only view of the current item slots.
    public IReadOnlyList<ItemSlot> ItemSlots => _itemSlots.AsReadOnly();

    public void Initialize(int slotsAmount) {
        _itemSlots = new List<ItemSlot>(slotsAmount);
        for (int i = 0; i < slotsAmount; i++) {
            _itemSlots.Add(new ItemSlot());
        }
        UpdateUI();
    }

    public void MarkAsChestContainer() {
        _ignoreToolbelt = true;
    }

    #region Add Item
    public int AddItem(ItemSlot itemSlot, bool skipToolbelt) {
        // Quick sanity checks
        if (itemSlot == null || itemSlot.IsEmpty) {
            Debug.LogError("Invalid itemSlot in AddItem.");
            return 0;
        }
        if (GameManager.Instance == null || GameManager.Instance.ItemManager == null) {
            Debug.LogError("GameManager or ItemManager is not available yet!");
            return itemSlot.Amount;
        }

        var itemSO = GameManager.Instance.ItemManager.ItemDatabase[itemSlot.ItemId];
        if (itemSO == null) {
            Debug.LogError($"No valid ItemSO found for itemId {itemSlot.ItemId} in AddItem.");
            return itemSlot.Amount;
        }

        int remaining = itemSO.IsStackable
            ? AddToExisting(itemSlot, skipToolbelt)
            : AddToEmpty(itemSlot, skipToolbelt);

        UpdateUI();
        return remaining;
    }

    int AddToExisting(ItemSlot itemSlot, bool skipToolbelt) {
        var slots = GetRelevantSlots(skipToolbelt);

        // First try to stack on existing similar items.
        foreach (var slot in slots) {
            if (slot.CanStackWith(itemSlot)) {
                int maxStack = GameManager.Instance.ItemManager.GetMaxStackableAmount(slot.ItemId);
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
        int maxStack = GameManager.Instance.ItemManager.GetMaxStackableAmount(itemSlot.ItemId);

        foreach (var slot in slots) {
            if (slot.IsEmpty) {
                slot.Set(new ItemSlot(itemSlot.ItemId, 0, itemSlot.RarityId));
                int addable = Mathf.Min(maxStack, itemSlot.Amount);
                slot.AddAmount(addable, maxStack);
                itemSlot.RemoveAmount(addable);
                if (itemSlot.Amount <= 0) break;
            }
        }

        return itemSlot.Amount; // leftover
    }

    #endregion


    #region Check To Add Item
    public bool CanAddItem(ItemSlot itemSlot, bool skipToolbelt = false) {
        if (itemSlot.IsEmpty) {
            Debug.LogError("Invalid itemId or amount in CanAddItem.");
            return false;
        }
        if (GameManager.Instance == null || GameManager.Instance.ItemManager == null) return false;

        var itemSO = GameManager.Instance.ItemManager.ItemDatabase[itemSlot.ItemId];
        if (itemSO == null) return false;

        int remaining = itemSO.IsStackable
            ? CheckExisting(itemSlot, itemSO, skipToolbelt)
            : CheckEmpty(itemSlot, itemSO, skipToolbelt);

        return remaining <= 0;
    }

    int CheckExisting(ItemSlot itemSlot, ItemSO itemSO, bool skipToolbelt = false) {
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

        // If still leftover, check empty slots.
        if (remaining > 0 && itemSO.IsStackable) {
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

    public bool RemoveItem(ItemSlot itemSlot) {
        if (itemSlot.IsEmpty) {
            Debug.LogWarning("Attempted to remove an empty item slot.");
            return false;
        }

        var combinedItems = CombineItemsByTypeAndRarity();
        var targetItemSlot = combinedItems.FirstOrDefault(x => x.ItemId == itemSlot.ItemId && x.RarityId == itemSlot.RarityId);
        if (targetItemSlot == null || targetItemSlot.Amount < itemSlot.Amount) return false;

        RemoveItemAmount(itemSlot);
        UpdateUI();
        return true;
    }

    public List<ItemSlot> CombineItemsByTypeAndRarity() {
        return _itemSlots
            .Where(slot => !slot.IsEmpty)
            .GroupBy(slot => new { slot.ItemId, slot.RarityId })
            .Select(g => new ItemSlot(g.Key.ItemId, g.Sum(x => x.Amount), g.Key.RarityId))
            .ToList();
    }

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
            if (filteredItemSlot.IsEmpty || filteredItemSlot.Amount <= 0) filteredItemSlot.Clear();
            if (itemSlot.IsEmpty || itemSlot.Amount <= 0) break;
        }
    }
    #endregion


    #region Sorting and Shifting
    public void SortItems(bool skipToolbelt = true) {
        if (_ignoreToolbelt) skipToolbelt = false;

        var relevantSlots = GetRelevantSlots(skipToolbelt).ToList();

        // Gather identical items
        Dictionary<(int itemId, int rarityId), int> combined = new();
        foreach (var slot in relevantSlots) {
            if (!slot.IsEmpty) {
                var key = (slot.ItemId, slot.RarityId);
                if (!combined.ContainsKey(key)) combined[key] = 0;
                combined[key] += slot.Amount;
            }
        }
        // Clear existing
        foreach (var slot in relevantSlots) slot.Clear();

        // Turn combined items into a sorted list
        List<ItemSlot> sortedList = new();
        foreach (var (key, totalAmount) in combined) {
            sortedList.Add(new ItemSlot(key.itemId, totalAmount, key.rarityId));
        }
        sortedList.Sort((a, b) => {
            var itemA = GameManager.Instance.ItemManager.ItemDatabase[a.ItemId];
            var itemB = GameManager.Instance.ItemManager.ItemDatabase[b.ItemId];
            int compareType = itemA.ItemType.CompareTo(itemB.ItemType);
            if (compareType != 0) return compareType;
            return a.RarityId.CompareTo(b.RarityId);
        });

        // Distribute back to empty slots
        foreach (var combinedSlot in sortedList) {
            int leftover = combinedSlot.Amount;
            int maxStack = GameManager.Instance.ItemManager.GetMaxStackableAmount(combinedSlot.ItemId);
            foreach (var slot in relevantSlots) {
                if (leftover <= 0) break;
                if (slot.IsEmpty) {
                    slot.Set(new ItemSlot(combinedSlot.ItemId, 0, combinedSlot.RarityId));
                    int toAdd = Mathf.Min(maxStack, leftover);
                    slot.AddAmount(toAdd, maxStack);
                    leftover -= toAdd;
                }
            }
        }

        UpdateUI();
    }

    public void ShiftSlots(int shiftAmount) {
        int slotsCount = _itemSlots.Count;
        if (slotsCount == 0) return;

        shiftAmount %= slotsCount;
        if (shiftAmount == 0) return;

        var shiftedSlots = new List<ItemSlot>(slotsCount);

        for (int i = 0; i < slotsCount; i++) {
            int newIndex = (i + shiftAmount) % slotsCount;
            if (newIndex < 0) newIndex += slotsCount;
            shiftedSlots.Add(_itemSlots[newIndex]);
        }

        _itemSlots = shiftedSlots;
        UpdateUI();
    }

    public void ClearItemContainer() {
        int toolbeltSize = 0;
        if (!_ignoreToolbelt && PlayerController.LocalInstance != null) {
            toolbeltSize = PlayerController.LocalInstance.PlayerToolbeltController.ToolbeltSizes[^1];
        }

        foreach (var slot in _itemSlots.Skip(toolbeltSize)) {
            slot.Clear();
        }
        UpdateUI();
    }

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
        return JsonConvert.SerializeObject(_itemSlots);
    }

    public void LoadItemContainer(string data) {
        if (string.IsNullOrEmpty(data)) return;
        ClearItemContainer();

        // Deserialize directly into a List<ItemSlot>.
        var slots = JsonConvert.DeserializeObject<List<ItemSlot>>(data);
        if (slots == null) {
            Debug.LogError("Failed to deserialize ItemContainer data.");
            return;
        }

        for (int i = 0; i < slots.Count; i++) {
            // Set the slot if it is not empty.
            if (!slots[i].IsEmpty) {
                _itemSlots[i].Set(slots[i]);
            }
        }
    }
    #endregion

    #region Helper Methods
    private IEnumerable<ItemSlot> GetRelevantSlots(bool skipToolbelt) {
        // If this container is a chest or something that doesn't have the concept of a toolbelt, we always skip = false.
        if (_ignoreToolbelt) skipToolbelt = false;
        if (skipToolbelt) {
            int toolbeltSize = PlayerController.LocalInstance.PlayerToolbeltController.ToolbeltSizes[^1];
            return _itemSlots.Skip(toolbeltSize);
        }
        return _itemSlots;
    }

    public void UpdateUI() => OnItemsUpdated?.Invoke();
    #endregion
}