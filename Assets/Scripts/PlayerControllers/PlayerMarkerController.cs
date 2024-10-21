using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages player markers on the marker tilemap, supporting single and area markers.
/// Utilizes Unity's Multiplayer System for networked synchronization.
/// </summary>
public class PlayerMarkerController : NetworkBehaviour {
    public static PlayerMarkerController LocalInstance { get; private set; }

    // Constants
    private const string MARKER_TILEMAP_TAG = "MarkerTilemap";

    [Header("Debug: Params")]
    [SerializeField] private float _areaChangeSizeTimer = 0.8f;

    [Header("References")]
    [SerializeField] private TileBase _markerTile;

    // Single Marker Parameters
    public Vector3Int MarkedCellPosition { get; private set; }
    private Vector3Int _lastCellPosition;

    // Area Marker Parameters
    private bool _useAreaIndicator;
    private float _currentChangeSizeTimer;
    private int _currentToolMaxRarity;
    private int _currentlyUsedRarity;
    private int _energyCost;
    private Area _areaSize;
    private readonly List<Vector3Int> _areaPositions = new();
    private ToolSO.ToolTypes _toolType;

    // Cached References
    private Tilemap _targetTilemap;
    private BoxCollider2D _boxCollider2D;
    private PlayerToolbeltController _toolbeltController;
    private PlayerMovementController _movementController;
    private CropsManager _cropsManager;
    private TilemapManager _tilemapManager;
    private PlayerToolsAndWeaponController _toolsAndWeaponController;

    #region Unity Callbacks

    private void Awake() {
        // Cache references to reduce overhead in Update
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _targetTilemap = GameObject.FindGameObjectWithTag(MARKER_TILEMAP_TAG)?.GetComponent<Tilemap>();

        // Validate references
        if (_targetTilemap == null) {
            Debug.LogError($"Tilemap with tag '{MARKER_TILEMAP_TAG}' not found.");
        }
    }

    private new void OnDestroy() => PlayerToolbeltController.LocalInstance.OnToolbeltChanged -= HandleToolbeltChanged;

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerMarkerController in the scene!");
                return;
            }
            LocalInstance = this;

            // Cache other singleton instances
            _toolbeltController = PlayerToolbeltController.LocalInstance;
            _movementController = PlayerMovementController.LocalInstance;
            _cropsManager = CropsManager.Instance;
            _tilemapManager = TilemapManager.Instance;
            _toolsAndWeaponController = PlayerToolsAndWeaponController.LocalInstance;

            _toolbeltController.OnToolbeltChanged += HandleToolbeltChanged;
        }
    }

    private void Update() {
        if (LocalInstance == null) {
            return;
        }

        Vector2 motionDirection = _movementController.LastMotionDirection;
        Vector3 positionOffset = new(
            transform.position.x + _boxCollider2D.offset.x + motionDirection.x,
            transform.position.y + _boxCollider2D.offset.y + motionDirection.y
        );

        Vector3Int gridPosition = _tilemapManager.GetGridPosition(positionOffset);

        if (_useAreaIndicator) {
            HandleAreaMarker(gridPosition, motionDirection);
            _lastCellPosition = Vector3Int.zero;
        } else {
            ShowSingleMarker(gridPosition);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles changes in the player's toolbelt.
    /// Resets area indicators and re-enables movement speed.
    /// </summary>
    private void HandleToolbeltChanged() {
        if (_useAreaIndicator) {
            _useAreaIndicator = false;
            ResetMarkerTiles();
        }
    }

    #endregion

    #region Area Marker

    /// <summary>
    /// Initiates the area marker with the specified parameters.
    /// </summary>
    /// <param name="toolRarity">Rarity level of the tool.</param>
    /// <param name="areaSize">Size of the area to mark.</param>
    /// <param name="energyCost">Energy cost associated with the action.</param>
    /// <param name="toolType">Type of the tool being used.</param>
    public void TriggerAreaMarker(int toolRarity, Area areaSize, int energyCost, ToolSO.ToolTypes toolType) {
        ResetAreaMarkerState(toolRarity, areaSize, energyCost, toolType);
    }

    /// <summary>
    /// Initializes the state for the area marker.
    /// </summary>
    private void ResetAreaMarkerState(int toolRarity, Area areaSize, int cost, ToolSO.ToolTypes type) {
        _currentChangeSizeTimer = 0f;
        _currentlyUsedRarity = 0;
        _currentToolMaxRarity = toolRarity;
        _areaSize = areaSize;
        _energyCost = cost;
        _useAreaIndicator = true;
        _toolType = type;
    }

    /// <summary>
    /// Handles the logic for displaying and updating the area marker.
    /// </summary>
    private void HandleAreaMarker(Vector3Int position, Vector2 lastMotionVector) {
        DisplayAreaMarker(position, lastMotionVector);
        _toolsAndWeaponController.AreaMarkerCallback();

        if (Input.GetMouseButtonUp(0)) {
            _useAreaIndicator = false;
            ExecuteToolAction();
            ResetMarkerTiles();
        }
    }

    /// <summary>
    /// Displays the area marker based on the current position and motion direction.
    /// </summary>
    private void DisplayAreaMarker(Vector3Int position, Vector2 motionDirection) {
        UpdateAreaSize();

        (int xSize, int ySize) = CalculateAreaDimensions(motionDirection, _areaSize);
        Vector3Int[,] positions = GenerateCellPositions(position, motionDirection, xSize, ySize);

        ResetMarkerTiles();
        MarkTilesAndStorePositions(positions);
    }

    /// <summary>
    /// Executes the action associated with the current tool type.
    /// </summary>
    private void ExecuteToolAction() {
        Vector3IntSerializable[] positionsSerializable = _areaPositions.Select(v => new Vector3IntSerializable(v)).ToArray();

        switch (_toolType) {
            case ToolSO.ToolTypes.Hoe:
                _cropsManager.PlowTilesServerRpc(positionsSerializable, _energyCost);
                break;
            case ToolSO.ToolTypes.WateringCan:
                _cropsManager.WaterCropTileServerRpc(positionsSerializable, _energyCost);
                break;
            default:
                Debug.LogError("Invalid tool type selected.");
                break;
        }
    }

    /// <summary>
    /// Updates the area size based on the timer and tool rarity.
    /// </summary>
    private void UpdateAreaSize() {
        _currentChangeSizeTimer += Time.deltaTime;
        if (_currentChangeSizeTimer >= _areaChangeSizeTimer &&
            _currentlyUsedRarity < _currentToolMaxRarity) {
            _currentChangeSizeTimer = 0f;
            _currentlyUsedRarity++;
        }
    }

    /// <summary>
    /// Calculates the dimensions of the area based on the motion direction.
    /// </summary>
    private (int xSize, int ySize) CalculateAreaDimensions(Vector2 motionDirection, Area currentAreaSize) {
        if (Mathf.Approximately(motionDirection.x, 0)) {
            return (currentAreaSize.XSize, currentAreaSize.YSize);
        } else {
            return (currentAreaSize.YSize, currentAreaSize.XSize);
        }
    }

    /// <summary>
    /// Generates cell positions for the area marker based on the current position and motion direction.
    /// </summary>
    private Vector3Int[,] GenerateCellPositions(Vector3Int position, Vector2 motionDirection, int xSize, int ySize) {
        Vector3Int[,] positions = new Vector3Int[xSize, ySize];
        Vector3Int offset;

        // Determine the offset based on motion direction
        if (motionDirection.x > 0) {
            offset = new Vector3Int(0, (ySize - 1) / 2, 0);
        } else if (motionDirection.x < 0) {
            offset = new Vector3Int(-xSize + 1, (ySize - 1) / 2, 0);
        } else if (motionDirection.y > 0) {
            offset = new Vector3Int((xSize - 1) / 2, 0, 0);
        } else {
            offset = new Vector3Int((xSize - 1) / 2, -ySize + 1, 0);
        }

        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {
                Vector3Int cellPosition = position + new Vector3Int(i, j, 0) - offset;
                positions[i, j] = cellPosition;
            }
        }

        return positions;
    }

    /// <summary>
    /// Resets the marker tiles by clearing all marked area positions.
    /// </summary>
    private void ResetMarkerTiles() {
        foreach (var cell in _areaPositions) {
            SetTile(cell);
        }
        _areaPositions.Clear();
    }

    /// <summary>
    /// Marks the tiles on the tilemap and stores their positions.
    /// </summary>
    private void MarkTilesAndStorePositions(Vector3Int[,] positions) {
        int xSize = positions.GetLength(0);
        int ySize = positions.GetLength(1);

        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {
                Vector3Int pos = positions[i, j];
                if (!_areaPositions.Contains(pos)) {
                    _areaPositions.Add(pos);
                    SetTile(pos, _markerTile);
                }
            }
        }
    }

    #endregion


    #region Single Marker

    /// <summary>
    /// Displays the single marker on the map.
    /// </summary>
    private void ShowSingleMarker(Vector3Int position) {
        MarkedCellPosition = position;

        if (MarkedCellPosition != _lastCellPosition) {
            SetTile(_lastCellPosition);
            SetTile(MarkedCellPosition, _markerTile);
            _lastCellPosition = MarkedCellPosition;
        }
    }

    /// <summary>
    /// Sets a tile at the specified position.
    /// </summary>
    /// <param name="position">Grid position to set the tile.</param>
    /// <param name="tile">Tile to set. If null, clears the tile.</param>
    private void SetTile(Vector3Int position, TileBase tile = null) => _targetTilemap.SetTile(position, tile);

    #endregion
}
