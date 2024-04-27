using UnityEngine;

// This script handels placing onjects on the map
[CreateAssetMenu(menuName = "Tool Action/Place Object")]
public class PlaceObject : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        Debug.Log("PlaceObject.OnApplyToTileMap() not implemented");
        return;

        if (PlaceableObjectsManager.Instance.IsPositionPlaced(position) || CropsManager.Instance.IsPositionSeeded(position)) {
        }

        PlaceableObjectsManager.Instance.PlaceObjectOnMap(position);

        PlayerInventoryController.LocalInstance.InventoryContainer.RemoveItem(itemSlot.Item.ItemId, 1, itemSlot.RarityId);

        //return true;
    }
}
