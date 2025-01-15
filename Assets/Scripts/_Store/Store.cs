using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Store : NetworkBehaviour, IInteractable {
    [SerializeField] protected ItemContainerSO _storeContainer;
    [SerializeField] protected StoreVisual _storeVisual;

    protected float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

    public virtual void InitializePreLoad(int itemId) { }

    
    public virtual void OnLeftClick(ItemSO itemSO) {
        // Left click logic
    }

    public virtual void Interact(PlayerController player) {
        // Call the store UI
    }

    public virtual void PickUpItemsInPlacedObject(PlayerController player) { }
}