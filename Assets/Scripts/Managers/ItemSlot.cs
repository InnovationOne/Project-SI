using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class ItemSlot : INetworkSerializable {
    // Constants for clarity
    public const int EmptyItemId = -1;

    /// <summary>
    /// The ID of the item. -1 signifies an empty slot.
    /// </summary>
    [SerializeField] private int _itemId;
    public int ItemId {
        get => _itemId;
        private set => _itemId = value;
    }

    /// <summary>
    /// The amount of the item in the slot.
    /// </summary>
    [SerializeField] private int _amount;
    public int Amount {
        get => _amount;
        private set => _amount = value;
    }

    /// <summary>
    /// The ID representing the rarity of the item.
    /// </summary>
    [SerializeField] private int _rarityId;
    public int RarityId {
        get => _rarityId;
        private set => _rarityId = value;
    }

    /// <summary>
    /// Indicates whether the item slot is empty.
    /// </summary>
    public bool IsEmpty => ItemId == EmptyItemId || ItemId <= 0;

    /// <summary>
    /// Initializes a new empty instance of the <see cref="ItemSlot"/> class.
    /// </summary>
    public ItemSlot() {
        Clear();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemSlot"/> class with the specified item ID, amount, and rarity ID.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="amount">The amount of the item.</param>
    /// <param name="rarityId">The ID of the rarity.</param>
    public ItemSlot(int itemId, int amount, int rarityId) => InitializeSlot(itemId, amount, rarityId);

    /// <summary>
    /// Sets the current item slot with the values from the specified item slot.
    /// </summary>
    /// <param name="itemSlot">The item slot to set.</param>
    public void Set(ItemSlot itemSlot) => InitializeSlot(itemSlot);

    /// <summary>
    /// Initializes the item slot with the provided values from another item slot.
    /// </summary>
    /// <param name="itemSlot">The item slot to initialize from.</param>
    private void InitializeSlot(ItemSlot itemSlot) {
        _itemId = itemSlot._itemId;
        _amount = itemSlot._amount;
        _rarityId = itemSlot._rarityId;
    }

    /// <summary>
    /// Initializes the item slot with the specified item ID, amount, and rarity ID.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="amount">The amount of the item.</param>
    /// <param name="rarityId">The ID of the rarity.</param>
    private void InitializeSlot(int itemId, int amount, int rarityId) {
        _itemId = itemId;
        _amount = amount;
        _rarityId = rarityId;
    }

    /// <summary>
    /// Clears the item slot by resetting the item ID, amount, and rarity ID.
    /// </summary>
    public void Clear() {
        _itemId = EmptyItemId;
        _amount = 0;
        _rarityId = 0;
    }

    /// <summary>
    /// Adds a specified amount to the item slot.
    /// </summary>
    /// <param name="amountToAdd">The amount to add.</param>
    /// <returns>The actual amount added, which may be less if it exceeds the maximum stackable amount.</returns>
    public int AddAmount(int amountToAdd, int maxStackableAmount) {
        if (IsEmpty) {
            Debug.LogWarning("Kann keine Menge zu einem leeren ItemSlot hinzufügen. Setzen Sie zuerst das Item.");
            return 0;
        }

        int spaceLeft = maxStackableAmount - _amount;
        int actualAdded = Mathf.Min(amountToAdd, spaceLeft);
        _amount += actualAdded;
        return actualAdded;
    }

    /// <summary>
    /// Removes a specified amount from the item slot.
    /// </summary>
    /// <param name="amountToRemove">The amount to remove.</param>
    /// <returns>The actual amount removed, which may be less if the slot doesn't contain enough.</returns>
    public int RemoveAmount(int amountToRemove) {
        int actualRemoved = Mathf.Min(amountToRemove, _amount);
        _amount -= actualRemoved;

        if (_amount == 0) {
            Clear();
        }

        return actualRemoved;
    }

    /// <summary>
    /// Determines if this item slot can stack with another.
    /// </summary>
    /// <param name="other">The other item slot.</param>
    /// <param name="itemManager">Reference to the ItemManager to get item data.</param>
    /// <returns>True if they can stack; otherwise, false.</returns>
    public bool CanStackWith(ItemSlot other, ItemManager itemManager) {
        if (IsEmpty || other.IsEmpty) {
            return false;
        }
            

        if (_itemId != other._itemId || _rarityId != other._rarityId) {
            return false;
        }

        int maxStackableAmount = itemManager.GetMaxStackableAmount(_itemId);
        return _amount < maxStackableAmount;
    }

    /// <summary>
    /// Tauscht den Inhalt dieses ItemSlots mit einem anderen ItemSlot.
    /// </summary>
    /// <param name="other">Der andere ItemSlot, mit dem getauscht werden soll.</param>
    public void SwapWith(ItemSlot other) {
        (this._itemId, other._itemId) = (other._itemId, this._itemId);
        (this._amount, other._amount) = (other._amount, this._amount);
        (this._rarityId, other._rarityId) = (other._rarityId, this._rarityId);
    }


    /// <summary>
    /// Serialisiert die Daten des ItemSlots mit dem bereitgestellten Serializer.
    /// </summary>
    /// <typeparam name="T">Der Typ des Serializers.</typeparam>
    /// <param name="serializer">Der Serializer, der zum Serialisieren der Daten verwendet wird.</param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref _itemId);
        serializer.SerializeValue(ref _amount);
        serializer.SerializeValue(ref _rarityId);
    }
}
