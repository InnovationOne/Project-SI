using System;

[Serializable]
public class ItemSlot {
    public ItemSO Item;
    public int Amount;
    public int RarityId;


    public ItemSlot() => Clear();

    public ItemSlot(int itemId, int amount, int rarityId) => InitializeSlot(itemId, amount, rarityId);
        
    public void Set(int itemId, int amount, int rarityId) => InitializeSlot(itemId, amount, rarityId);

    public void Copy(ItemSlot itemSlot) => InitializeSlot(itemSlot.Item.ItemId, itemSlot.Amount, itemSlot.RarityId);
    
    private void InitializeSlot(int itemId, int amount, int rarityId) {
        Item = FetchItemFromDatabase(itemId);
        Amount = amount;
        RarityId = rarityId;
    }

    private ItemSO FetchItemFromDatabase(int itemId) {
        var item = ItemManager.Instance.ItemDatabase[itemId];
        return item == null ? throw new InvalidOperationException("Item not found.") : item;
    }

    public void Clear() {
        Item = null;
        Amount = 0;
        RarityId = 0;
    }
}
