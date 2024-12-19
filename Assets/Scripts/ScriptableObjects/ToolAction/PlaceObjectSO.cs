using UnityEngine;

// Place object action: Places a selected object from the player's toolbelt onto the map.
[CreateAssetMenu(menuName = "Tool Action/Place Object")]
public class PlaceObjectSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        var toolbeltItemId = PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId;
        var highlightId = PlayerController.LocalInstance.PlayerMarkerController.HighlightId;

        PlaceableObjectsManager.Instance.PlaceObjectOnMapServerRpc(
            new Vector3IntSerializable(position),
            toolbeltItemId,
            highlightId
        );
    }
}
