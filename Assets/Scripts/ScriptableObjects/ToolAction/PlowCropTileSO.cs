using UnityEngine;

// Plow action: Applies a "hoe" action to an area defined by the equipped AreaToolSO.
[CreateAssetMenu(menuName = "Tool Action/Plow")]
public class PlowCropTileSO : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        int rarityIndex = itemSlot.RarityId - 1;
        if (ItemManager.Instance.ItemDatabase[itemSlot.ItemId] is AreaToolSO areaTool) {
            int energyCost = areaTool.EnergyOnAction[rarityIndex];
            Area[] area = areaTool.Area;

            PlayerController.LocalInstance.PlayerMarkerController.TriggerAreaMarker(
                rarityIndex,
                area,
                energyCost,
                ToolSO.ToolTypes.Hoe
            );
        } else {
            Debug.LogWarning("Attempted to plow with a non-area tool.");
        }
    }
}
