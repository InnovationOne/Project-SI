using UnityEngine;

// Pick-up action: Picks up an object placed on the map.
[CreateAssetMenu(menuName = "Tool Action/Pick Up")]
public class PickUpSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int gridPosition, ItemSlot itemSlot) {
        if (PlaceableObjectsManager.Instance.TryGetNetworkIdAt(gridPosition, out var id)) {
            PlaceableObjectsManager.Instance.PickUpObjectServerRpc(id);
        }
    }
}
