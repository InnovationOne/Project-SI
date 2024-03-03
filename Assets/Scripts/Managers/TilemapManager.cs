using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;


public class TilemapManager : NetworkBehaviour {
    public static TilemapManager Instance { get; private set; }

    private Tilemap _targetTilemap;


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Tilemap Read Manager in the scene.");
        } else {
            Instance = this;
        }

        _targetTilemap = GetComponent<Tilemap>();
    }

    //### OLD CODE ###

    // Returns the grid position for the given position in world coordinates
    // If mousePosition is set to true, the position is assumed to be in screen coordinates
    // and is converted to world coordinates before being converted to a grid position
    public Vector3Int GetGridPosition(Vector2 position, bool mousePosition = false) {
        // Convert the position to world coordinates if it is in screen coordinates
        Vector3 worldposition;
        if (mousePosition)
            worldposition = Camera.main.ScreenToWorldPoint(position);
        else
            worldposition = position;

        // Convert the world position to a grid position and return it
        Vector3Int gridposition = _targetTilemap.WorldToCell(worldposition);
        return gridposition;
    }

    public TileBase ReturnTileBaseAtGridPosition(Vector3Int gridposition) {
        return _targetTilemap.GetTile(gridposition);
    }

    // Adjust the position of an object to align with the center of a grid cell
    public Vector3 FixPositionOnGrid(Vector3 cellToWorld) {
        // Add 0.5 to both the x and y coordinates of the vector
        return cellToWorld + new Vector3(0.5f, 0.5f);
    }


}
