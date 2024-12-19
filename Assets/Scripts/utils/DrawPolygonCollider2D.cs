using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// Highlights the polygon defined by a PolygonCollider2D using a LineRenderer, syncs data via netcode.
[RequireComponent(typeof(PolygonCollider2D))]
public class DrawPolygonCollider2D : NetworkBehaviour
{
    [SerializeField] GameObject _linePrefab;
    [SerializeField] PolygonCollider2D _polygonCollider2D;

    // Networked polygon points for multiplayer synchronization
    NetworkList<Vector2> _syncedPoints;

    LineRenderer _lineRenderer;
    readonly List<Vector2> _pointsCache = new();

    void Awake() {
        _syncedPoints = new NetworkList<Vector2>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // If this is the server/host, populate the polygon points.
        if (IsServer) {
            _polygonCollider2D.GetPath(0, _pointsCache);
            _syncedPoints.Clear();
            for (int i = 0; i < _pointsCache.Count; i++) {
                _syncedPoints.Add(_pointsCache[i]);
            }
        }

        // Listen for changes to syncedPoints on all clients.
        _syncedPoints.OnListChanged += OnPolygonPointsChanged;

        // Set up the line renderer after data is received (or immediately if host).
        SetupLineRenderer();
        UpdateLinePositions();
    }

    void OnPolygonPointsChanged(NetworkListEvent<Vector2> changeEvent) {
        // Whenever polygon points change, update the line positions.
        UpdateLinePositions();
    }

    void SetupLineRenderer() {
        if (_lineRenderer == null && _linePrefab != null) {
            var lineObj = Instantiate(_linePrefab, transform);
            _lineRenderer = lineObj.GetComponent<LineRenderer>();
            if (_lineRenderer != null) {
                _lineRenderer.useWorldSpace = true;
            }
        }
    }

    void UpdateLinePositions() {
        if (_lineRenderer == null) { return; }

        int count = _syncedPoints.Count;
        if (count == 0) { return; }

        Vector3[] positions = new Vector3[count];
        for (int i = 0; i < count; i++) {
            // Transform from local to world space.
            positions[i] = transform.TransformPoint(_syncedPoints[i]);
        }

        _lineRenderer.positionCount = count;
        _lineRenderer.SetPositions(positions);
    }
}
