using System;
using UnityEngine;

public class Bed : MonoBehaviour, IInteractable {
    private SpriteRenderer _visual;
    private int _itemId;

    [NonSerialized] private float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

    /// <summary>
    /// Initializes the bed object with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    public void InitializePreLoad(int itemId) {
        _itemId = itemId;
        _visual = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Interacts with the bed object.
    /// </summary>
    /// <param name="player">The player object.</param>
    public void Interact(PlayerController player) {
        var pMC = player.PlayerMovementController;
        bool inBed = pMC.ActivePlayerState == PlayerMovementController.PlayerState.Sleeping;
        player.TogglePlayerInBed();
        PlayerController.LocalInstance.PlayerMovementController.SetCanMoveAndTurn(inBed);
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }

    /// <summary>
    /// Represents a ScriptableObject for objects in the game.
    /// </summary>
    private ObjectSO GetObjectSO() => GameManager.Instance.ItemManager.ItemDatabase[_itemId] as ObjectSO;
}
