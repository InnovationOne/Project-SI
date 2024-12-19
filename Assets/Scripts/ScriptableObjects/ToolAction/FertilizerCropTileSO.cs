using UnityEngine;

// Fertilizer action: Fertilizes a tile to enhance crop yield.
[CreateAssetMenu(menuName = "Tool Action/Fertilizer")]
public class FertilizerCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.FertilizeTileServerRpc(
                    new Vector3IntSerializable(position),
                    itemSlot.ItemId
        );
    }
}
