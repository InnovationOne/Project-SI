using Unity.Burst;
using UnityEngine;
// Provides parabolic interpolation between two points. Burst compiled for performance.
[BurstCompile]
public class MathParabola {
    // Returns a parabolic point between two Vector3s at a given t.
    public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float t) {
        float f(float x) => -4f * height * x * x + 4f * height * x;
        var mid = Vector3.Lerp(start, end, t);
        return new Vector3(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
    }

    // Returns a parabolic point between two Vector2s at a given t.
    public static Vector2 Parabola(Vector2 start, Vector2 end, float height, float t) {
        float f(float x) => -4f * height * x * x + 4f * height * x;
        var mid = Vector2.Lerp(start, end, t);
        return new Vector2(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t));
    }
}
