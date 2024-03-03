using System.Threading.Tasks;
using UnityEngine;

// This script handels picking up placed items from the map oder destroying crop on the grid
[CreateAssetMenu(menuName = "Tool Action/Pick Up")]
public class PickUp : ToolActionSO {
    public override void OnApplyToTileMap(Vector3Int gridPosition, ItemSlot itemSlot) {
        if (PlaceableObjectsManager.Instance.IsPositionPlaced(gridPosition)) {
            PlaceableObjectsManager.Instance.PickUpObject(gridPosition);
        }
    }
}
