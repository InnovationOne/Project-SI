using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class HarvestCrop : NetworkBehaviour, IInteractable {
    [NonSerialized] float _maxDistanceToPlayer;
    public float MaxDistanceToPlayer => _maxDistanceToPlayer;
    Vector3Int _cropPosition;

    public void Interact(PlayerController player) {
        GameManager.Instance.CropsManager.HarvestCropServerRpc(new Vector3IntSerializable(_cropPosition));
    }

    public void SetPosition(Vector3Int position) {
        _cropPosition = position;
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }

    public void InitializePreLoad(int itemId) { }

}
