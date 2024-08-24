using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerFishingController : MonoBehaviour {
    public Vector2 FishingRodTip; // Die Spitze der Angelrute, wo die Schnur erscheint
    public GameObject BobberPrefab;
    public LineRenderer LineRendererPrefab;
    public FishDatabaseSO FishDatabaseSO;

    private const float _maxCastingDistance = 3f; // Maximale Wurfweite
    private const float _castingSpeed = 2f; // Geschwindigkeit, mit der die Wurfweite zunimmt
    private const float _timeToBiteMin = 2f; // Minimale Wartezeit bis ein Fisch anbeißt
    private const float _timeToBiteMax = 5f; // Maximale Wartezeit bis ein Fisch anbeißt
    private const float _castArcHeight = 2f; // Höhe der Parabel
    private const float _lineSagHeight = 0.1f; // Höhe des Durchhängens der Angelschnur
    private const int _segmentCount = 20;

    private Tilemap _fishingTilemap;
    private GameObject _bobber;
    private LineRenderer _lineRenderer;
    private bool _isFishing = false;
    private bool _fishBiting = false;
    private float _currentCastingDistance = 0f;
    private bool _isCasting = false;
    private FishSO _fish;

    private static readonly Dictionary<string, int> _tileNameToIdMap = new Dictionary<string, int> {
        { "Coast", 0 },
        { "Sea", 0 },
        { "Deep_Sea", 0 },
        { "River", 3 },
        { "Lake", 4 }
    };

    private void Start() {
        FishDatabaseSO.InitializeFishData();
        _fishingTilemap = GameObject.FindGameObjectWithTag("FishingTilemap").GetComponent<Tilemap>();
    }

    void Update() {
        // This position hast to change depending on the player rotation and the tip of the fishing rod.
        FishingRodTip = transform.position;


        if (Input.GetKeyDown(KeyCode.Space) && !_isFishing) {
            // Startet den Casting-Prozess
            _isCasting = true;
            StartPreview();
        }

        if (Input.GetKey(KeyCode.Space) && _isCasting) {
            // Erhöht die Wurfweite basierend auf der Dauer des Drückens
            _currentCastingDistance += _castingSpeed * Time.deltaTime;
            _currentCastingDistance = Mathf.Clamp(_currentCastingDistance, 0, _maxCastingDistance);
            UpdatePreview();
        }

        if (Input.GetKeyUp(KeyCode.Space) && _isCasting) {
            // Sobald Space losgelassen wird, wird die Angel ausgeworfen
            _isCasting = false;
            StopPreview();
            StartCoroutine(CastLine());
        }

        if (Input.GetKeyDown(KeyCode.Space) && _isFishing && !_fishBiting) {
            // Holt die Angel ein, wenn noch kein Fisch angebissen hat
            ReelInWithoutCatch();
        }

        if (Input.GetKeyDown(KeyCode.Space) && _isFishing && _fishBiting) {
            // Holt die Angel ein, wenn ein Fisch angebissen hat
            ReelInWithCatch();
        }
    }

    private void StartPreview() {
        if (_bobber == null) {
            _bobber = Instantiate(BobberPrefab, FishingRodTip, Quaternion.identity);
            _bobber.GetComponent<SpriteRenderer>().color -= new Color(0, 0, 0, 0.5f);
        }

        if (_lineRenderer == null) {
            _lineRenderer = Instantiate(LineRendererPrefab.gameObject, FishingRodTip, Quaternion.identity).GetComponent<LineRenderer>();
            _lineRenderer.startWidth = 0.04f;
        }
    }

    private void UpdatePreview() {
        Vector3 castPosition = FishingRodTip + PlayerMovementController.LocalInstance.LastMotionDirection * _currentCastingDistance;
        _bobber.transform.position = castPosition;

        // Zeichnet die Parabel
        Vector3[] linePositions = new Vector3[_segmentCount];
        for (int i = 0; i < _segmentCount; i++) {
            float t = (float)i / (_segmentCount - 1);
            linePositions[i] = CalculateArcPoint(t, FishingRodTip, castPosition, _castArcHeight);
        }
        _lineRenderer.positionCount = _segmentCount;
        _lineRenderer.SetPositions(linePositions);
    }

    private void StopPreview() {
        _bobber.GetComponent<SpriteRenderer>().color += new Color(0, 0, 0, 0.5f);
    }


    private IEnumerator CastLine() {
        _isFishing = true;

        // Position berechnen, wo die Boje landen soll
        Vector3 castPosition = FishingRodTip + PlayerMovementController.LocalInstance.LastMotionDirection * _currentCastingDistance;

        // Bobber entlang der Parabel bewegen
        Vector3[] arcPositions = new Vector3[_segmentCount];
        for (int i = 0; i < _segmentCount; i++) {
            float t = (float)i / (_segmentCount - 1);
            arcPositions[i] = CalculateArcPoint(t, FishingRodTip, castPosition, _castArcHeight);
            _bobber.transform.position = arcPositions[i];

            // Angelschnur zeichnen
            DrawFishingLine(FishingRodTip, _bobber.transform.position, _segmentCount);

            yield return null; // wartet einen Frame
        }


        TileBase castTile = _fishingTilemap.GetTile(_fishingTilemap.WorldToCell(castPosition));
        if (castTile == null) {
            Debug.Log("Das Feld ist kein Fangbereich.");
            ReelInWithoutCatch();
            yield break;
        }

        // Sound abspielen, wenn der Bobber die Zielstelle erreicht
        // AudioSource.PlayClipAtPoint(fishSplashSound, currentBobber.transform.position);

        // Warten, bis ein Fisch anbeißt
        float timeToBite = Random.Range(_timeToBiteMin, _timeToBiteMax);
        yield return new WaitForSeconds(timeToBite);

        if (!_tileNameToIdMap.TryGetValue(castTile.name, out int tileId)) {
            tileId = -1;
            Debug.LogError("Feld hat einen falschen Namen.");
        }

        _fish = FishDatabaseSO.GetFish(tileId, FishSO.CatchingMethod.FishingRod);

        // Simuliere einen Fischbiss
        _fishBiting = true;

        Debug.Log("Ein Fisch hat angebissen! Drücke SPACE, um ihn einzuholen.");

        // Minigame maybe like animal crossing
    }

    private void ReelInWithoutCatch() {
        Debug.Log("Du hast die Angel eingeholt, ohne etwas zu fangen.");
        ResetVariables();
    }

    private void ReelInWithCatch() {
        if (_fishBiting) {
            Debug.Log(_fish.CatchText[Random.Range(0, _fish.CatchText.Length - 1)]);
            Debug.Log($"Der Fisch ist {_fish.CalculateFishSize()} cm lang.");
            ResetVariables();
        }
    }

    private Vector3 CalculateArcPoint(float t, Vector3 start, Vector3 end, float height) {
        Vector3 point = Vector3.Lerp(start, end, t);
        point.y += Mathf.Sin(t * Mathf.PI) * height;
        return point;
    }

    private void DrawFishingLine(Vector3 start, Vector3 end, int segmentCount) {
        Vector3[] linePositions = new Vector3[segmentCount];
        for (int i = 0; i < segmentCount; i++) {
            float t = (float)i / (segmentCount - 1);
            linePositions[i] = CalculateSagPoint(t, start, end, _lineSagHeight);
        }
        _lineRenderer.positionCount = segmentCount;
        _lineRenderer.SetPositions(linePositions);
    }

    private Vector3 CalculateSagPoint(float t, Vector3 start, Vector3 end, float sagHeight) {
        Vector3 point = Vector3.Lerp(start, end, t);
        float sag = Mathf.Sin(t * Mathf.PI) * sagHeight;
        point.y -= sag;
        return point;
    }

    private void ResetVariables() {
        Destroy(_bobber);
        Destroy(_lineRenderer.gameObject);
        _isFishing = false;
        _fishBiting = false;
        _currentCastingDistance = 0f;
        _isCasting = false;
        _fish = null;
    }
}
