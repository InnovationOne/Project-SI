using UnityEngine;

// This script handels placing onjects on the map
[CreateAssetMenu(menuName = "Tool Action/Place Object")]
public class PlaceObject : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        Debug.Log("PlaceObject.OnApplyToTileMap() not implemented");
        return;

        if (PlaceableObjectsManager.Instance.POContainer.PlaceableObjects.ContainsKey(position) || CropsManager.Instance.CropTileContainer.IsPositionSeeded(position)) {
        }

        PlaceableObjectsManager.Instance.PlaceObjectOnMapServerRpc(
            PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId, 
            position);

        PlayerInventoryController.LocalInstance.InventoryContainer.RemoveItem(itemSlot);

        //return true;
    }
}
