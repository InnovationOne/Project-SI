using UnityEngine;

public class PlayerFishingController : MonoBehaviour {
    public LineRenderer lineRendererPrefab;
    public float maxLineLength = 5.0f;
    public float curveHeight = 2.5f;
    public int resolution = 20; // Anzahl der Punkte auf der Parabel

    private LineRenderer[] lineRenderers = new LineRenderer[8];
    private bool shouldDraw = false;

    private void Awake() {
        lineRendererPrefab = GetComponentInChildren<LineRenderer>();
    }

    void Start() {
        return;
        // Initialisiere LineRenderers
        for (int i = 0; i < lineRenderers.Length; i++) {
            lineRenderers[i] = Instantiate(lineRendererPrefab, transform.position, Quaternion.identity, transform);
            lineRenderers[i].positionCount = resolution;
            lineRenderers[i].enabled = false;
        }
    }

    void Update() {
        return;
        if (Input.GetMouseButtonDown(0)) // Maustaste gedrückt
        {
            shouldDraw = true;
            DrawParabolas();
        } else if (Input.GetMouseButtonUp(0)) // Maustaste losgelassen
          {
            shouldDraw = false;
            DisableLines();
        }
    }

    void DrawParabolas() {
        if (!shouldDraw) return;

        for (int i = 0; i < lineRenderers.Length; i++) {
            float angle = i * Mathf.PI / 4; // 45 Grad Schritte
            DrawParabola(lineRenderers[i], angle);
            lineRenderers[i].enabled = true;
        }
    }

    void DrawParabola(LineRenderer lineRenderer, float angle) {
        Vector3 startPosition = transform.position;
        for (int j = 0; j < resolution; j++) {
            float t = j / (float)(resolution - 1);
            float dx = t * maxLineLength;
            float dy = -4 * curveHeight * (t * (1 - t)); // Parabel Formel: y = -4a(x * (1 - x))
            Vector3 point = new Vector3(dx * Mathf.Cos(angle), dy, dx * Mathf.Sin(angle)) + startPosition;
            lineRenderer.SetPosition(j, point);
        }
    }

    void DisableLines() {
        foreach (var lineRenderer in lineRenderers) {
            lineRenderer.enabled = false;
        }
    }
}
