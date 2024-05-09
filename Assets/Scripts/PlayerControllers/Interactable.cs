using System;
using UnityEngine;

/// <summary>
/// Base class for interactable objects within the game.
/// </summary>
public abstract class Interactable : MonoBehaviour {
    /// <summary>
    /// Maximum distance at which a player can interact with the object.
    /// </summary>
    [NonSerialized] private float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

    /// <summary>
    /// Defines interaction logic for the interactable object.
    /// </summary>
    /// <param name="player">The player that is interacting with the object.</param>
    public virtual void Interact(Player player) { }

    /// <summary>
    /// Logic to pick up items from the object when it is interacted with.
    /// </summary>
    /// <param name="player">The player picking up items.</param>
    public virtual void PickUpItemsInPlacedObject(Player player) { }

    public virtual void Initialize(int itemId) { 

    }
}
