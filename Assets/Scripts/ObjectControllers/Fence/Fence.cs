using UnityEngine;

public class Fence : AdjustingObject {
    private FenceSO _fenceSO;

    public override void Interact(PlayerController player) {
        // TODO: Klettern o. Ä. implementieren
    }

    public override void InitializePostLoad() { }
    public override void LoadObject(string data) { }
    public override string SaveObject() => string.Empty;
    public override void OnStateReceivedCallback(string callbackName) { }

    protected override void SetTileInternal(int itemId, Vector3Int cell) {
        if (_fenceSO == null || _itemId != itemId)
            _fenceSO = GameManager.Instance.ItemManager.ItemDatabase[itemId] as FenceSO;

        if (_fenceSO == null) {
            Debug.LogError($"Item {itemId} ist kein FenceSO");
            return;
        }

        var tile = _fenceSO.FenceRuleTile;
        SharedTilemap.SetTile(cell, tile);
        SharedTilemap.RefreshTile(cell);
        RefreshNeighbors(cell);
    }
}
