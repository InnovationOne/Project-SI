using UnityEngine;

// Scythe action: Destroys a crop tile and applies energy cost.
[CreateAssetMenu(menuName = "Tool Action/Scythe")]
public class ScytheCropSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        int rarityIndex = itemSlot.RarityId - 1;
        if (GameManager.Instance.ItemManager.ItemDatabase[itemSlot.ItemId] is ToolSO tool) {
            int energyCost = tool.EnergyOnAction[rarityIndex];
            GameManager.Instance.CropsManager.DestroyCropTileServerRpc(
                new Vector3IntSerializable(position),
                energyCost,
                ToolSO.ToolTypes.Scythe
            );
        } else {
            Debug.LogWarning("Attempted to scythe with an invalid tool configuration.");
        }
    }
}
