using Unity.Netcode;
using UnityEngine;

public class Gate : AdjustingObject {
    private GateSO _gateSO;
    private readonly NetworkVariable<bool> _isOpened = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void InitializePreLoad(int itemId) {
        base.InitializePreLoad(itemId);
        _gateSO = GameManager.Instance.ItemManager.ItemDatabase[itemId] as GateSO;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _isOpened.OnValueChanged += (_, _) => SetTileInternal(_itemId, CellPosition);
    }

    public override void Interact(PlayerController player) {
        if (IsServer) _isOpened.Value = !_isOpened.Value;
        else ToggleGateServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleGateServerRpc(ServerRpcParams rpcParams = default) {
        _isOpened.Value = !_isOpened.Value;
    }

    protected override void SetTileInternal(int itemId, Vector3Int cell) {
        if (_gateSO == null || _itemId != itemId)
            _gateSO = GameManager.Instance.ItemManager.ItemDatabase[itemId] as GateSO;

        if (_gateSO == null) {
            Debug.LogError($"Item {itemId} ist kein GateSO");
            return;
        }

        var tile = _isOpened.Value
            ? _gateSO.OpenGateRuleTile
            : _gateSO.ClosedGateRuleTile;

        SharedTilemap.SetTile(cell, tile);
        SharedTilemap.RefreshTile(cell);
        RefreshNeighbors(cell);
    }

    public override void LoadObject(string data) {
        if (bool.TryParse(data, out bool opened)) _isOpened.Value = opened;
    }

    public override string SaveObject() => _isOpened.Value.ToString();
    public override void InitializePostLoad() { }
    public override void OnStateReceivedCallback(string callbackName) { }
}
