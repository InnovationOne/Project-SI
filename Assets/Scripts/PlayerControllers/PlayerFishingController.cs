using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine;

public class PlayerFishingController : MonoBehaviour {
    public Vector2 fishingRodTip; // Die Spitze der Angelrute, wo die Schnur erscheint
    public GameObject bobberPrefab; // Prefab für die Boje

    public LineRenderer lineRendererPrefab; // Linie zur Darstellung der Flugbahn
    public float maxCastingDistance = 2f; // Maximale Wurfweite
    public float castingSpeed = 2f; // Geschwindigkeit, mit der die Wurfweite zunimmt
    public float timeToBiteMin = 10f; // Minimale Wartezeit bis ein Fisch anbeißt
    public float timeToBiteMax = 30f; // Maximale Wartezeit bis ein Fisch anbeißt

    private GameObject bobber;
    private LineRenderer lineRenderer;
    private bool isFishing = false;
    private bool fishBiting = false;
    private float currentCastingDistance = 0f;
    private bool isCasting = false;

    void Update() {
        fishingRodTip = transform.position;
        if (Input.GetKeyDown(KeyCode.Space) && !isFishing) {
            // Startet den Casting-Prozess
            isCasting = true;
            StartPreview();
        }

        if (Input.GetKey(KeyCode.Space) && isCasting) {
            // Erhöht die Wurfweite basierend auf der Dauer des Drückens
            currentCastingDistance += castingSpeed * Time.deltaTime;
            currentCastingDistance = Mathf.Clamp(currentCastingDistance, 0, maxCastingDistance);
            UpdatePreview();
        }

        if (Input.GetKeyUp(KeyCode.Space) && isCasting) {
            // Sobald Space losgelassen wird, wird die Angel ausgeworfen
            isCasting = false;
            StopPreview();
            StartCoroutine(CastLine());
        }

        if (Input.GetKeyDown(KeyCode.Space) && isFishing && !fishBiting) {
            // Holt die Angel ein, wenn noch kein Fisch angebissen hat
            ReelInWithoutCatch();
        }

        if (Input.GetKeyDown(KeyCode.Space) && isFishing && fishBiting) {
            // Holt die Angel ein, wenn ein Fisch angebissen hat
            ReelInWithCatch();
        }
    }

    private void StartPreview() {
        if (bobber == null) {
            bobber = Instantiate(bobberPrefab, fishingRodTip, Quaternion.identity);
            bobber.GetComponent<SpriteRenderer>().color -= new Color(0, 0, 0, 0.5f);
        }

        if (lineRenderer == null) {
            lineRenderer = Instantiate(lineRendererPrefab.gameObject, fishingRodTip, Quaternion.identity).GetComponent<LineRenderer>();
            lineRenderer.startColor -= new Color(0, 0, 0, 0.5f);
            lineRenderer.endColor -= new Color(0, 0, 0, 0.5f);
        }
    }

    private void UpdatePreview() {
        Vector3 castPosition = fishingRodTip + PlayerMovementController.LocalInstance.LastMotionDirection * currentCastingDistance;
        bobber.transform.position = castPosition;

        // Zeichnet die Parabel
        Vector3[] linePositions = new Vector3[2];
        linePositions[0] = fishingRodTip;
        linePositions[1] = castPosition;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(linePositions);
    }

    private void StopPreview() {
        bobber.GetComponent<SpriteRenderer>().color += new Color(0, 0, 0, 0.5f);
        lineRenderer.startColor += new Color(0, 0, 0, 0.5f);
        lineRenderer.endColor += new Color(0, 0, 0, 0.5f);
    }


    private IEnumerator CastLine() {
        isFishing = true;

        // Warten, bis ein Fisch anbeißt
        float timeToBite = Random.Range(timeToBiteMin, timeToBiteMax);
        yield return new WaitForSeconds(timeToBite);

        // Simuliere einen Fischbiss
        fishBiting = true;
        Debug.Log("Ein Fisch hat angebissen! Drücke SPACE, um ihn einzuholen.");
    }

    private void ReelInWithoutCatch() {
        Debug.Log("Du hast die Angel eingeholt, ohne etwas zu fangen.");
        ResetVariables();
    }

    private void ReelInWithCatch() {
        if (fishBiting) {
            Debug.Log("Du hast den Fisch gefangen!");
            ResetVariables();
        }
    }

    private void ResetVariables() {
        Destroy(bobber);
        Destroy(lineRenderer.gameObject);
        isFishing = false;
        fishBiting = false;
        currentCastingDistance = 0f;
        isCasting = false;
    }
}
