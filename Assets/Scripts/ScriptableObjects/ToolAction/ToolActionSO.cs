using UnityEngine;

public abstract class ToolActionSO : ScriptableObject {
    public virtual void OnApplyToTileMap(Vector3Int position, ItemSlot itemSlot) {
        throw new System.NotImplementedException();
    }
}
