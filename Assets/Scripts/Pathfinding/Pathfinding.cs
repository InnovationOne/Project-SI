using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles pathfinding logic using the A* algorithm.
/// </summary>
[RequireComponent(typeof(PathfindingGrid))]
public class Pathfinding : MonoBehaviour {
    private const int DIAGONAL_MOVE_COST = 14;
    private const int STRAIGHT_MOVE_COST = 10;

    private PathfindingGrid _pathfindingGrid;

    /// <summary>
    /// Initializes the Pathfinding component.
    /// </summary>
    private void Awake() {
        _pathfindingGrid = GetComponent<PathfindingGrid>();
    }

    /// <summary>
    /// Finds a path between the start and end points in the path request.
    /// </summary>
    /// <param name="pathRequest">The pathfinding request containing start and end points and a callback.</param>
    /// <param name="callback">The callback to be invoked with the pathfinding result.</param>
    public void FindPath(PathRequest pathRequest, Action<PathResult> callback) {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Node startNode = _pathfindingGrid.GetNodeFromWorldPoint(pathRequest.PathStart);
        Node endNode = _pathfindingGrid.GetNodeFromWorldPoint(pathRequest.PathEnd);

        if (startNode.Walkable && endNode.Walkable) {
            Heap<Node> openSet = new Heap<Node>(_pathfindingGrid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0) {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                // Found the path
                if (currentNode == endNode) {
                    pathSuccess = true;
                    break;
                }


                foreach (Node neighbour in _pathfindingGrid.GetNeighbours(currentNode)) {
                    if (!neighbour.Walkable ||
                        closedSet.Contains(neighbour)) {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.GCost + GetDistance(currentNode, neighbour) + neighbour.MovementPenalty;
                    if (newMovementCostToNeighbour < neighbour.GCost || !openSet.Contains(neighbour)) {
                        neighbour.GCost = newMovementCostToNeighbour;
                        neighbour.HCost = GetDistance(neighbour, endNode);
                        neighbour.Parent = currentNode;

                        if (!openSet.Contains(neighbour)) {
                            openSet.Add(neighbour);
                        } else {
                            openSet.UpdateItem(neighbour);
                        }
                    }
                }
            }
        }

        if (pathSuccess) {
            waypoints = RetracePath(startNode, endNode);
            pathSuccess = waypoints.Length > 0;
        }
        callback(new PathResult(waypoints, pathSuccess, pathRequest.Callback));
    }

    /// <summary>
    /// Retraces the path from the end node to the start node.
    /// </summary>
    /// <param name="startNode">The starting node of the path.</param>
    /// <param name="endNode">The ending node of the path.</param>
    /// <returns>An array of waypoints representing the path.</returns>
    private Vector3[] RetracePath(Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);

        return waypoints;
    }

    /// <summary>
    /// Simplifies the path by removing unnecessary nodes.
    /// </summary>
    /// <param name="path">The original path as a list of nodes.</param>
    /// <returns>An array of waypoints representing the simplified path.</returns>
    private Vector3[] SimplifyPath(List<Node> path) {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++) {
            Vector2 directionNew = new Vector2(path[i - 1].GridX - path[i].GridX, path[i - 1].GridY - path[i].GridY);
            if (directionNew != directionOld) {
                waypoints.Add(path[i].WorldPosition);
            }
            directionOld = directionNew;
        }

        return waypoints.ToArray();
    }

    /// <summary>
    /// Calculates the distance between two nodes.
    /// </summary>
    /// <param name="nodeA">The first node.</param>
    /// <param name="nodeB">The second node.</param>
    /// <returns>The distance between the two nodes.</returns>
    private int GetDistance(Node nodeA, Node nodeB) {
        int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);

        if (dstX > dstY) {
            return DIAGONAL_MOVE_COST * dstY + STRAIGHT_MOVE_COST * (dstX - dstY);
        }

        return DIAGONAL_MOVE_COST * dstX + STRAIGHT_MOVE_COST * (dstY - dstX);
    }
}
