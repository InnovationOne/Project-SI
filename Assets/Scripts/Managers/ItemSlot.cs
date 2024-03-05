using System;

// This class handels a item slot
[Serializable]
public class ItemSlot {
    public ItemSO Item;
    public int Amount;
    public int RarityID;

    public ItemSlot() {
        Item = null;
        Amount = 0;
        RarityID = 0;
    }

    public ItemSlot(int itemID, int amount, int rarityID) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        Item = item;
        Amount = amount;
        RarityID = rarityID;
    }

    public void Set(int itemID, int amount, int rarityID) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        Item = item;
        Amount = amount;
        RarityID = rarityID;
    }

    public void Copy(int itemID, int amount, int rarityID) {
        var item = ItemManager.Instance.ItemDatabase.GetItemFromItemId(itemID);

        Item = item;
        Amount = amount;
        RarityID = rarityID;
    }

    public void Clear() {
        Item = null;
        Amount = 0;
        RarityID = 0;
    }
}
