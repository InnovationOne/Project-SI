using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Represents the grid used for pathfinding.
/// </summary>
public class PathfindingGrid : MonoBehaviour {
    [SerializeField] private bool _displayGridGizmos;
    [SerializeField] private LayerMask _unwalkableMask;
    [SerializeField] private Vector2 _gridWorldSize;
    [SerializeField] private float _nodeRadius;
    public int MaxSize => _gridSizeX * _gridSizeY;

    private Node[,] _grid;
    private float _nodeDiameter;
    private int _gridSizeX, _gridSizeY;

    [Header("Tilemap tile settings")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TerrainType[] _walkableRegions;
    [SerializeField] private int _obstacleProximityPenalty = 10;
    private Dictionary<TileBase, int> _walkableRegionsDictionary = new Dictionary<TileBase, int>();
    private int _penaltyMin = int.MaxValue;
    private int _penaltyMax = int.MinValue;

    /// <summary>
    /// Initializes the PathfindingGrid component and creates the grid.
    /// </summary>
    private void Awake() {
        // Calculate the diameter of a node
        _nodeDiameter = _nodeRadius * 2;

        // Calculate the size of the grid
        _gridSizeX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
        _gridSizeY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);

        foreach (TerrainType region in _walkableRegions) {
            foreach (TileBase tile in region.Tiles) {
                _walkableRegionsDictionary.Add(tile, region.TerrainPenalty);
            }
        }

        // Create the grid
        CreateGrid();
    }

    /// <summary>
    /// Creates the grid and initializes nodes.
    /// </summary>
    private void CreateGrid() {
        // Create a 2D array of nodes on the grid
        _grid = new Node[_gridSizeX, _gridSizeY];

        // Get bottom left corner of the grid
        Vector3 worldBottomLeft = transform.position - Vector3.right * _gridWorldSize.x / 2 - Vector3.up * _gridWorldSize.y / 2;

        for (int x = 0; x < _gridSizeX; x++) {
            for (int y = 0; y < _gridSizeY; y++) {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * _nodeDiameter + _nodeRadius) + Vector3.up * (y * _nodeDiameter + _nodeRadius);
                bool walkable = !Physics2D.OverlapCircle(worldPoint, _nodeRadius, _unwalkableMask);

                int movementPenalty = 0;

                TileBase tile = _tilemap.GetTile(_tilemap.WorldToCell(worldPoint));
                if (tile != null && _walkableRegionsDictionary.ContainsKey(tile)) {
                    movementPenalty = _walkableRegionsDictionary[tile];
                } else {
                    walkable = false; // If no tile or not in walkable regions, mark as unwalkable
                }

                if (!walkable) {
                    movementPenalty += _obstacleProximityPenalty;
                }

                _grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }

        BlurPanaltyMap(5);
    }

    /// <summary>
    /// Blurs the movement penalty map to create smoother transitions.
    /// </summary>
    /// <param name="blurSize">The size of the blur kernel.</param>
    private void BlurPanaltyMap(int blurSize) {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHorizontalPass = new int[_gridSizeX, _gridSizeY];
        int[,] penaltiesVerticalPass = new int[_gridSizeX, _gridSizeY];

        for (int y = 0; y < _gridSizeY; y++) {
            for (int x = -kernelExtents; x <= kernelExtents; x++) {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += _grid[sampleX, y].MovementPenalty;
            }

            for (int x = 1; x < _gridSizeX; x++) {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, _gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, _gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - _grid[removeIndex, y].MovementPenalty + _grid[addIndex, y].MovementPenalty;
            }
        }

        

        for (int x = 0; x < _gridSizeX; x++) {
            for (int y = -kernelExtents; y <= kernelExtents; y++) {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            _grid[x, 0].MovementPenalty = blurredPenalty;

            for (int y = 1; y < _gridSizeY; y++) {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, _gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, _gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                _grid[x, y].MovementPenalty = blurredPenalty;

                if (blurredPenalty > _penaltyMax) {
                    _penaltyMax = blurredPenalty;
                }
                if (blurredPenalty < _penaltyMin) {
                    _penaltyMin = blurredPenalty;
                }
            }
        }
    }

    /// <summary>
    /// Gets the neighbors of a given node.
    /// </summary>
    /// <param name="node">The node for which neighbors are to be found.</param>
    /// <returns>A list of neighboring nodes.</returns>
    public List<Node> GetNeighbours(Node node) {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) {
                    continue;
                }

                int checkX = node.GridX + x;
                int checkY = node.GridY + y;

                if (checkX >= 0 && checkX < _gridSizeX &&
                    checkY >= 0 && checkY < _gridSizeY) {
                    neighbours.Add(_grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    /// <summary>
    /// Gets the node at a specific world point.
    /// </summary>
    /// <param name="worldPoint">The world point to convert to a node.</param>
    /// <returns>The node at the given world point.</returns>
    public Node GetNodeFromWorldPoint(Vector3 worldPoint) {
        float percentX = Mathf.Clamp01(worldPoint.x / _gridWorldSize.x + 0.5f);
        float percentY = Mathf.Clamp01(worldPoint.y / _gridWorldSize.y + 0.5f);

        int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);
        return _grid[x, y];
    }

    /// <summary>
    /// Draws the grid gizmos in the editor.
    /// </summary>
    private void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(_gridWorldSize.x, _gridWorldSize.y, 1));

        if (_grid != null && _displayGridGizmos) {
            foreach (Node n in _grid) {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(_penaltyMin, _penaltyMax, n.MovementPenalty));

                Gizmos.color = n.Walkable ? Gizmos.color : Color.red;
                Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_nodeDiameter));
            }
        }
    }

    /// <summary>
    /// Represents a terrain type with associated tiles and penalties.
    /// </summary>
    [Serializable]
    public class TerrainType {
        public TileBase[] Tiles;
        public int TerrainPenalty;
    }
}


