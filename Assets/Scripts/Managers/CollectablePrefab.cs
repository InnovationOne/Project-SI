using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider2D))]
public class CollectablePrefab : NetworkBehaviour, IInteractable {
    [NonSerialized] float _maxDistanceToPlayer = 1.5f;
    public float MaxDistanceToPlayer => _maxDistanceToPlayer;
    [SerializeField] private ItemSO _itemSO;

    public void Interact(PlayerController player) {
        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            new ItemSlot(_itemSO.ItemId, 1, 0), 
            transform.position, 
            Vector2.zero, 
            spreadType: ItemSpawnManager.SpreadType.Circle);
        NetworkObject.Despawn(true);
        Destroy(gameObject);
    }
    public void InitializePreLoad(int itemId) { }

    public void PickUpItemsInPlacedObject(PlayerController player) { }
}
