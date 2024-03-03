using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class DrawPolygonCollider2D : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab;
    LineRenderer lineRenderer;
    [SerializeField] PolygonCollider2D polygonCollider2D;

    void Start() {
        lineRenderer = Instantiate(linePrefab).GetComponent<LineRenderer>();
        lineRenderer.transform.SetParent(transform);
        lineRenderer.transform.localPosition = Vector3.zero;
    }

    void Update() {
        HiliteCollider();
    }

    void HiliteCollider() {

        var points = polygonCollider2D.GetPath(0); // dumb assumption for demo -- only one path

        Vector3[] positions = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) {
            positions[i] = transform.TransformPoint(points[i]);
        }
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(positions);
    }
}
