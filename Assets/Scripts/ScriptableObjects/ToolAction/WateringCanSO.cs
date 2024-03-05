using UnityEngine;

// This script is for waterning a crop tile
[CreateAssetMenu(menuName = "Tool Action/Watering Can")]
public class WateringCanSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        PlayerMarkerController.LocalInstance.TriggerAreaMarker(itemSlot.RarityID - 1, itemSlot.Item.UsageOrDamageOnAction.ToArray(), itemSlot.Item.EnergyOnAction[itemSlot.RarityID - 1], ToolTypes.WateringCan);
    }
}
