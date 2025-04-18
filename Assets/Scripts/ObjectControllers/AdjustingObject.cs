using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class AdjustingObject : PlaceableObject {
    private static Tilemap _sharedTilemap;
    protected Tilemap SharedTilemap {
        get {
            if (_sharedTilemap == null) {
                var go = GameObject.FindWithTag("FenceGateTilemap");
                if (go == null) Debug.LogError("FenceGateTilemap nicht gefunden! Bitte Tag setzen.");
                else _sharedTilemap = go.GetComponent<Tilemap>();
            }
            return _sharedTilemap;
        }
    }

    protected Grid Grid { get; private set; }
    protected Vector3Int CellPosition { get; private set; }
    protected int _itemId;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    private void Awake() {
        Grid = SharedTilemap.GetComponentInParent<Grid>();
    }

    public override void InitializePreLoad(int itemId) {
        _itemId = itemId;
        CellPosition = Grid.WorldToCell(transform.position);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        // Stelle sicher, dass CellPosition aktuell ist
        CellPosition = Grid.WorldToCell(transform.position);

        if (IsServer) {
            // Server-seitig Tile setzen und an Clients broadcasten
            SetTileInternal(_itemId, CellPosition);
            SetTileClientRpc(_itemId, CellPosition.x, CellPosition.y, CellPosition.z);
        }
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) {
        if (IsServer) {
            // Server entfernt Tile und informiert Clients
            RemoveTileInternal(CellPosition);
            RemoveTileClientRpc(CellPosition.x, CellPosition.y, CellPosition.z);
        }
    }

    [ClientRpc]
    private void SetTileClientRpc(int itemId, int x, int y, int z) {
        SetTileInternal(itemId, new Vector3Int(x, y, z));
    }

    [ClientRpc]
    private void RemoveTileClientRpc(int x, int y, int z) {
        RemoveTileInternal(new Vector3Int(x, y, z));
    }

    protected abstract void SetTileInternal(int itemId, Vector3Int cell);

    protected virtual void RemoveTileInternal(Vector3Int cell) {
        SharedTilemap.SetTile(cell, null);
        SharedTilemap.RefreshTile(cell);
        RefreshNeighbors(cell);
    }

    protected void RefreshNeighbors(Vector3Int cell) {
        var deltas = new Vector3Int[]
        {
            new( 0,  1, 0), new( 1,  1, 0),
            new( 1,  0, 0), new( 1, -1, 0),
            new( 0, -1, 0), new(-1, -1, 0),
            new(-1,  0, 0), new(-1,  1, 0),
        };
        foreach (var d in deltas) {
            SharedTilemap.RefreshTile(cell + d);
        }
    }
}
