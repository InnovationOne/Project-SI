using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AdjustingObject : PlaceableObject {
    protected SpriteRenderer _visual;
    protected PolygonCollider2D _collider;

    private const int LAYER_MASK_BIT = 8;
    private LayerMask _layerMask = 1 << LAYER_MASK_BIT;
    private Grid _grid;

    private Vector3Int _fencePosition;
    private Vector3Int[] _adjacentPositions;
    private bool[] _currentNeighborFlags = new bool[4];
    protected int _itemId;
    protected int _patternIndex;

    [NonSerialized] private float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

   

    private static readonly bool[][] _neighborhoodPatterns = {
        new bool[] { false, false, false, false }, // None              Gate: Horizontal
        new bool[] { true, false, false, false }, // Up                 Gate: Vertical
        new bool[] { false, true, false, false }, // Right              Gate: Horizontal
        new bool[] { false, false, true, false }, // Down               Gate: Vertical
        new bool[] { false, false, false, true }, // Left               Gate: Horizontal
        new bool[] { true, true, false, false }, // Up-Right            Gate: Horizontal
        new bool[] { true, false, true, false }, // Up-Down             Gate: Vertical
        new bool[] { true, false, false, true }, // Up-Left             Gate: Horizontal
        new bool[] { false, true, true, false }, // Right-Down          Gate: Horizontal
        new bool[] { false, true, false, true }, // Right-Left          Gate: Horizontal
        new bool[] { false, false, true, true }, // Down-Left           Gate: Horizontal
        new bool[] { true, true, true, false }, // Up-Right-Down        Gate: Horizontal
        new bool[] { true, true, false, true }, // Up-Right-Left        Gate: Horizontal
        new bool[] { false, true, true, true }, // Right-Down-Left      Gate: Horizontal
        new bool[] { true, false, true, true }, // Up-Down-Left         Gate: Horizontal
        new bool[] { true, true, true, true }, // Up-Right-Down-Left    Gate: Horizontal
    };

    private void Awake() {
        _grid = FindFirstObjectByType<Grid>();
        _visual = GetComponent<SpriteRenderer>();
        _collider = GetComponent<PolygonCollider2D>();
    }

    /// <summary>
    /// Initializes the AdjustingObject with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    public virtual void InitializePreLoad(int itemId) {
        _itemId = itemId;
        _fencePosition = _grid.WorldToCell(transform.position);
        InitializeAdjacentPositions();
        UpdateNeighborConnections(true);
    }

    /// <summary>
    /// Initializes the array of adjacent positions based on the current fence position.
    /// </summary>
    private void InitializeAdjacentPositions() {
        _adjacentPositions = new Vector3Int[] {
            _fencePosition + Vector3Int.up,
            _fencePosition + Vector3Int.right,
            _fencePosition + Vector3Int.down,
            _fencePosition + Vector3Int.left
        };
    }

    /// <summary>
    /// Updates the neighbor connections of the object.
    /// </summary>
    /// <param name="add">A boolean value indicating whether to add or remove the neighbor connections.</param>
    private void UpdateNeighborConnections(bool add) {
        for (int i = 0; i < _adjacentPositions.Length; i++) {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(_grid.GetCellCenterWorld(_adjacentPositions[i]), 0.1f, _layerMask);
            UpdateNeighborPresence(colliders, i, add);
        }
        UpdateVisualBasedOnNeighbors();
    }

    /// <summary>
    /// Updates the presence of neighboring objects based on the given colliders and index.
    /// </summary>
    /// <param name="colliders">An array of colliders to check for neighboring objects.</param>
    /// <param name="index">The index of the current object in the neighbor flags array.</param>
    /// <param name="add">A flag indicating whether to add or remove the neighboring object.</param>
    private void UpdateNeighborPresence(Collider2D[] colliders, int index, bool add) {
        foreach (var collider in colliders) {
            if (collider.gameObject.TryGetComponent(out AdjustingObject adjacentObject)) {
                _currentNeighborFlags[index] = true;
                adjacentObject.UpdateNeighbor(add, (index + 2) % 4);
                break;
            }
        }
    }

    /// <summary>
    /// Updates the neighbor flag at the specified index and updates the visual based on the neighbors.
    /// </summary>
    /// <param name="add">A boolean value indicating whether to add or remove the neighbor.</param>
    /// <param name="index">The index of the neighbor flag to update.</param>
    public void UpdateNeighbor(bool add, int index) {
        _currentNeighborFlags[index] = add;
        UpdateVisualBasedOnNeighbors();
    }

    /// <summary>
    /// Updates the visual representation of the object based on its neighbors.
    /// </summary>
    protected virtual void UpdateVisualBasedOnNeighbors() {
        _patternIndex = FindMatchingPatternIndex();

        
    }

    /// <summary>
    /// Finds the index of the matching pattern in the neighborhood patterns array.
    /// </summary>
    /// <returns>The index of the matching pattern, or -1 if no match is found.</returns>
    private int FindMatchingPatternIndex() {
        for (int i = 0; i < _neighborhoodPatterns.Length; i++) {
            if (_neighborhoodPatterns[i].SequenceEqual(_currentNeighborFlags)) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Picks up the object and updates neighbor connections.
    /// </summary>
    public virtual void PickUpItemsInPlacedObject(Player player) {
        UpdateNeighborConnections(false);
        // Additional pick up logic can be implemented here
    }
    public virtual void Interact(Player player) {
        
    }

    /// <summary>
    /// Compares two boolean arrays for equality.
    /// </summary>
    private class BoolArrayEqualityComparer : IEqualityComparer<bool[]> {
        /// <summary>
        /// Determines whether two boolean arrays are equal.
        /// </summary>
        /// <param name="x">The first boolean array to compare.</param>
        /// <param name="y">The second boolean array to compare.</param>
        /// <returns>True if the boolean arrays are equal; otherwise, false.</returns>
        public bool Equals(bool[] x, bool[] y) {
            return x.SequenceEqual(y);
        }

        /// <summary>
        /// Returns the hash code for the specified boolean array.
        /// </summary>
        /// <param name="obj">The boolean array for which to get the hash code.</param>
        /// <returns>The hash code for the specified boolean array.</returns>
        public int GetHashCode(bool[] obj) {
            return obj.Aggregate(0, (acc, value) => (acc << 1) | (value ? 1 : 0));
        }
    }
}


