using UnityEngine;

[CreateAssetMenu(menuName = "Tool Action/Destroy Tile")]
public class DestroyCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.DestroyCropTileServerRpc(position, (itemSlot.Item as ToolSO).EnergyOnAction[itemSlot.RarityId - 1], ToolSO.ToolTypes.Pickaxe);
    }
}
