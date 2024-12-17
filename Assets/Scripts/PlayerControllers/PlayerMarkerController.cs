using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.ShadowCascadeGUI;

/// <summary>
/// Manages player markers on the marker tilemap, supporting single and area markers.
/// Utilizes Unity's Multiplayer System for networked synchronization.
/// </summary>
public class PlayerMarkerController : NetworkBehaviour {
    public static PlayerMarkerController LocalInstance { get; private set; }

    // Constants
    private const string MARKER_TILEMAP_TAG = "MarkerTilemap";
    private const string BASE_TILEMAP_TAG = "BaseTilemap";

    [Header("Debug: Params")]
    [SerializeField] private float _areaChangeSizeTimer = 0.8f;

    [Header("References")]
    [SerializeField] private TileBase _markerTile;

    // Single marker
    public Vector3Int MarkedCellPosition { get; private set; }
    private Vector3Int _lastCellPosition;

    // Area marker
    private bool _useAreaIndicator;
    private float _currentChangeSizeTimer;
    private int _currentToolMaxRarity;
    private int _currentlyUsedRarity;
    private int _energyCost;
    private Area[] _area;
    private readonly List<Vector3Int> _areaPositions = new();
    private ToolSO.ToolTypes _toolType;

    // Placeable object highlight
    private GameObject _highlightGO;
    private SpriteRenderer _highlightSR;
    public int HighlightId { get; private set; }

    // Cached references
    private Tilemap _targetTilemap;
    private Tilemap _baseTilemap;
    private BoxCollider2D _boxCollider2D;
    private PlayerToolbeltController _toolbeltController;
    private PlayerMovementController _movementController;
    private CropsManager _cropsManager;
    private TilemapManager _tilemapManager;
    private PlayerToolsAndWeaponController _toolsAndWeaponController;
    private InputManager _inputManager;

    #region Unity Callbacks

    private void Awake() {
        // Cache references to reduce overhead in Update
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _targetTilemap = GameObject.FindGameObjectWithTag(MARKER_TILEMAP_TAG)?.GetComponent<Tilemap>();
        _baseTilemap = GameObject.FindGameObjectWithTag(BASE_TILEMAP_TAG)?.GetComponent<Tilemap>();
        if (_targetTilemap == null) {
            Debug.LogError($"Tilemap with tag '{MARKER_TILEMAP_TAG}' not found.");
        }
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerMarkerController in the scene!");
                return;
            }
            LocalInstance = this;

            LocalInstance = this;

            // Cache references to other systems
            _toolbeltController = PlayerToolbeltController.LocalInstance;
            _movementController = PlayerMovementController.LocalInstance;
            _cropsManager = CropsManager.Instance;
            _tilemapManager = TilemapManager.Instance;
            _toolsAndWeaponController = PlayerToolsAndWeaponController.LocalInstance;
            _inputManager = InputManager.Instance;

            // Listen to toolbelt changes and input events
            _toolbeltController.OnToolbeltChanged += HandleToolbeltChanged;
            _inputManager.OnLeftClickCanceled += HandleLeftClickCanceled;
            _inputManager.OnRotateAction += HandleCWRotate;
            _inputManager.OnVMirrorAction += HandleVMirror;
            _inputManager.OnHMirrorAction += HandleHMirror;
        }
    }

    private void OnDestroy() {
        if (LocalInstance == this && _toolbeltController != null) {
            _toolbeltController.OnToolbeltChanged -= HandleToolbeltChanged;
        }

        if (_inputManager != null) {
            _inputManager.OnLeftClickCanceled -= HandleLeftClickCanceled;
        }
    }

    private void Update() {
        if (LocalInstance == null) {
            return;
        }

        // Determine the cell based on movement direction and player position
        Vector2 motionDirection = _movementController.LastMotionDirection;
        Vector3 positionOffset = transform.position + (Vector3)_boxCollider2D.offset + (Vector3)motionDirection;
        Vector3Int gridPosition = _targetTilemap.WorldToCell(positionOffset);

        // Update marker display
        if (_useAreaIndicator) {
            DisplayAreaMarker(gridPosition, motionDirection);
            _lastCellPosition = Vector3Int.zero;
        } else {
            ShowSingleMarker(gridPosition);
        }
    }

    #endregion Unity Callbacks


    #region Event Handlers

    // Reset area indicator if toolbelt changed
    private void HandleToolbeltChanged() {
        if (_useAreaIndicator) {
            _useAreaIndicator = false;
            ResetMarkerTiles();
        }
    }

    // When the left click is released, finalize the area action if active
    private void HandleLeftClickCanceled() {
        if (_useAreaIndicator) {
            _useAreaIndicator = false;
            ExecuteToolAction();
            ResetMarkerTiles();
        }
    }

    #endregion Event Handlers


    #region Public Methods for Area Marker

    // Trigger the area marker visualization when player selects a suitable tool
    public void TriggerAreaMarker(int toolRarity, Area[] area, int energyCost, ToolSO.ToolTypes toolType) {
        _useAreaIndicator = true;
        _currentChangeSizeTimer = 0f;
        _currentlyUsedRarity = 0;
        _currentToolMaxRarity = toolRarity;
        _area = area;
        _energyCost = energyCost;
        _toolType = toolType;
    }

    #endregion Public Methods for Area Marker


    #region Area Marker Internal Logic

    // Displays and updates the area marker
    private void DisplayAreaMarker(Vector3Int position, Vector2 motionDirection) {
        UpdateAreaSize();
        var currentArea = _area[_currentlyUsedRarity];

        // Determine orientation based on direction
        int xSize, ySize;
        if (Mathf.Abs(motionDirection.x) > Mathf.Abs(motionDirection.y)) {
            // Horizontal orientation
            xSize = currentArea.YSize;
            ySize = currentArea.XSize;
        } else {
            // Vertical orientation
            xSize = currentArea.XSize;
            ySize = currentArea.YSize;
        }

        // Determine offset based on direction sign
        Vector3Int offset = CalculateOffset(motionDirection, xSize, ySize);

        // Generate positions
        Vector3Int[,] positions = GenerateCellPositions(position, xSize, ySize, offset);

        // Reset old markers and set new ones
        ResetMarkerTiles();
        MarkAreaPositions(positions);

        // Inform tools controller about current area marker
        _toolsAndWeaponController.AreaMarkerCallback();
    }

    // Execute server-side action based on the tool type
    private void ExecuteToolAction() {
        Vector3IntSerializable[] positionsSerializable = new Vector3IntSerializable[_areaPositions.Count];
        for (int i = 0; i < _areaPositions.Count; i++) {
            positionsSerializable[i] = new Vector3IntSerializable(_areaPositions[i]);
        }

        switch (_toolType) {
            case ToolSO.ToolTypes.Hoe:
                _cropsManager.PlowTilesServerRpc(positionsSerializable, _energyCost);
                break;
            case ToolSO.ToolTypes.WateringCan:
                _cropsManager.WaterCropTileServerRpc(positionsSerializable, _energyCost);
                break;
            default:
                Debug.LogError("Unsupported tool type for area action.");
                break;
        }
    }

    // Gradually increases area size with time until reaching max rarity
    private void UpdateAreaSize() {
        _currentChangeSizeTimer += Time.deltaTime;
        if (_currentChangeSizeTimer >= _areaChangeSizeTimer && _currentlyUsedRarity < _currentToolMaxRarity) {
            _currentChangeSizeTimer = 0f;
            _currentlyUsedRarity++;
        }
    }

    // Calculate offset based on direction, ensuring correct placement for negative directions
    private static Vector3Int CalculateOffset(Vector2 direction, int xSize, int ySize) {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) {
            int yOffset = (ySize - 1) / 2;

            if (direction.x > 0) {
                return new Vector3Int(0, yOffset, 0);
            } else {
                return new Vector3Int(xSize - 1, yOffset, 0);
            }
        } else {
            int xOffset = (xSize - 1) / 2;

            if (direction.y > 0) {
                return new Vector3Int(xOffset, 0, 0);
            } else {
                return new Vector3Int(xOffset, ySize - 1, 0);
            }
        }
    }


    // Generate cell positions for the area
    private static Vector3Int[,] GenerateCellPositions(Vector3Int position, int xSize, int ySize, Vector3Int offset) {
        var positions = new Vector3Int[xSize, ySize];
        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {
                positions[i, j] = position + new Vector3Int(i, j, 0) - offset;
            }
        }
        return positions;
    }

    // Clears previously marked tiles
    private void ResetMarkerTiles() {
        for (int i = 0; i < _areaPositions.Count; i++) {
            SetTile(_areaPositions[i], null);
        }
        _areaPositions.Clear();
    }

    // Marks the tiles for the new area and stores their positions
    private void MarkAreaPositions(Vector3Int[,] positions) {
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

    #endregion Area Marker Internal Logic


    #region Single Marker Logic

    // Shows a single marker tile at the player's target cell
    private void ShowSingleMarker(Vector3Int position) {
        MarkedCellPosition = position;

        int id = _toolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId;

        ItemSO itemSO = ItemManager.Instance.ItemDatabase[id];
        if (itemSO != null && itemSO.ItemType == ItemSO.ItemTypes.PlaceableObject) {
            SetTile(_lastCellPosition, null);
            ObjectSO objectSO = (ObjectSO)itemSO;
            Sprite sprite = objectSO.ObjectRotationSprites[HighlightId];

            // Leicht transparente Farbe zum Hervorheben
            Color highlightColor = new Color(0.5f, 1f, 0.5f, 0.75f);

            // Highlight anzeigen statt Tile
            ShowHighlight(MarkedCellPosition, sprite, objectSO.tileSpriteOffset, highlightColor);
        } else if (_lastCellPosition != MarkedCellPosition) {
            // Wenn es kein platzierbares Objekt ist, zeige vielleicht einen Standardmarker oder gar nichts
            HideHighlight();
            SetTile(_lastCellPosition, null);
            SetTile(MarkedCellPosition, _markerTile);
        } else {
            HideHighlight();
            SetTile(MarkedCellPosition, _markerTile);
        }

        _lastCellPosition = MarkedCellPosition;

    }

    public void ShowHighlight(Vector3Int cellPosition, Sprite sprite, Vector3 offset, Color highlightColor) {
        if (_highlightGO == null) {
            // Neues GameObject erstellen, wenn noch keines vorhanden ist.
            _highlightGO = new GameObject("HighlightGO");
            _highlightSR = _highlightGO.AddComponent<SpriteRenderer>();
            ZDepth zd = _highlightGO.AddComponent<ZDepth>();
            zd._isObjectStationary = false;
        }

        // Weltposition aus Tilemap-Koordinaten ermitteln
        Vector3 worldPos = _baseTilemap.GetCellCenterWorld(cellPosition);
        worldPos += offset; // Offset aus dem ObjectSO hinzufügen

        _highlightGO.transform.position = worldPos;
        _highlightSR.sprite = sprite;
        _highlightSR.color = highlightColor;
    }

    public void HideHighlight() {
        if (_highlightGO != null) {
            Destroy(_highlightGO);
            _highlightGO = null;
            _highlightSR = null;
        }
    }

    private void HandleCWRotate() {
        if (_highlightGO != null) {
            int id = _toolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId;
            ItemSO itemSO = ItemManager.Instance.ItemDatabase[id];
            ObjectSO objectSO = (ObjectSO)itemSO;
            
            if (objectSO.ObjectRotationSprites.Length > 1) {
                HighlightId++;
                if (HighlightId > objectSO.ObjectRotationSprites.Length - 1) {
                    HighlightId = 0;
                }
            }
        }
    }

    // TODO: If needed by the players, implement the following method
    private void HandleCCWRotate() {
        if (_highlightGO != null) {
            int id = _toolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId;
            ItemSO itemSO = ItemManager.Instance.ItemDatabase[id];
            ObjectSO objectSO = (ObjectSO)itemSO;

            if (objectSO.ObjectRotationSprites.Length > 1) {
                if (HighlightId < 0) {
                    HighlightId = objectSO.ObjectRotationSprites.Length - 1;
                } else {
                    HighlightId--;
                }
            }
        }
    }

    private void HandleVMirror() {
        // TODO: Implement vertical mirroring
    }

    private void HandleHMirror() {
        // TODO: Implement horizontal mirroring
    }

    // Sets the specified tile at the given position (null clears the tile)
    private void SetTile(Vector3Int position, TileBase tile = null) {
        if (_targetTilemap != null) {
            _targetTilemap.SetTile(position, tile);
        }
    }

    #endregion Single Marker Logic
}
