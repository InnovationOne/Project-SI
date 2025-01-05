using UnityEngine;

// Seed action: Plants a seed on a plowed tile.
[CreateAssetMenu(menuName = "Tool Action/Seed")]
public class SeedCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        GameManager.Instance.CropsManager.SeedTileServerRpc(
            new Vector3IntSerializable(position),
            itemSlot.ItemId
        );
    }
}
