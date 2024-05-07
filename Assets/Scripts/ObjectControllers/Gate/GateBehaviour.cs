using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GateBehaviour : Interactable {
    [SerializeField] private GateVisual _lowerGateVisual;
    [SerializeField] private GateVisual _upperGateVisual;
    [SerializeField] private LayerMask _gateLayerMask; // LayerMask to detect other fences

    private Grid _grid;

    private Vector3Int _fencePosition;
    private Vector3Int[] _adjacentPositions;

    private bool[] _currentNeighborBoolArray = new bool[4]; // up, right, down, left

    private bool _isOpened;
    private bool _openedRightToLeft;

    // Define neighborhood patterns
    private readonly List<bool[]> _neighborhoodPatterns = new List<bool[]> {
        new bool[] { false, false, false, false }, // None
        new bool[] { true, false, false, false }, // Up
        new bool[] { false, true, false, false }, // Right
        new bool[] { false, false, true, false }, // Down
        new bool[] { false, false, false, true }, // Left
        new bool[] { true, true, false, false }, // Up-Right
        new bool[] { true, false, true, false }, // Up-Down
        new bool[] { true, false, false, true }, // Up-Left
        new bool[] { false, true, true, false }, // Right-Down
        new bool[] { false, true, false, true }, // Right-Left
        new bool[] { false, false, true, true }, // Down-Left
        new bool[] { true, true, true, false }, // Up-Right-Down
        new bool[] { true, true, false, true }, // Up-Right-Left
        new bool[] { false, true, true, true }, // Right-Down-Left
        new bool[] { true, false, true, true }, // Up-Down-Left
        new bool[] { true, true, true, true }, // Up-Right-Down-Left
    };


    private void Awake() {
        _grid = FindFirstObjectByType<Grid>();
    }

    private void Start() {
        _fencePosition = _grid.WorldToCell(transform.position);
        _adjacentPositions = new Vector3Int[]
        {
            _fencePosition + new Vector3Int(0, 1, 0), // Up
            _fencePosition + new Vector3Int(1, 0, 0), // Right
            _fencePosition + new Vector3Int(0, -1, 0), // Down
            _fencePosition + new Vector3Int(-1, 0, 0), // Left
        };

        CheckNeighbor(true, true);
    }

    private void CheckNeighbor(bool add, bool primary = false) {
        int index = 0;
        foreach (Vector3Int position in _adjacentPositions) {
            Collider2D[] adjacentColliders = Physics2D.OverlapCircleAll(_grid.GetCellCenterWorld(position), 0.1f, _gateLayerMask);

            if (adjacentColliders.Length > 0) {
                for (int i = 0; i < adjacentColliders.Length; i++) {
                    if (adjacentColliders[i].gameObject.TryGetComponent<FenceBehaviour>(out var adjacentFence)) {
                        _currentNeighborBoolArray[index] = true;
                        if (primary) {
                            adjacentFence.UpdateForNewNeighbor(add, index);
                        }
                        break;
                    } else if (adjacentColliders[i].gameObject.TryGetComponent<GateBehaviour>(out var adjacentGate)) {
                        _currentNeighborBoolArray[index] = true;
                        if (primary) {
                            adjacentGate.UpdateForNewNeighbor(add, index);
                        }
                        break;
                    }
                }
            }

            index++;
        }

        UpdateSprite();
    }

    public void UpdateForNewNeighbor(bool add, int index) {
        int newIndex = -1;
        if (index == 0) {
            newIndex = 2;
        } else if (index == 1) {
            newIndex = 3;
        } else if (index == 2) {
            newIndex = 0;
        } else if (index == 3) {
            newIndex = 1;
        }

        // To add a neighbor "add" is true, to remove "add" is false
        _currentNeighborBoolArray[newIndex] = add;
        UpdateSprite();
    }

    private void UpdateSprite() {
        // Determine the current neighborhood pattern
        bool[] currentPattern = new bool[] { _currentNeighborBoolArray[0], _currentNeighborBoolArray[1], _currentNeighborBoolArray[2], _currentNeighborBoolArray[3] };
        int index = _neighborhoodPatterns.FindIndex(pattern => Enumerable.SequenceEqual(pattern, currentPattern));

        // If the current pattern isn't found, default to the first pattern
        if (index == -1) {
            index = 0;
        }

        // Convert to gate sprites
        if (index == 1 || index == 3 || index == 6) {
            if (_openedRightToLeft) {
                index = 2;
            } else {
                index = 1;
            }
        } else {
            index = 0;
        }

        // Assign gate sprite and collider2d based on current pattern
        _lowerGateVisual.UpdateVisual(index, _isOpened);

        
        _upperGateVisual.UpdateVisual(index, _isOpened);
        _upperGateVisual.gameObject.SetActive(_isOpened);
    }

    public override void Interact(Player character) {
        if (_isOpened) {
            _isOpened = false;
            _openedRightToLeft = false;
        } else {
            _isOpened = true;
            if (gameObject.transform.position.x <= character.transform.position.x) {
                _openedRightToLeft = true;
            } else {
                _openedRightToLeft = false;
            }
        }

        CheckNeighbor(true, false);
    }

    public override void PickUpItemsInPlacedObject(Player player) {
        CheckNeighbor(false, true);
    }
}
