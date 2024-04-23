using UnityEngine;

// This script handels plowing a tile on the grid
[CreateAssetMenu(menuName = "Tool Action/Plow")]
public class PlowCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        PlayerMarkerController.LocalInstance.TriggerAreaMarker(itemSlot.RarityID - 1, 
            (itemSlot.Item as ToolSO).UsageOrDamageOnAction.ToArray(), 
            (itemSlot.Item as ToolSO).EnergyOnAction[itemSlot.RarityID - 1], 
            ToolTypes.Hoe);
    }
}
