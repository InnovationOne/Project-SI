using UnityEngine;

[CreateAssetMenu(menuName = "Tool Action/Destroy Tile")]
public class DestroyCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.DestroyCropTileServerRpc(new Vector3IntSerializable(position), (ItemManager.Instance.ItemDatabase[itemSlot.ItemId] as ToolSO).EnergyOnAction[itemSlot.RarityId - 1], ToolSO.ToolTypes.Pickaxe);
    }
}
