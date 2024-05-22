using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Manages pathfinding requests and processes the results.
/// </summary>
public class PathRequestManager : MonoBehaviour {
    private static PathRequestManager _instance;
    private Queue<PathResult> _results = new Queue<PathResult>();
    private Pathfinding _pathfinding;

    private void Awake() {
        if (_instance != null) {
            Debug.LogError("There is more than one instance of PathRequestManager in the scene!");
            return;
        }
        _instance = this;

        _pathfinding = GetComponent<Pathfinding>();
    }

    /// <summary>
    /// Processes the pathfinding results queue and invokes the callback for each result.
    /// </summary>
    private void Update() {
        if (_results.Count > 0) {
            int itemsInQueue = _results.Count;
            lock (_results) {
                for (int i = 0; i < itemsInQueue; i++) {
                    PathResult result = _results.Dequeue();
                    result.Callback(result.Path, result.Success);
                }
            }
        }
    }

    /// <summary>
    /// Requests a pathfinding operation.
    /// </summary>
    /// <param name="request">The pathfinding request containing start and end points and a callback.</param>
    public static void RequestPath(PathRequest request) {
        ThreadStart threadStart = delegate {
            _instance._pathfinding.FindPath(request, _instance.FinishedProcessingPath);
        };

        threadStart.Invoke();
    }

    /// <summary>
    /// Called when pathfinding is finished. Enqueues the pathfinding result.
    /// </summary>
    /// <param name="pathResult">The result of the pathfinding operation.</param>
    public void FinishedProcessingPath(PathResult pathResult) {
        lock (_results) {
            _results.Enqueue(pathResult);
        }
    }
}

/// <summary>
/// Represents a pathfinding request.
/// </summary>
public struct PathRequest {
    public Vector3 PathStart;
    public Vector3 PathEnd;
    public Action<Vector3[], bool> Callback;

    /// <summary>
    /// Initializes a new instance of the PathRequest struct.
    /// </summary>
    /// <param name="pathStart">The starting point of the path.</param>
    /// <param name="pathEnd">The ending point of the path.</param>
    /// <param name="callback">The callback to be invoked when the path is found.</param>
    public PathRequest(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback) {
        PathStart = pathStart;
        PathEnd = pathEnd;
        Callback = callback;
    }
}

/// <summary>
/// Represents the result of a pathfinding operation.
/// </summary>
public struct PathResult {
    public Vector3[] Path;
    public bool Success;
    public Action<Vector3[], bool> Callback;

    /// <summary>
    /// Initializes a new instance of the PathResult struct.
    /// </summary>
    /// <param name="path">The calculated path.</param>
    /// <param name="success">Indicates whether the pathfinding operation was successful.</param>
    /// <param name="callback">The callback to be invoked with the result.</param>
    public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback) {
        Path = path;
        Success = success;
        Callback = callback;
    }
}
