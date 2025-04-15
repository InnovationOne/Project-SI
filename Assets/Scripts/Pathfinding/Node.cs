using UnityEngine;

/// <summary>
/// Represents a node in the pathfinding grid.
/// </summary>
public class Node : IHeapItem<Node> {
    // 
    public bool Walkable;
    public Vector3 WorldPosition;
    public int GridX;
    public int GridY;
    public int MovementPenalty;

    public int GCost;
    public int HCost;
    public int FCost => GCost + HCost;

    public Node Parent;
    public int HeapIndex { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    /// <param name="walkable">Indicates whether the node is walkable.</param>
    /// <param name="worldPosition">The world position of the node.</param>
    /// <param name="gridX">The x-coordinate of the node in the grid.</param>
    /// <param name="gridY">The y-coordinate of the node in the grid.</param>
    /// <param name="movementPenalty">The movement penalty for the node.</param>
    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY, int movementPenalty) {
        Walkable = walkable;
        WorldPosition = worldPosition;
        GridX = gridX;
        GridY = gridY;
        MovementPenalty = movementPenalty;
    }

    /// <summary>
    /// Compares the current node to another node based on their FCost and HCost.
    /// </summary>
    /// <param name="nodeToCompare">The node to compare to.</param>
    /// <returns>
    /// A value less than zero if this instance is less than <paramref name="nodeToCompare"/>; 
    /// zero if this instance is equal to <paramref name="nodeToCompare"/>; 
    /// a value greater than zero if this instance is greater than <paramref name="nodeToCompare"/>.
    /// </returns>
    public int CompareTo(Node nodeToCompare) {
        int compare = FCost.CompareTo(nodeToCompare.FCost);
        if (compare == 0) {
            compare = HCost.CompareTo(nodeToCompare.HCost);
        }
        return -compare;
    }
}
