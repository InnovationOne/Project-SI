using UnityEngine;

// This script handels seeding a plowed tile
[CreateAssetMenu(menuName = "Tool Action/Fertilizer")]
public class FertilizerCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.FertilizeTileServerRpc(new Vector3IntSerializable(position), itemSlot.ItemId);
    }
}
