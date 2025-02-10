using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages player markers on the marker tilemap, supporting single and area markers.
/// Utilizes Unity's Multiplayer System for networked synchronization.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerMarkerController : NetworkBehaviour {
    // Constants for readability
    const string MARKER_TILEMAP_TAG = "MarkerTilemap";
    const string BASE_TILEMAP_TAG = "BaseTilemap";

    [Header("Marker Timing")]
    [SerializeField] float _areaChangeSizeTimer = 0.8f;

    [Header("Tile References")]
    [SerializeField] TileBase _markerTile;

    public Vector2 test = new Vector2(1, 1);

    // Single-marker state
    public Vector3Int MarkedCellPosition { get; private set; }
    Vector3Int _lastCellPosition;

    // Area-marker state
    bool _useAreaIndicator;
    float _currentChangeSizeTimer;
    int _currentToolMaxRarity;
    int _currentlyUsedRarity;
    int _energyCost;
    Area[] _area;
    readonly List<Vector3Int> _areaPositions = new();
    ToolSO.ToolTypes _toolType;

    // Highlight object for placeables
    GameObject _highlightGO;
    SpriteRenderer _highlightSR;
    public int HighlightId { get; private set; }

    // Cached references
    BoxCollider2D _boxCollider2D;
    PlayerToolbeltController _toolbeltController;
    PlayerMovementController _movementController;
    PlayerToolsAndWeaponController _toolsAndWeaponController;
    Tilemap _targetTilemap;
    Tilemap _baseTilemap;
    CropsManager _cropsManager;
    InputManager _inputManager;

    void Awake() {
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _toolbeltController = GetComponent<PlayerToolbeltController>();
        _movementController = GetComponent<PlayerMovementController>();
        _toolsAndWeaponController = GetComponent<PlayerToolsAndWeaponController>();

        GameObject markerTilemap = GameObject.FindGameObjectWithTag(MARKER_TILEMAP_TAG);
        if (markerTilemap != null) _targetTilemap = markerTilemap.GetComponent<Tilemap>();

        GameObject baseTilemap = GameObject.FindGameObjectWithTag(BASE_TILEMAP_TAG);
        if (baseTilemap != null) _baseTilemap = baseTilemap.GetComponent<Tilemap>();
    }

    void Start() {
        // Cache references to other systems for quick access
        _cropsManager = GameManager.Instance.CropsManager;
        _inputManager = GameManager.Instance.InputManager;

        // Subscribe to relevant events
        _toolbeltController.OnToolbeltChanged += HandleToolbeltChanged;
        _inputManager.OnLeftClickCanceled += HandleLeftClickCanceled;
        _inputManager.OnRotateAction += HandleCWRotate;
        _inputManager.OnVMirrorAction += HandleVMirror;
        _inputManager.OnHMirrorAction += HandleHMirror;
    }

    void OnDestroy() {
        // Unsubscribe from events to prevent memory leaks
        _toolbeltController.OnToolbeltChanged -= HandleToolbeltChanged;
        _inputManager.OnLeftClickCanceled -= HandleLeftClickCanceled;
        _inputManager.OnRotateAction -= HandleCWRotate;
        _inputManager.OnVMirrorAction -= HandleVMirror;
        _inputManager.OnHMirrorAction -= HandleHMirror;
    }

    void Update() {
        // Determine the target cell based on player direction and position
        Vector2 motionDirection = _movementController.LastMotionDirection;
        Vector3 positionOffset = transform.position + (Vector3)motionDirection + new Vector3(0f, 0.9f);
        Vector3Int gridPosition = _targetTilemap.WorldToCell(positionOffset);

        // Choose between showing area marker or single marker
        if (_useAreaIndicator) {
            DisplayAreaMarker(gridPosition, motionDirection);
            _lastCellPosition = Vector3Int.zero;
        } else {
            ShowSingleMarker(gridPosition);
        }
    }

    // Disable area indicator if toolbelt changes mid-drag
    void HandleToolbeltChanged() {
        if (!_useAreaIndicator) return;

        _useAreaIndicator = false;
        ClearOldMarkedTiles();
    }

    // Finalize area action when mouse is released
    void HandleLeftClickCanceled() {
        if (!_useAreaIndicator) return;

        _useAreaIndicator = false;
        ExecuteToolAction();
        ClearOldMarkedTiles();
    }

    // Show area marker growth for tools that affect multiple tiles.
    public void TriggerAreaMarker(int toolRarity, Area[] area, int energyCost, ToolSO.ToolTypes toolType) {
        _useAreaIndicator = true;
        _currentChangeSizeTimer = 0f;
        _currentlyUsedRarity = 0;
        _currentToolMaxRarity = toolRarity;
        _area = area;
        _energyCost = energyCost;
        _toolType = toolType;
    }

    #region -------------------- Area Marker Internal Logic --------------------
    // Displays and updates the area marker over time until the max rarity
    void DisplayAreaMarker(Vector3Int position, Vector2 motionDirection) {
        UpdateAreaSize();
        var currentArea = _area[_currentlyUsedRarity];

        // Determine orientation of the marker (horizontal/vertical)
        int xSize, ySize;
        if (Mathf.Abs(motionDirection.x) > Mathf.Abs(motionDirection.y)) {
            xSize = currentArea.YSize;
            ySize = currentArea.XSize;
        } else {
            xSize = currentArea.XSize;
            ySize = currentArea.YSize;
        }

        var offset = CalculateOffset(motionDirection, xSize, ySize);
        var positions = GenerateCellPositions(position, xSize, ySize, offset);
        ClearOldMarkedTiles();
        MarkAreaPositionTiles(positions);
        _toolsAndWeaponController.AreaMarkerCallbackClientRpc();
    }

    // Execute server-side action based on the tool type
    void ExecuteToolAction() {
        // Convert positions to a serializable form
        var posSer = new Vector3IntSerializable[_areaPositions.Count];
        for (int i = 0; i < _areaPositions.Count; i++) {
            posSer[i] = new Vector3IntSerializable(_areaPositions[i]);
        }

        // Execute server-side actions based on the current tool type
        switch (_toolType) {
            case ToolSO.ToolTypes.Hoe:
                _cropsManager.PlowTilesServerRpc(posSer, _energyCost);
                break;
            case ToolSO.ToolTypes.WateringCan:
                _cropsManager.WaterCropTileServerRpc(posSer, _energyCost);
                break;
            default:
                Debug.LogError("Unsupported tool type for area action.");
                break;
        }
    }

    // Increases area size with time until reaching max rarity
    void UpdateAreaSize() {
        _currentChangeSizeTimer += Time.deltaTime;
        if (_currentChangeSizeTimer >= _areaChangeSizeTimer && _currentlyUsedRarity < _currentToolMaxRarity) {
            _currentChangeSizeTimer = 0f;
            _currentlyUsedRarity++;
        }
    }

    // Calculate offset based on direction, ensuring correct placement for negative directions
    Vector3Int CalculateOffset(Vector2 direction, int xSize, int ySize) {
        // Offset ensures the area marker aligns properly based on direction
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) {
            int yOffset = (ySize - 1) / 2;
            return direction.x > 0 ? new Vector3Int(0, yOffset, 0) : new Vector3Int(xSize - 1, yOffset, 0);
        } else {
            int xOffset = (xSize - 1) / 2;
            return direction.y > 0 ? new Vector3Int(xOffset, 0, 0) : new Vector3Int(xOffset, ySize - 1, 0);
        }
    }


    // Generate cell positions for the area
    Vector3Int[,] GenerateCellPositions(Vector3Int position, int xSize, int ySize, Vector3Int offset) {
        var positions = new Vector3Int[xSize, ySize];
        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {
                positions[i, j] = position + new Vector3Int(i, j, 0) - offset;
            }
        }
        return positions;
    }

    private void ClearOldMarkedTiles() {
        for (int i = 0; i < _areaPositions.Count; i++) {
            SetTile(_areaPositions[i]);
        }
        _areaPositions.Clear();
    }

    private void MarkAreaPositionTiles(Vector3Int[,] positions) {
        int xSize = positions.GetLength(0);
        int ySize = positions.GetLength(1);

        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {
                Vector3Int pos = positions[i, j];
                _areaPositions.Add(pos);
                SetTile(pos, _markerTile);
            }
        }
    }

    #endregion -------------------- Area Marker Internal Logic --------------------

    #region -------------------- Single Marker Logic --------------------

    // Shows a single marker tile at the player's target cell
    private void ShowSingleMarker(Vector3Int position) {
        // Update the single marker position for quick actions like placing a single object
        MarkedCellPosition = position;
        int id = _toolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId;
        var itemSO = GameManager.Instance.ItemManager.ItemDatabase[id];

        // If the selected item is placeable, show a highlight instead of a tile
        if (itemSO is ObjectSO objectSO && objectSO.ObjectRotationSprites != null && objectSO.ObjectRotationSprites.Length > 0 && itemSO.ItemType == ItemSO.ItemTypes.PlaceableObject) {
            SetTile(_lastCellPosition);
            Sprite sprite = objectSO.ObjectRotationSprites[HighlightId];
            var highlightColor = new Color(0.5f, 1f, 0.5f, 0.75f);
            ShowHighlight(MarkedCellPosition, sprite, objectSO.tileSpriteOffset, highlightColor);
        } else if (_lastCellPosition != MarkedCellPosition) {
            // For non-placeable items, simply show a standard marker tile
            HideHighlight();
            SetTile(_lastCellPosition);
            SetTile(MarkedCellPosition, _markerTile);
        } else {
            // If no changes, just ensure highlight is hidden and the tile is set
            HideHighlight();
            SetTile(MarkedCellPosition, _markerTile);
        }

        _lastCellPosition = MarkedCellPosition;

    }

    public void ShowHighlight(Vector3Int cellPosition, Sprite sprite, Vector3 offset, Color highlightColor) {
        // Create a highlight object if none exists yet
        if (_highlightGO == null) {
            _highlightGO = new GameObject("HighlightGO");
            _highlightSR = _highlightGO.AddComponent<SpriteRenderer>();
        }

        // Position and color the highlight object at the target cell
        var worldPos = _baseTilemap.GetCellCenterWorld(cellPosition) + offset;
        _highlightGO.transform.position = worldPos;
        _highlightSR.sprite = sprite;
        _highlightSR.color = highlightColor;
    }

    // Clean up highlight object when not needed
    public void HideHighlight() {
        if (_highlightGO == null) return;

        Destroy(_highlightGO);
        _highlightGO = null;
        _highlightSR = null;
    }

    private void HandleCWRotate() {
        if (_highlightGO == null) return;

        // Cycle through available rotation sprites when rotating highlight
        int id = _toolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId;
        ItemSO itemSO = GameManager.Instance.ItemManager.ItemDatabase[id];
        if (itemSO is ObjectSO objectSO && objectSO.ObjectRotationSprites.Length > 1) {
            HighlightId++;
            if (HighlightId >= objectSO.ObjectRotationSprites.Length) {
                HighlightId = 0;
            }
        }

    }

    private void HandleCCWRotate() {
        // TODO: If needed by the players, implement the following method
    }

    private void HandleVMirror() {
        // TODO: Implement vertical mirroring
    }

    private void HandleHMirror() {
        // TODO: Implement horizontal mirroring
    }

    // Sets the specified tile at the given position (null clears the tile)
    private void SetTile(Vector3Int position, TileBase tile = null) => _targetTilemap.SetTile(position, tile);

    #endregion -------------------- Single Marker Logic --------------------
}
