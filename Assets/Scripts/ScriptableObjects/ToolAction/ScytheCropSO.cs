using UnityEngine;

// This class is for clearing crop with a sense when it is dead
[CreateAssetMenu(menuName = "Tool Action/Scythe")]
public class ScytheCropSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.DestroyCropTileServerRpc(new Vector3IntSerializable(position), (ItemManager.Instance.ItemDatabase[itemSlot.ItemId] as ToolSO).EnergyOnAction[itemSlot.RarityId - 1], ToolSO.ToolTypes.Scythe);
    }
}
