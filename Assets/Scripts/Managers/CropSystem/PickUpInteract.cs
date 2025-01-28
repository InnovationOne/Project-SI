using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PickUpInteract : NetworkBehaviour, IInteractable {
    [NonSerialized] float _maxDistanceToPlayer;
    public float MaxDistanceToPlayer => _maxDistanceToPlayer;
    Vector3Int _cropPosition;

    public void Interact(PlayerController player) {
        GameManager.Instance.CropsManager.HarvestCropServerRpc(new Vector3IntSerializable(_cropPosition));        
        // TODO Pick Up other item e.g. mushroom
    }

    public void SetPosition(Vector3Int position, bool isCrop = false) {
        _cropPosition = position;
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }

    public void InitializePreLoad(int itemId) { }

}
