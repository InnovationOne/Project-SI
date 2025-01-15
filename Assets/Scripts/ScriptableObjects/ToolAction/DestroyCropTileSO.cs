using UnityEngine;

// Destroy action: Uses a pickaxe-like tool to remove a crop tile.
[CreateAssetMenu(menuName = "Tool Action/Destroy Tile")]
public class DestroyCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        int rarityIndex = itemSlot.RarityId - 1;
        if (GameManager.Instance.ItemManager.ItemDatabase[itemSlot.ItemId] is ToolSO tool) {
            int energyCost = tool.EnergyOnAction[rarityIndex];
            GameManager.Instance.CropsManager.DestroyCropTileServerRpc(
                new Vector3IntSerializable(position),
                energyCost,
                ToolSO.ToolTypes.Pickaxe
            );
        } else {
            Debug.LogWarning("Attempted to destroy a tile with an invalid tool configuration.");
        }
    }
}
