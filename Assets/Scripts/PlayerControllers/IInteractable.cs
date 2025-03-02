// Interface for interactable objects within the game.
// Classes implementing this interface can be interacted with by players.
using UnityEngine;

public interface IInteractable {
    // Gets the maximum distance at which a player can interact with the object.
    [Tooltip("The maximum distance at which a player can interact with the object.")]
    float MaxDistanceToPlayer { get; }

    // Gets whether the object is interactable in a circle around the object or a cone in front of the player.
    [Tooltip("Whether the object is interactable in a circle around the object or a cone in front of the player.")]
    bool CircleInteract { get; }

    // Executes the interaction logic when a player interacts with the object.
    void Interact(PlayerController player);

    // Handles the logic for picking up items from the object when interacted with.
    void PickUpItemsInPlacedObject(PlayerController player);

    // Initializes the interactable object before the game loads.
    void InitializePreLoad(int itemId);
}
