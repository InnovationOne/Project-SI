using System.Collections.Generic;
using Unity.Netcode;
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
    private List<Vector3Int> _areaPositions;
    private ToolTypes _toolType;

    // References
    private Tilemap _targetTilemap;
    private BoxCollider2D _boxCollider2D;
    private PlayerToolbeltController _playerToolbeltController;
    private PlayerMovementController _playerMovementController;
    private PlayerToolsAndWeaponController _playerToolsAndWeaponController;
    private CropsManager _cropsManager;
    private TilemapManager _tilemapManager;


    private void Awake() {
        // Get references
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerToolsAndWeaponController = GetComponent<PlayerToolsAndWeaponController>();

        // Get the marker tilemap
        _targetTilemap = GameObject.FindGameObjectWithTag("MarkerTilemap").GetComponent<Tilemap>();

        _areaPositions = new List<Vector3Int>();
    }

    private void Start() {
        _cropsManager = CropsManager.Instance;
        _tilemapManager = TilemapManager.Instance;

        _playerToolbeltController.OnToolbeltChanged += PlayerToolbeltController_OnToolbeltChanged;
    }

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

        Vector2 lastMotionVector = _playerMovementController.LastMotionDirection;
        Vector3 position = new(
            transform.position.x + _boxCollider2D.offset.x + lastMotionVector.x,
            transform.position.y + _boxCollider2D.offset.y + lastMotionVector.y);

        Debug.Log(_useAreaIndicator);
        if (_useAreaIndicator) {
            UseAreaMarker(_tilemapManager.GetGridPosition(position), lastMotionVector);
            _lastCellPosition = Vector3Int.zero;
        } else {
            ShowMarker(_tilemapManager.GetGridPosition(position));
        }
    }


    #region Area Marker
    public void TriggerAreaMarker(int toolRarity, int[] areaSizes, int energyCost, ToolTypes toolType) {
        _currentChangeSizeTimer = 0f;
        _currentlyUsedRarity = 0;
        _currentToolMaxRarity = toolRarity;
        _areaSizes = areaSizes;
        _energyCost = energyCost;
        _useAreaIndicator = true;
        _toolType = toolType;
        _playerMovementController.SetMoveAndRunSpeed(false);
    }

    private void UseAreaMarker(Vector3Int position, Vector2 lastMotionVector) {
        ShowAreaMarker(position, lastMotionVector);
        _playerToolsAndWeaponController.AreaMarkerCallback();

        if (Input.GetMouseButtonUp(0)) {
            _useAreaIndicator = false;
            ProcessToolAction();
            ResetMarkerTiles();
            EnableMoveSpeed();
        }
    }

    private void ProcessToolAction() {
        switch (_toolType) {
            case ToolTypes.Hoe:
                _cropsManager.PlowTiles(_areaPositions, _energyCost);
                break;
            case ToolTypes.WateringCan:
                _cropsManager.WaterTiles(_areaPositions, _energyCost);
                break;
            default:
                Debug.LogError("No valid tool type.");
                break;
        }
    }

    

    private void EnableMoveSpeed() {
        _playerMovementController.SetMoveAndRunSpeed(true);
    }


    private void ShowAreaMarker(Vector3Int position, Vector2 lastMotionVector) {
        UpdateAreaSize();

        int currentAreaSize = _areaSizes[_currentlyUsedRarity];
        CalculateAreaDimensions(lastMotionVector, currentAreaSize, out int xsize, out int ysize);

        Vector3Int[,] cellPositions = GenerateCellPositions(position, lastMotionVector, xsize, ysize);

        ResetMarkerTiles();

        MarkTilesAndAddToList(cellPositions);
    }

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

        if (MarkedCellPosition == _lastCellPosition) {
            return;
        }

        SetTile(_lastCellPosition);
        SetTile(MarkedCellPosition, _markerTile);

        _lastCellPosition = MarkedCellPosition;
    }

    // Set a tile to marker on the marker tilemap
    private void SetTile(Vector3Int position, TileBase tileBase = null) {
        _targetTilemap.SetTile(position, tileBase);
    }
    #endregion
}
