using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : MonoBehaviour {
    /*
    private const int MOVE_DIAGONAL_COST = 14;
    private const int MOVE_STRAIGHT_COST = 10;

    public Tilemap Tilemap;
    private List<Vector3> _pathPoints = new();
    public Vector3 test;
    
    private void Update() {
        
        if (Input.GetKeyDown(KeyCode.LeftAlt)) {
            MoveTo(1f, test);
        }
    }

    public void MoveTo(float moveSpeed, Vector3 endWorldPosition) {
        _pathPoints.Clear();
        Vector3Int startGridPosition = Tilemap.WorldToCell(transform.position);
        Vector3Int endGridPosition = Tilemap.WorldToCell(endWorldPosition);

        bool[] walkabilityMap = new bool[Tilemap.cellBounds.size.x * Tilemap.cellBounds.size.y];
        for (int x = 0; x < Tilemap.cellBounds.size.x; x++) {
            for (int y = 0; y < Tilemap.cellBounds.size.y; y++) {
                Vector3Int cellPosition = new Vector3Int(x + Tilemap.cellBounds.xMin, y + Tilemap.cellBounds.yMin, 0);
                Vector3 worldPosition = Tilemap.CellToWorld(cellPosition);
                Collider2D collider = Physics2D.OverlapBox(worldPosition, new Vector2(1f, 1f), 0);
                walkabilityMap[x + y * Tilemap.cellBounds.size.x] = collider == null;
            }
        }
        NativeArray<bool> nativeWalkabilityMap = new NativeArray<bool>(walkabilityMap, Allocator.TempJob);
        var findPathJob = new FindPathJob {
            startPosition = new int2(startGridPosition.x, startGridPosition.y),
            endPosition = new int2(endGridPosition.x, endGridPosition.y),
            size = new int2(Tilemap.cellBounds.size.x, Tilemap.cellBounds.size.y),
            pathResult = new NativeList<int2>(Allocator.TempJob),
            walkabilityMap = nativeWalkabilityMap,
        };

        JobHandle jobHandle = findPathJob.Schedule();
        jobHandle.Complete();

        _pathPoints.Add(endWorldPosition);
        for (int i = 1; i < findPathJob.pathResult.Length; i++) {
            Vector3 worldPos = Tilemap.CellToWorld(new Vector3Int(findPathJob.pathResult[i].x, findPathJob.pathResult[i].y, 0));
            worldPos += new Vector3(0.5f, 0.5f);
            worldPos.z = 0;
            _pathPoints.Add(worldPos);
        }

        nativeWalkabilityMap.Dispose();
        //findPathJob.walkabilityMap.Dispose();
        findPathJob.pathResult.Dispose();
        _pathPoints.Reverse();

        StartCoroutine(FollowPath(moveSpeed));
    }

    private IEnumerator FollowPath(float moveSpeed) {
        foreach (var point in _pathPoints) {
            while (Vector3.Distance(transform.position, point) > 0.1f) {
                transform.position = Vector3.MoveTowards(transform.position, point, moveSpeed * Time.deltaTime);
                yield return null;
            }
        }
        _pathPoints.Clear();
    }

    [BurstCompile]
    private struct FindPathJob : IJob {
        public int2 startPosition;
        public int2 endPosition;
        public int2 size;
        public NativeList<int2> pathResult;
        public NativeArray<bool> walkabilityMap;

        public void Execute() {
            NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(size.x * size.y, Allocator.Temp);

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    PathNode pathNode = new() {
                        X = x,
                        Y = y,
                        Index = CalculateIndex(x, y, size.x),
                        CameFromNodeIndex = -1,
                        GCost = int.MaxValue,
                        HCost = CalculateDistanceCost(new int2(x, y), endPosition),
                    };
                    pathNode.IsWalkable = walkabilityMap[pathNode.Index];
                    pathNode.CalculateFCost();
                    pathNodeArray[pathNode.Index] = pathNode;
                }
            }

            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp) {
                [0] = new int2(-1, 0), // Left
                [1] = new int2(+1, 0), // Right
                [2] = new int2(0, +1), // Up
                [3] = new int2(0, -1), // Down
                [4] = new int2(-1, -1), // Left Down
                [5] = new int2(-1, +1), // Left Up
                [6] = new int2(+1, -1), // Right Down
                [7] = new int2(+1, +1), // Right Up
            };

            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, size.x);

            PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, size.x)];
            startNode.GCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.Index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.Index);

            while (openList.Length > 0) {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                var currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex) {
                    // Reached our destination
                    ReconstructPath(currentNode, pathNodeArray, pathResult);
                    break;
                }

                for (int i = 0; i < openList.Length; i++) {
                    if (openList[i] == currentNodeIndex) {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }
                closedList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++) {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.X + neighbourOffset.x, currentNode.Y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, size)) {
                        // Neighbour not inside the grid
                        continue;
                    }

                    int neighbourIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, size.x);
                    if (closedList.Contains(neighbourIndex)) {
                        // Node already processed
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourIndex];
                    if (!neighbourNode.IsWalkable) {
                        Debug.Log("Knoten bei " + neighbourPosition + " ist nicht begehbar.");
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.X, currentNode.Y);
                    int tentativeGCost = currentNode.GCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                    if (tentativeGCost < neighbourNode.GCost) {
                        neighbourNode.CameFromNodeIndex = currentNodeIndex;
                        neighbourNode.GCost = tentativeGCost;
                        neighbourNode.CalculateFCost();
                        pathNodeArray[neighbourIndex] = neighbourNode;

                        if (!openList.Contains(neighbourIndex)) {
                            openList.Add(neighbourIndex);
                        }
                    }
                }
            }

            neighbourOffsetArray.Dispose();
            pathNodeArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        private void ReconstructPath(PathNode endNode, NativeArray<PathNode> pathNodeArray, NativeList<int2> pathResult) {
            PathNode currentNode = endNode;
            while (currentNode.CameFromNodeIndex != -1) {
                pathResult.Add(new int2(currentNode.X, currentNode.Y));
                currentNode = pathNodeArray[currentNode.CameFromNodeIndex];
            }
            pathResult.Add(new int2(currentNode.X, currentNode.Y));
        }

        private bool IsPositionInsideGrid(int2 gridPos, int2 size) {
            return
                gridPos.x >= 0 &&
                gridPos.y >= 0 &&
                gridPos.x < size.x &&
                gridPos.y < size.y;
        }

        private int CalculateIndex(int x, int y, int gridWidth) {
            return x + y * gridWidth;
        }

        private int CalculateDistanceCost(int2 a, int2 b) {
            int xDistance = math.abs(a.x - b.x);
            int yDistance = math.abs(a.y - b.y);
            int remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }

        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray) {
            PathNode lowestCostPathNode = pathNodeArray[openList[0]];
            for (int i = 1; i < openList.Length; i++) {
                PathNode testPathNode = pathNodeArray[openList[i]];
                if (testPathNode.FCost < lowestCostPathNode.FCost) {
                    lowestCostPathNode = testPathNode;
                }
            }
            return lowestCostPathNode.Index;
        }
    }

    private struct PathNode {
        public int X, Y, Index, CameFromNodeIndex, GCost, HCost, FCost;
        public bool IsWalkable;

        public void CalculateFCost() {
            FCost = GCost + HCost;
        }
    }
*/
}