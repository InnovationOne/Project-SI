using System;
using Unity.Netcode;

[Serializable]
public class ItemSlot : INetworkSerializable {
    public int ItemId;
    public int Amount;
    public int RarityId;

    /// <summary>
    /// Represents a slot for holding an item with amount and a rarity.
    /// </summary>
    public ItemSlot() { }

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
    /// Initializes the item slot with the provided values.
    /// </summary>
    /// <param name="itemSlot">The item slot to initialize from.</param>
    private void InitializeSlot(ItemSlot itemSlot) {
        ItemId = itemSlot.ItemId;
        Amount = itemSlot.Amount;
        RarityId = itemSlot.RarityId;
    }

    /// <summary>
    /// Initializes the item slot with the specified item ID, amount, and rarity ID.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="amount">The amount of the item.</param>
    /// <param name="rarityId">The ID of the rarity.</param>
    private void InitializeSlot(int itemId, int amount, int rarityId) {
        ItemId = itemId;
        Amount = amount;
        RarityId = rarityId;
    }

    /// <summary>
    /// Clears the item slot by resetting the item ID, amount, and rarity ID.
    /// </summary>
    public void Clear() {
        ItemId = -1;
        Amount = 0;
        RarityId = 0;
    }

    /// <summary>
    /// Serializes the item slot data using the provided serializer.
    /// </summary>
    /// <typeparam name="T">The type of the serializer.</typeparam>
    /// <param name="serializer">The serializer used to serialize the data.</param>
    /// <remarks>
    /// This method serializes the item slot data, including the item ID, amount, and rarity ID,
    /// using the provided serializer.
    /// </remarks>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref ItemId);
        serializer.SerializeValue(ref Amount);
        serializer.SerializeValue(ref RarityId);
    }
}
