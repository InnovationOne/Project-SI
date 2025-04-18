using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Serializable container for a placed object's metadata and state.
/// Used internally in PlaceableObjectsManager.
/// </summary>
[Serializable]
public struct PlaceableObjectData {
    public int ObjectId;
    public int RotationIdx;
    public Vector3Int Position;
    public string State;
}

/// <summary>
/// Abstract base class for placeable objects.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public abstract class PlaceableObject : NetworkBehaviour, IObjectDataPersistence, IInteractable {
    /// <summary>
    /// The maximum distance at which the player can interact with the object.
    /// </summary>
    public abstract float MaxDistanceToPlayer { get; }

    /// <summary>
    /// Indicates whether the interaction area is circular.
    /// </summary>
    public abstract bool CircleInteract { get; }

    /// <summary>
    /// Called before loading object data to perform preliminary initialization.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    public abstract void InitializePreLoad(int itemId);

    /// <summary>
    /// Called after loading object data to finalize initialization.
    /// </summary>
    public abstract void InitializePostLoad();

    /// <summary>
    /// Called when a player interacts with the object.
    /// </summary>
    public abstract void Interact(PlayerController player);

    /// <summary>
    /// Called when a player picks up items stored in the object.
    /// </summary>
    public abstract void PickUpItemsInPlacedObject(PlayerController player);

    public abstract void OnStateReceivedCallback(string callbackName);

    /// <summary>
    /// Saves the current state of the object to a serialized string.
    /// </summary>
    public abstract string SaveObject();

    /// <summary>
    /// Loads the object's state from the given serialized string.
    /// </summary>
    public abstract void LoadObject(string data);

    /// <summary>
    /// Updates the object's state with the new data. By default, simply calls LoadObject.
    /// </summary>
    /// <param name="newState">The new state as a serialized string.</param>
    public virtual void UpdateState(string newState) {
        LoadObject(newState);
    }
}