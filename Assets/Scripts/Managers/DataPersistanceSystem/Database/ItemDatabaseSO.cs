using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This script is for a database of items
[CreateAssetMenu(menuName = "Database/Item Database")]
public class ItemDatabaseSO : ScriptableObject {
    [Header("All items in the game, item id = place in list")]
    public List<ItemSO> Items;

    public void SetItemID() {
        Items = Items.Select((item, index) => {
            item.ItemID = index;
            return item;
        }).ToList();
    }

    public void SetItemTypeID() {
        var sortedItems = Items
            .OrderBy(x => x.ItemType)
            .ThenBy(x => x.ItemName)
            .ToList();

        // Set the itemTypeId for each item
        int runningCount = 0;
        ItemTypes currentItemType = sortedItems[0].ItemType;
        string currentItemName = sortedItems[0].ItemName;

        for (int i = 0; i < sortedItems.Count; i++) {
            // If the item names no longer match, set the new item name and up the running count
            if (sortedItems[i].ItemName != currentItemName) {
                currentItemName = sortedItems[i].ItemName;
                runningCount++;
            }

            // If the item type no longer match, set the new item type and reset the running count
            if (sortedItems[i].ItemType != currentItemType) {
                currentItemType = sortedItems[i].ItemType;
                runningCount = 0;
            }

            // Set the item type id to the item where the item id matches
            Items.Where(x => x.ItemID == sortedItems[i].ItemID)
                .FirstOrDefault()
                .ItemTypeID = runningCount;
        }
    }

    public ItemSO GetItemFromItemId(int itemID) {
        return Items.Find(itemSO => itemSO.ItemID == itemID);
    }
}
