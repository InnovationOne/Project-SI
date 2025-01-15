/// <summary>
/// Interface for interactable objects within the game.
/// Classes implementing this interface can be interacted with by players.
/// </summary>
public interface IInteractable {
    /// <summary>
    /// Gets the maximum distance at which a player can interact with the object.
    /// </summary>
    float MaxDistanceToPlayer { get; }

    /// <summary>
    /// Executes the interaction logic when a player interacts with the object.
    /// </summary>
    /// <param name="player">The player that is interacting with the object.</param>
    void Interact(PlayerController player);

    /// <summary>
    /// Handles the logic for picking up items from the object when interacted with.
    /// </summary>
    /// <param name="player">The player attempting to pick up items.</param>
    void PickUpItemsInPlacedObject(PlayerController player);

    /// <summary>
    /// Initializes the interactable object before the game loads.
    /// </summary>
    /// <param name="itemId">The identifier for the item to initialize.</param>
    void InitializePreLoad(int itemId);
}
