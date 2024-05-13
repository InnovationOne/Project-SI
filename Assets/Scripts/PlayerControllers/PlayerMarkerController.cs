using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

// This script manages the marker on the marker tilemap
public class PlayerMarkerController : NetworkBehaviour {
    public static PlayerMarkerController LocalInstance { get; private set; }

    [Header("Debug: Params")]
    [SerializeField] private float _areaChangeSizeTimer = 0.8f;

    [Header("References")]
    [SerializeField] private TileBase _markerTile;
    
    // Single Marker Params
    public Vector3Int MarkedCellPosition { get; private set; }
    private Vector3Int _lastCellPosition;

    // Area Marker Params
    private bool _useAreaIndicator;
    private float _currentChangeSizeTimer;
    private int _currentToolMaxRarity;
    private int _currentlyUsedRarity;
    private int _energyCost;
    private int[] _areaSizes;
    private List<Vector3Int> _areaPositions = new();
    private ToolSO.ToolTypes _toolType;

    // References
    private Tilemap _targetTilemap;
    private BoxCollider2D _boxCollider2D;


    private void Awake() {
        // Get references
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _targetTilemap = GameObject.FindGameObjectWithTag("MarkerTilemap").GetComponent<Tilemap>();
    }

    private void Start() => PlayerToolbeltController.LocalInstance.OnToolbeltChanged += PlayerToolbeltController_OnToolbeltChanged;
    
    private new void OnDestroy() => PlayerToolbeltController.LocalInstance.OnToolbeltChanged -= PlayerToolbeltController_OnToolbeltChanged;
    
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerMarkerController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    private void PlayerToolbeltController_OnToolbeltChanged() {
        if (_useAreaIndicator) {
            _useAreaIndicator = false;
            ResetMarkerTiles();
            EnableMoveSpeed();
        }
    }

    private void Update() {
        if (LocalInstance == null) {
            return;
        }

        Vector2 motionDirection = PlayerMovementController.LocalInstance.LastMotionDirection;
        Vector3 positionOffset = new(
            transform.position.x + _boxCollider2D.offset.x + motionDirection.x,
            transform.position.y + _boxCollider2D.offset.y + motionDirection.y);
        Vector3Int gridPosition = TilemapManager.Instance.GetGridPosition(positionOffset);

        if (_useAreaIndicator) {
            UseAreaMarker(gridPosition, motionDirection);
            _lastCellPosition = Vector3Int.zero;
        } else {
            ShowMarker(gridPosition);
        }
    }

    #region Area Marker
    public void TriggerAreaMarker(int toolRarity, int[] areaSizes, int energyCost, ToolSO.ToolTypes toolType) {
        ResetAreaMarkerState(toolRarity, areaSizes, energyCost, toolType);
        PlayerMovementController.LocalInstance.ChangeMoveSpeed(false);
    }

    private void ResetAreaMarkerState(int toolRarity, int[] sizes, int cost, ToolSO.ToolTypes type) {
        _currentChangeSizeTimer = 0f;
        _currentlyUsedRarity = 0;
        _currentToolMaxRarity = toolRarity;
        _areaSizes = sizes;
        _energyCost = cost;
        _useAreaIndicator = true;
        _toolType = type;
    }

    private void UseAreaMarker(Vector3Int position, Vector2 lastMotionVector) {
        ShowAreaMarker(position, lastMotionVector);
        PlayerToolsAndWeaponController.LocalInstance.AreaMarkerCallback();

        if (Input.GetMouseButtonUp(0)) {
            _useAreaIndicator = false;
            ProcessToolAction();
            ResetMarkerTiles();
            EnableMoveSpeed();
        }
    }

    private void ShowAreaMarker(Vector3Int position, Vector2 motionDirection) {
        UpdateAreaSize();
        int size = _areaSizes[_currentlyUsedRarity];
        CalculateAreaDimensions(motionDirection, size, out int xsize, out int ysize);
        Vector3Int[,] positions = GenerateCellPositions(position, motionDirection, xsize, ysize);

        ResetMarkerTiles();

        MarkTilesAndAddToList(positions);
    }

    private void ProcessToolAction() {
        switch (_toolType) {
            case ToolSO.ToolTypes.Hoe:
                CropsManager.Instance.PlowTiles(_areaPositions, _energyCost);
                break;
            case ToolSO.ToolTypes.WateringCan:
                CropsManager.Instance.WaterTiles(_areaPositions, _energyCost);
                break;
            default:
                Debug.LogError("No valid tool type.");
                break;
        }
    }

    private void EnableMoveSpeed() => PlayerMovementController.LocalInstance.ChangeMoveSpeed(true);
    
    // Change the area size every _areaChangeSizeTimer seconds
    private void UpdateAreaSize() {
        _currentChangeSizeTimer += Time.deltaTime;
        if (_currentChangeSizeTimer >= _areaChangeSizeTimer && _currentlyUsedRarity < _currentToolMaxRarity) {
            _currentChangeSizeTimer = 0f;
            _currentlyUsedRarity++;
        }
    }

    // Calculate the area dimensions based on the last motion vector (horizontal or vertical)
    private void CalculateAreaDimensions(Vector2 lastMotionVector, int currentAreaSize, out int xsize, out int ysize) {
        if (lastMotionVector.x == 0) {
            xsize = currentAreaSize / 10;
            ysize = currentAreaSize % 10;
        } else {
            xsize = currentAreaSize % 10;
            ysize = currentAreaSize / 10;
        }
    }

    private Vector3Int[,] GenerateCellPositions(Vector3Int position, Vector2 lastMotionVector, int xsize, int ysize) {
        Vector3Int[,] positions = new Vector3Int[xsize, ysize];
        for (int i = 0; i < xsize; i++) {
            for (int j = 0; j < ysize; j++) {
                if (lastMotionVector.x > 0) {
                    positions[i, j] = position - new Vector3Int(0, (ysize - 1) / 2) + new Vector3Int(i, j, 0);
                } else if (lastMotionVector.x < 0) {
                    positions[i, j] = position - new Vector3Int(0, (ysize - 1) / 2) + new Vector3Int(-i, j, 0);
                } else if (lastMotionVector.y > 0) {
                    positions[i, j] = position - new Vector3Int((xsize - 1) / 2, 0) + new Vector3Int(i, j, 0);
                } else if (lastMotionVector.y < 0) {
                    positions[i, j] = position - new Vector3Int((xsize - 1) / 2, 0) + new Vector3Int(i, -j, 0);
                }
            }
        }
        return positions;
    }

    private void ResetMarkerTiles() {
        foreach (var cell in _areaPositions) {
            SetTile(cell);
        }
        _areaPositions.Clear();
    }

    private void MarkTilesAndAddToList(Vector3Int[,] positions) {
        foreach (var position in positions) {
            _areaPositions.Add(position);
            SetTile(position, _markerTile);
        }
    }
    #endregion


    #region Single Marker
    // Show the single maker on the map
    private void ShowMarker(Vector3Int position) {
        MarkedCellPosition = position;

        if (MarkedCellPosition != _lastCellPosition) {
            SetTile(_lastCellPosition);
            SetTile(MarkedCellPosition, _markerTile);

            _lastCellPosition = MarkedCellPosition;
        }
    }

    private void SetTile(Vector3Int position, TileBase tileBase = null) => _targetTilemap.SetTile(position, tileBase);
    
    #endregion
}
