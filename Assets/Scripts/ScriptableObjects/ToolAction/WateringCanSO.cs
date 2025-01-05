using UnityEngine;

// Watering can action: Waters an area of tiles to promote growth.
[CreateAssetMenu(menuName = "Tool Action/Watering Can")]
public class WateringCanSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        int rarityIndex = itemSlot.RarityId - 1;
        if (GameManager.Instance.ItemManager.ItemDatabase[itemSlot.ItemId] is AreaToolSO areaTool) {
            int energyCost = areaTool.EnergyOnAction[rarityIndex];
            Area[] area = areaTool.Area;

            PlayerController.LocalInstance.PlayerMarkerController.TriggerAreaMarker(
                rarityIndex,
                area,
                energyCost,
                ToolSO.ToolTypes.WateringCan
            );
        } else {
            Debug.LogWarning("Attempted to water with a non-area tool.");
        }
    }
}
