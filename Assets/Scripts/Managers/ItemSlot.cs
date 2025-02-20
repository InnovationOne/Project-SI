using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class ItemSlot : INetworkSerializable {
    public const int EmptyItemId = -1;


    public int ItemId;
    public int Amount;
    public int RarityId;

    public bool IsEmpty => ItemId == EmptyItemId;

    public ItemSlot() {
        Clear();
    }

    public ItemSlot(int itemId, int amount, int rarityId) => InitializeSlot(itemId, amount, rarityId);

    public void Set(ItemSlot itemSlot) => InitializeSlot(itemSlot);

    void InitializeSlot(ItemSlot itemSlot) {
        ItemId = itemSlot.ItemId;
        Amount = itemSlot.Amount;
        RarityId = itemSlot.RarityId;
    }

    void InitializeSlot(int itemId, int amount, int rarityId) {
        ItemId = itemId;
        Amount = amount;
        RarityId = rarityId;
    }

    public void Clear() {
        ItemId = EmptyItemId;
        Amount = 0;
        RarityId = 0;
    }

    public int AddAmount(int amountToAdd, int maxStackableAmount) {
        if (IsEmpty) {
            Debug.LogWarning("Cannot add amount to an empty slot. Set the item first.");
            return 0;
        }

        int spaceLeft = maxStackableAmount - Amount;
        int actualAdded = Mathf.Min(amountToAdd, spaceLeft);
        Amount += actualAdded;
        return actualAdded;
    }

    public int RemoveAmount(int amountToRemove) {
        int actualRemoved = Mathf.Min(amountToRemove, Amount);
        Amount -= actualRemoved;

        if (Amount == 0) {
            Clear();
        }

        return actualRemoved;
    }

    public bool CanStackWith(ItemSlot other) {
        if (IsEmpty || other.IsEmpty) return false;
        if (ItemId != other.ItemId || RarityId != other.RarityId) return false;
        int maxStack = GameManager.Instance.ItemManager.GetMaxStackableAmount(ItemId);
        return Amount < maxStack;
    }

    public void SwapWith(ItemSlot other) {
        (ItemId, other.ItemId) = (other.ItemId, ItemId);
        (Amount, other.Amount) = (other.Amount, Amount);
        (RarityId, other.RarityId) = (other.RarityId, RarityId);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref ItemId);
        serializer.SerializeValue(ref Amount);
        serializer.SerializeValue(ref RarityId);
    }
}
