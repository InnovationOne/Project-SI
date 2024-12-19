using UnityEngine;

// This script handels placing onjects on the map
[CreateAssetMenu(menuName = "Tool Action/Place Object")]
public class PlaceObjectSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        PlaceableObjectsManager.Instance.PlaceObjectOnMapServerRpc(new Vector3IntSerializable(position), 
                                                                   PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId,
                                                                   PlayerController.LocalInstance.PlayerMarkerController.HighlightId);
    }
}
