using UnityEngine;

// Seed action: Plants a seed on a plowed tile.
[CreateAssetMenu(menuName = "Tool Action/Seed")]
public class SeedCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.SeedTileServerRpc(
            new Vector3IntSerializable(position),
            itemSlot.ItemId
        );
    }
}
