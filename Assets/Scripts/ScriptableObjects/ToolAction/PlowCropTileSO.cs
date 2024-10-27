using UnityEngine;

// This script handels plowing a tile on the grid
[CreateAssetMenu(menuName = "Tool Action/Plow")]
public class PlowCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        PlayerMarkerController.LocalInstance.TriggerAreaMarker(itemSlot.RarityId - 1, 
            (ItemManager.Instance.ItemDatabase[itemSlot.ItemId] as AreaToolSO).Area, 
            (ItemManager.Instance.ItemDatabase[itemSlot.ItemId] as AreaToolSO).EnergyOnAction[itemSlot.RarityId - 1], 
            ToolSO.ToolTypes.Hoe);
    }
}
