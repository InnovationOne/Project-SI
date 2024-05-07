using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        return _targetTilemap.WorldToCell((Vector3)position);
    }

    /// <summary>
    /// Returns the <see cref="TileBase"/> at the specified grid position.
    /// </summary>
    /// <param name="gridposition">The grid position to retrieve the tile from.</param>
    /// <returns>The <see cref="TileBase"/> at the specified grid position.</returns>
    public TileBase ReturnTileBaseAtGridPosition(Vector3Int gridposition) {
        return _targetTilemap.GetTile(gridposition);
    }

    /// <summary>
    /// Represents a three-dimensional vector.
    /// </summary>
    public Vector3 AlignPositionToGridCenter(Vector3 position) {
        return position + new Vector3(0.5f, 0.5f);
    }
}
