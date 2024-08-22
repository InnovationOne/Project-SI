using UnityEngine;

public struct Line {
    private const float VERTICAL_LINE_GRADIENT = 1e5f;

    private float _gradient;
    private float _y_intercept;
    private Vector2 _pointOnLine1;
    private Vector2 _pointOnLine2;

    private float _gradientPerpendicular;

    private bool _approachSide;

    /// <summary>
    /// Represents a line in 2D space defined by a point on the line and a point perpendicular to the line.
    /// </summary>
    /// <param name="pointOnLine">A point that lies on the line.</param>
    /// <param name="pointPerpendicularToLine">A point that is perpendicular to the line.</param>
    public Line(Vector2 pointOnLine, Vector2 pointPerpendicularToLine) {
        float dx = pointOnLine.x - pointPerpendicularToLine.x;
        float dy = pointOnLine.y - pointPerpendicularToLine.y;

        if (dx == 0) {
            _gradientPerpendicular = VERTICAL_LINE_GRADIENT;
        } else {
            _gradientPerpendicular = dy / dx;
        }

        if (_gradientPerpendicular == 0) {
            _gradient = VERTICAL_LINE_GRADIENT;
        } else {
            _gradient = -1 / _gradientPerpendicular;
        }

        _y_intercept = pointOnLine.y - _gradient * pointOnLine.x;
        _pointOnLine1 = pointOnLine;
        _pointOnLine2 = pointOnLine + new Vector2(1, _gradient);

        _approachSide = false;
        _approachSide = GetSide(pointPerpendicularToLine);
    }

    /// <summary>
    /// Determines which side of the line a given point is on.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>
    /// <c>true</c> if the point is on the side of the line determined by the order of the line's points;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool GetSide(Vector2 point) =>
        (point.x - _pointOnLine1.x) * (_pointOnLine2.y - _pointOnLine1.y) > 
        (point.y - _pointOnLine1.y) * (_pointOnLine2.x - _pointOnLine1.x);
    
    /// <summary>
    /// Checks if the given point has crossed the line.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the point has crossed the line, false otherwise.</returns>
    public bool HasCrossedLine(Vector2 point) => GetSide(point) != _approachSide;

    /// <summary>
    /// Calculates the distance between the line and a given point.
    /// </summary>
    /// <param name="point">The point to calculate the distance from.</param>
    /// <returns>The distance between the line and the point.</returns>
    public float DistanceFromPoint(Vector2 point) {
        float y_interceptPerpendicular = point.y - _gradientPerpendicular * point.x;
        float intersectX = (y_interceptPerpendicular - _y_intercept) / (_gradient - _gradientPerpendicular);
        float intersectY = _gradient * intersectX + _y_intercept;
        return Vector2.Distance(point, new Vector2(intersectX, intersectY));
    }

    /// <summary>
    /// Draws a line with Gizmos in the scene view.
    /// </summary>
    /// <param name="length">The length of the line.</param>
    public void DrawWithGizmos(float length) {
        Vector3 lineDir = new Vector3(1, _gradient, 0).normalized;
        Vector3 lineCentre = new Vector3(_pointOnLine1.x, _pointOnLine1.y, 0) + Vector3.forward;
        Gizmos.DrawLine(lineCentre - lineDir * length / 2f, lineCentre + lineDir * length / 2f);
    }
}
