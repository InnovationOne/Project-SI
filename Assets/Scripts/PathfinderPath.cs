using UnityEngine;

public class PathfinderPath {
    public readonly Vector3[] LookPoints;
    public readonly Line[] TurnBoundaries;
    public readonly int FinishLineIndex;
    public readonly int SlowDownIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathfinderPath"/> class.
    /// </summary>
    /// <param name="waypoints">An array of 3D points representing the path.</param>
    /// <param name="startPos">The starting position of the path.</param>
    /// <param name="turnDst">The distance from the path to create the turn boundaries.</param>
    /// <param name="stoppingDst">The distance from the end of the path to start slowing down.</param>
    public PathfinderPath(Vector3[] waypoints, Vector3 startPos, float turnDst, float stoppingDst) {
        LookPoints = waypoints;
        TurnBoundaries = new Line[LookPoints.Length];
        FinishLineIndex = TurnBoundaries.Length - 1;

        Vector2 previousPoint = startPos;
        for (int i = 0; i < LookPoints.Length; i++) {
            Vector2 currentPoint = LookPoints[i];
            Vector2 directionToCurrentPoint = (currentPoint - previousPoint).normalized;
            Vector2 turnBoundaryPoint = (i == FinishLineIndex) ? currentPoint : currentPoint - directionToCurrentPoint * turnDst;
            TurnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - directionToCurrentPoint * turnDst);
            previousPoint = turnBoundaryPoint;
        }

        float dstFromEndPoint = 0;
        for (int i = LookPoints.Length - 1; i > 0; i--) {
            dstFromEndPoint += Vector3.Distance(LookPoints[i], LookPoints[i - 1]);
            if (dstFromEndPoint > stoppingDst) {
                SlowDownIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// Draws the path and turn boundaries as gizmos in the Unity Editor.
    /// </summary>
    public void DrawWithGizmos() {
        Gizmos.color = Color.black;
        foreach (Vector3 p in LookPoints) {
            Gizmos.DrawCube(p + Vector3.forward, new Vector3(0.3f, 0.3f, 0.3f));
        }

        Gizmos.color = Color.white;
        foreach (Line l in TurnBoundaries) {
            l.DrawWithGizmos(2);
        }
    }
}
