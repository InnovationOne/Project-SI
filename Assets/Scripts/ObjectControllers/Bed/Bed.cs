using System;
using UnityEngine;

public class Bed : MonoBehaviour, IInteractable {
    private ObjectVisual _visual;
    private int _itemId;

    [NonSerialized] private float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

    /// <summary>
    /// Initializes the bed object with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    public void InitializePreLoad(int itemId) {
        _itemId = itemId;
        _visual = GetComponentInChildren<ObjectVisual>();
        _visual.SetSprite(GetObjectSO().InactiveSprite);
    }

    /// <summary>
    /// Interacts with the bed object.
    /// </summary>
    /// <param name="player">The player object.</param>
    public void Interact(Player player) {
        player.SetPlayerInBed(!player.InBed);
        PlayerMovementController.LocalInstance.SetCanMoveAndTurn(player.InBed);
    }

    public void PickUpItemsInPlacedObject(Player player) { }

    /// <summary>
    /// Represents a ScriptableObject for objects in the game.
    /// </summary>
    private ObjectSO GetObjectSO() => ItemManager.Instance.ItemDatabase[_itemId] as ObjectSO;
}
