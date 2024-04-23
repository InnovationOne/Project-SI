using UnityEngine;

// This class is for clearing crop with a sense when it is dead
[CreateAssetMenu(menuName = "Tool Action/Scythe")]
public class ScytheCropSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        CropsManager.Instance.DestroyCropTileServerRpc(position, (itemSlot.Item as ToolSO).EnergyOnAction[itemSlot.RarityID - 1], ToolTypes.Scythe);
    }
}
