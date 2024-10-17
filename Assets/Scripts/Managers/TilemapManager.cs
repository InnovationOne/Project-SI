using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapManager : NetworkBehaviour {
    public static TilemapManager Instance { get; private set; }

    private Tilemap _targetTilemap;


    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Tilemap Read Manager in the scene.");
        } else {
            Instance = this;
        }

        _targetTilemap = GetComponent<Tilemap>();
    }

    /// <summary>
    /// Converts a 2D position to a grid position in the tilemap.
    /// </summary>
    /// <param name="position">The 2D position to convert.</param>
    /// <returns>The grid position in the tilemap.</returns>
    public Vector3Int GetGridPosition(Vector2 position) {
        return _targetTilemap.WorldToCell(position);
    }

    /// <summary>
    /// Returns the TileBase at the specified grid position.
    /// </summary>
    /// <param name="gridPosition">The grid position to retrieve the tile from.</param>
    /// <returns>The TileBase at the specified grid position.</returns>
    public TileBase GetTileAtGridPosition(Vector3Int gridPosition) {
        return _targetTilemap.GetTile(gridPosition);
    }

    /// <summary>
    /// Aligns a world position to the center of the nearest grid cell.
    /// </summary>
    /// <param name="position">The world position to align.</param>
    /// <returns>The aligned world position.</returns>
    public Vector3 AlignPositionToGridCenter(Vector3 position) {
        Vector3Int cell = _targetTilemap.WorldToCell(position);
        return _targetTilemap.GetCellCenterWorld(cell);
    }
}
