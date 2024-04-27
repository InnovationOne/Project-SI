using UnityEngine;

// This script is for waterning a crop tile
[CreateAssetMenu(menuName = "Tool Action/Watering Can")]
public class WateringCanSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        PlayerMarkerController.LocalInstance.TriggerAreaMarker(itemSlot.RarityId - 1, 
            (itemSlot.Item as ToolSO).UsageOrDamageOnAction.ToArray(), 
            (itemSlot.Item as ToolSO).EnergyOnAction[itemSlot.RarityId - 1],
            ToolSO.ToolTypes.WateringCan);
    }
}
