using UnityEngine;

/// <summary>
/// Base class for interactable objects within the game.
/// </summary>
public class Interactable : MonoBehaviour {
    /// <summary>
    /// Maximum distance at which a player can interact with the object.
    /// </summary>
    public virtual float MaxDistanceToPlayer { get; protected set; }

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
}
