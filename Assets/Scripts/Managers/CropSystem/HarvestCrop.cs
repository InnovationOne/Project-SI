using UnityEngine;

public class HarvestCrop : Interactable {
    private Vector3Int _cropPosition;

    public override void Interact(Player player) {
        CropsManager.Instance.HarvestCropServerRpc(_cropPosition);
    }

    public void SetCropPosition(Vector3Int position) {
        _cropPosition = position;
    }
}
