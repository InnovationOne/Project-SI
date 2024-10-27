using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public abstract class PlaceableObject : NetworkBehaviour, IObjectDataPersistence, IInteractable {
    public PlaceableObjectData PlaceableObjectData;

    public virtual float MaxDistanceToPlayer => throw new NotImplementedException();

    public virtual void InitializePreLoad(int itemId) { }

    public virtual void InitializePostLoad() { }

    public virtual void LoadObject(FixedString4096Bytes data) { }

    public virtual string SaveObject() { return string.Empty; }

    public virtual void Interact(Player player) { }

    public virtual void PickUpItemsInPlacedObject(Player player) { }
}

[Serializable]
public struct PlaceableObjectData : INetworkSerializable, IEquatable<PlaceableObjectData> {
    public int ObjectId;
    public Vector3Int Position;
    public FixedString4096Bytes State;
    public ulong PrefabNetworkObjectId;

    /// <summary>
    /// Initializes the PlaceableObjectData to its default state.
    /// </summary>
    public PlaceableObjectData(bool initialize = true) {
        ObjectId = -1;
        Position = Vector3Int.zero;
        State = new FixedString4096Bytes();
        PrefabNetworkObjectId = 0;
    }

    /// <summary>
    /// Serializes the PlaceableObjectData for network transmission.
    /// </summary>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref ObjectId);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref State);
        serializer.SerializeValue(ref PrefabNetworkObjectId);
    }

    /// <summary>
    /// Serializes the PlaceableObjectData for network transmission.
    /// </summary>
    public bool Equals(PlaceableObjectData other) {
        return ObjectId == other.ObjectId &&
               Position.Equals(other.Position) &&
               State.Equals(other.State) &&
               PrefabNetworkObjectId == other.PrefabNetworkObjectId;
    }

    public override bool Equals(object obj) {
        return obj is PlaceableObjectData other && Equals(other);
    }

    public override int GetHashCode() {
        var hash = new HashCode();
        hash.Add(ObjectId);
        hash.Add(Position);
        hash.Add(State);
        hash.Add(PrefabNetworkObjectId);
        return hash.ToHashCode();
    }
}