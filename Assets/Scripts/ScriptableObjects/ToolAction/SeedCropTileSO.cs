using UnityEngine;

// This script handels seeding a plowed tile
[CreateAssetMenu(menuName = "Tool Action/Seed")]
public class SeedCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.SeedTileServerRpc(position, itemSlot.ItemId);
    }
}
