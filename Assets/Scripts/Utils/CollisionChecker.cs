using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Utility class for performing collision checks on a Tilemap using a Grid.
/// Checks if any of the given grid cells contain a collider on one of the forbidden layers.
/// </summary>
public class CollisionChecker {
    // Combined forbidden collision layers bitmask.
    public LayerMask ForbiddenLayers { get; private set; }

    // Reference to the parent Grid, used for cell-to-world conversions.
    private readonly Grid _grid;

    /// <summary>
    /// Initializes a new instance of the CollisionChecker.
    /// </summary>
    /// <param name="grid">Parent Grid of the Tilemap.</param>
    /// <param name="forbiddenLayers">Combined forbidden layers mask.</param>
    public CollisionChecker(Grid grid, LayerMask forbiddenLayers) {
        _grid = grid;
        ForbiddenLayers = forbiddenLayers;
    }

    /// <summary>
    /// Checks if any of the specified cells contain a collider on the forbidden layers.
    /// The check is done at the center point of each cell.
    /// </summary>
    /// <param name="cells">List of grid cell positions to check.</param>
    /// <returns>True if any forbidden collider is found; otherwise, false.</returns>
    public bool CheckCollision(List<Vector3Int> cells) {
        foreach (Vector3Int cell in cells) {
            // Calculate the center of the cell.
            Vector3 worldCenter = _grid.CellToWorld(cell) + (_grid.cellSize / 2f);
            Collider2D hit = Physics2D.OverlapPoint(worldCenter, ForbiddenLayers);
            if (hit != null)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Calculates the list of grid cell positions occupied by an object,
    /// given the bottom-left origin and the size in grid cells.
    /// </summary>
    /// <param name="origin">Bottom-left cell position.</param>
    /// <param name="sizeInCells">Size in grid cells (width, height).</param>
    /// <returns>List of all grid cell positions occupied.</returns>
    public List<Vector3Int> CalculateOccupiedCells(Vector3Int origin, Vector2Int sizeInCells) {
        var cells = new List<Vector3Int>();
        for (int x = 0; x < sizeInCells.x; x++) {
            for (int y = 0; y < sizeInCells.y; y++) {
                cells.Add(new Vector3Int(origin.x + x, origin.y + y, origin.z));
            }
        }
        return cells;
    }
}