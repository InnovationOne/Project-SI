using System.Collections.Generic;
using UnityEngine;

// This script is for a database of items
[CreateAssetMenu(menuName = "Database/Item Database")]
public class ItemDatabaseSO : ScriptableObject {
    // List of items in the database
    [SerializeField] private List<ItemSO> _items = new();
    // Cache to store items by their IDs for fast lookup
    private Dictionary<int, ItemSO> _cache = new();

    /// <summary>
    /// Initializes the items in the items database on Start() and cache all items.
    /// </summary>
    public void InitializeItems() {
        for (int i = 0; i < _items.Count; i++) {
            _items[i].ItemId = i;
            _cache[i] = _items[i]; // Populate the cache
        }
    }

    /// <summary>
    /// Indexer to access items by their IDs from the cache
    /// </summary>
    public ItemSO this[int itemId] {
        get {
            if (_cache.TryGetValue(itemId, out var item)) {
                return item;
            } else if (itemId == -1) {
                return null;            
            } else {
                throw new KeyNotFoundException($"Item with ID {itemId} does not exist in the cache.");
            }
        }
    }
}
