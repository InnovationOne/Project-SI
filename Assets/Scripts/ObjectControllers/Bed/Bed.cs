using UnityEngine;

public class Bed : Interactable {
    [SerializeField] private ObjectVisual _visual;

    private int _itemId;

    /// <summary>
    /// Initializes the bed object with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    public override void Initialize(int itemId) {
        _itemId = itemId;
        _visual.SetSprite(GetObjectSO().InactiveSprite);
    }

    /// <summary>
    /// Interacts with the bed object.
    /// </summary>
    /// <param name="player">The player object.</param>
    public override void Interact(Player player) {
        player.SetPlayerInBed(!player.InBed);
        PlayerMovementController.LocalInstance.SetCanMoveAndTurn(player.InBed);
    }

    /// <summary>
    /// Represents a ScriptableObject for objects in the game.
    /// </summary>
    private ObjectSO GetObjectSO() => ItemManager.Instance.ItemDatabase[_itemId] as ObjectSO;
}
