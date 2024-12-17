using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class ItemSlot : INetworkSerializable {
    public const int EmptyItemId = -1;

    [SerializeField] int _itemId;
    public int ItemId {
        get => _itemId;
        private set => _itemId = value;
    }

    [SerializeField] int _amount;
    public int Amount {
        get => _amount;
        private set => _amount = value;
    }

    [SerializeField] int _rarityId;
    public int RarityId {
        get => _rarityId;
        private set => _rarityId = value;
    }

    public bool IsEmpty => ItemId == EmptyItemId;

    public ItemSlot() {
        Clear();
    }

    public ItemSlot(int itemId, int amount, int rarityId) => InitializeSlot(itemId, amount, rarityId);

    public void Set(ItemSlot itemSlot) => InitializeSlot(itemSlot);

    void InitializeSlot(ItemSlot itemSlot) {
        _itemId = itemSlot._itemId;
        _amount = itemSlot._amount;
        _rarityId = itemSlot._rarityId;
    }

    void InitializeSlot(int itemId, int amount, int rarityId) {
        _itemId = itemId;
        _amount = amount;
        _rarityId = rarityId;
    }

    public void Clear() {
        _itemId = EmptyItemId;
        _amount = 0;
        _rarityId = 0;
    }

    public int AddAmount(int amountToAdd, int maxStackableAmount) {
        if (IsEmpty) {
            Debug.LogWarning("Cannot add amount to an empty slot. Set the item first.");
            return 0;
        }

        int spaceLeft = maxStackableAmount - _amount;
        int actualAdded = Mathf.Min(amountToAdd, spaceLeft);
        _amount += actualAdded;
        return actualAdded;
    }

    public int RemoveAmount(int amountToRemove) {
        int actualRemoved = Mathf.Min(amountToRemove, _amount);
        _amount -= actualRemoved;

        if (_amount == 0) {
            Clear();
        }

        return actualRemoved;
    }

    public bool CanStackWith(ItemSlot other) {
        if (IsEmpty || other.IsEmpty) return false;
        if (_itemId != other._itemId || _rarityId != other._rarityId) return false;
        int maxStack = ItemManager.Instance.GetMaxStackableAmount(_itemId);
        return _amount < maxStack;
    }

    public void SwapWith(ItemSlot other) {
        (_itemId, other._itemId) = (other._itemId, _itemId);
        (_amount, other._amount) = (other._amount, _amount);
        (_rarityId, other._rarityId) = (other._rarityId, _rarityId);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref _itemId);
        serializer.SerializeValue(ref _amount);
        serializer.SerializeValue(ref _rarityId);
    }
}
