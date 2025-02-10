using System;
using Unity.Netcode;
using UnityEngine;

public class CollectablePrefab : NetworkBehaviour, IInteractable {
    [NonSerialized] float _maxDistanceToPlayer;
    public float MaxDistanceToPlayer => _maxDistanceToPlayer;
    [SerializeField] private ItemSO _itemSO;

    public void Interact(PlayerController player) {
        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            new ItemSlot(_itemSO.ItemId, 1, 0), 
            transform.position, 
            Vector2.zero, 
            spreadType: ItemSpawnManager.SpreadType.Circle);
    }
    public void InitializePreLoad(int itemId) { }

    public void PickUpItemsInPlacedObject(PlayerController player) { }
}
