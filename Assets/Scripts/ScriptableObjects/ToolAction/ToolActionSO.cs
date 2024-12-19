using UnityEngine;

// Base class for all tool actions on a tile, designed for extension.
public class ToolActionSO : ScriptableObject {
    // Called when applying a tool to a tile on the map.
    public virtual void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        throw new System.NotImplementedException();
    }
}
