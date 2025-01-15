using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents an object that can be placed in the game world, synchronized over the network.
/// Acts as a base class for more specialized placeable objects.
/// Implements IObjectDataPersistence and IInteractable interfaces.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public abstract class PlaceableObject : NetworkBehaviour, IObjectDataPersistence, IInteractable {
    // Stores data about this placeable object, including its position and state.
    public PlaceableObjectData PlaceableObjectData;

    // This property is implemented by subclasses to define interaction distance.
    public virtual float MaxDistanceToPlayer => throw new NotImplementedException();

    // Called before object data loading. Used by subclasses to set up initial state if needed.
    public virtual void InitializePreLoad(int itemId) { }

    // Called after object data loading. Used by subclasses to finalize initialization.
    public virtual void InitializePostLoad() { }

    // Loads object state from the given data string.
    public virtual void LoadObject(FixedString4096Bytes data) { }

    // Saves the current state of the object as a serialized string.
    public virtual string SaveObject() { return string.Empty; }

    // Called when a player interacts with this object.
    public virtual void Interact(PlayerController player) { }

    // Called when a player picks up items stored within this placed object.
    public virtual void PickUpItemsInPlacedObject(PlayerController player) { }
}

/// <summary>
/// Network-synced data representing a placeable object's state.
/// Includes an ID, position, serialization state, and a reference to its prefab's NetworkObjectId.
/// </summary>
[Serializable]
public struct PlaceableObjectData : INetworkSerializable, IEquatable<PlaceableObjectData> {
    public int ObjectId;
    public int RotationIdx;
    public Vector3Int Position;
    public FixedString4096Bytes State;
    public ulong PrefabNetworkObjectId;

    // Initializes the data to a default invalid state.
    public PlaceableObjectData(bool initialize = true) {
        ObjectId = -1;
        RotationIdx = 0;
        Position = Vector3Int.zero;
        State = new FixedString4096Bytes();
        PrefabNetworkObjectId = 0;
    }

    // Serializes/Deserializes the object data over the network.
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref ObjectId);
        serializer.SerializeValue(ref RotationIdx);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref State);
        serializer.SerializeValue(ref PrefabNetworkObjectId);
    }

    // Checks equality with another PlaceableObjectData.
    public bool Equals(PlaceableObjectData other) =>
        ObjectId == other.ObjectId &&
        RotationIdx == other.RotationIdx &&
        Position.Equals(other.Position) &&
        State.Equals(other.State) &&
        PrefabNetworkObjectId == other.PrefabNetworkObjectId;

    public override bool Equals(object obj) => obj is PlaceableObjectData other && Equals(other);

    public override int GetHashCode() {
        var hash = new HashCode();
        hash.Add(ObjectId);
        hash.Add(RotationIdx);
        hash.Add(Position);
        hash.Add(State);
        hash.Add(PrefabNetworkObjectId);
        return hash.ToHashCode();
    }
}