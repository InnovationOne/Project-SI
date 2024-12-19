using Ink.Parsed;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static FishSO;

[Serializable]
public struct FishButtonPressRange {
    public int MinPresses;
    public int MaxPresses;

    public FishButtonPressRange(int min, int max) {
        MinPresses = min;
        MaxPresses = max;
    }
}

/// <summary>
/// Controls the player's fishing mechanics, including casting, waiting for fish bites,
/// and reeling in fish through a button press minigame.
/// </summary>
public class PlayerFishingController : MonoBehaviour {
    // Prefab and configuration references assigned via the inspector
    [Header("Fishing Setup")]
    [SerializeField] GameObject _bobberPrefab;
    [SerializeField] LineRenderer _lineRendererPrefab;
    [SerializeField] FishDatabaseSO _fishDatabaseSO;
    [SerializeField] FishingRodToolSO _fishingRod;

    [Header("UI Elements")]
    [SerializeField] Image _fishCatchText;
    [SerializeField] SpriteRenderer _alertPopup;

    [Header("Visual Settings")]
    [SerializeField, Range(0f, 1f)] float _bobberAlphaAdjustment = 0.5f;

    // Fishing system constants
    const string FISHING_TILEMAP_TAG = "FishingTilemap";
    const string CATCH_TEXT_TAG = "CatchText";
    const float MAX_ROD_CASTING_DISTANCE = 2.5f;
    const float CASTING_SPEED = 1.8f;
    const float LINE_RENDER_WIDTH = 0.04f;
    const float MIN_TIME_TO_BITE = 5f;
    const float MAX_TIME_TO_BITE = 15f;
    const float CAST_ARC_HEIGHT = 1.5f;
    const float LINE_SAG_HEIGHT = 0.1f;
    const int SEGMENT_COUNT = 20;
    const float CATCH_TEXT_SHOW_TIME = 5f;
    const float UI_FADE_DURATION = 0.2f;
    const float TIME_TO_CATCH_FISH = 4.5f;
    const float TIME_TO_START_MINIGAME = 0.8f;
    const float COOLDOWN_TO_FISH_AGAIN = 0.8f;

    enum TileType {
        Invalid = -1,
        Coast = 0,
        Sea = 1,
        DeepSea = 2,
        River = 3,
        Lake = 4
    }

    enum FishingState {
        Idle,
        Casting,
        Fishing,
        ReelingIn
    }

    // Predefined ranges of button presses required per fish size
    static readonly FishButtonPressRange[] _pressRanges = new FishButtonPressRange[] {
        new(3, 5),    // VerySmall
        new(5, 8),    // Small
        new(8, 12),   // Medium
        new(12, 16),  // Large
        new(16, 20),  // VeryLarge
        new(20, 30)   // Leviathan
    };

    // Internal fields for managing the fishing process
    GameObject _bobberInstance;
    LineRenderer _lineRenderer;
    SpriteRenderer _bobberSpriteRenderer;
    TextMeshProUGUI _catchTextTMP;
    FishingState _currentState = FishingState.Idle;
    Vector3 _fishingRodTip;
    float _currentCastingDistance = 0f;
    bool _fishIsBiting = false;
    FishSO _currentFish;
    int _bobberTileId = -1;
    float _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
    int _requiredButtonPresses = 0;
    int _currentButtonPresses = 0;
    float _timeToCatchFish = TIME_TO_CATCH_FISH;
    float _currentCooldown = 0f;
    bool _isLeftClickHeld = false;

    // References
    PlayerController _playerController;
    PlayerToolbeltController _playerToolbeltController;
    PlayerMovementController _playerMovementController;
    PlayerInventoryController _playerInventoryController;
    Tilemap _fishingTilemap;
    AudioManager _audioManager;
    InputManager _inputManager;

    // Coroutine references
    Coroutine _castLineCoroutine;
    Coroutine _waitForFishCoroutine;

    // Preallocated array to reduce allocations each frame
    readonly Vector3[] _linePositionsBuffer = new Vector3[SEGMENT_COUNT];

    void Awake() {
        _playerController = GetComponent<PlayerController>();
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerInventoryController = GetComponent<PlayerInventoryController>();
        _alertPopup.enabled = false;
    }

    void Start() {
        // Retrieve the fishing tilemap reference
        _fishingTilemap = GameObject.FindGameObjectWithTag(FISHING_TILEMAP_TAG).GetComponent<Tilemap>();
        _audioManager = AudioManager.Instance;
        _inputManager = InputManager.Instance;
        _inputManager.OnLeftClickAction += OnLeftClickAction;
        _inputManager.OnLeftClickStarted += OnLeftClickStarted;
        _inputManager.OnLeftClickCanceled += OnLeftClickCanceled;

        // Initialize fish data from the database
        _fishDatabaseSO.InitializeFishData();

        // Retrieve the catch text UI element
        GameObject catchTextGO = GameObject.FindGameObjectWithTag(CATCH_TEXT_TAG);
        if (catchTextGO.TryGetComponent(out Image fishCatchImage)) {
            _fishCatchText = fishCatchImage;
            _catchTextTMP = fishCatchImage.GetComponentInChildren<TextMeshProUGUI>();
            _fishCatchText.gameObject.SetActive(false);
        }
    }

    void OnDestroy() {
        _inputManager.OnLeftClickAction -= OnLeftClickAction;
        _inputManager.OnLeftClickStarted -= OnLeftClickStarted;
        _inputManager.OnLeftClickCanceled -= OnLeftClickCanceled;
    }

    void Update() {
        // Prevent fishing logic if the currently selected tool is not the fishing rod
        if (_fishingRod.ItemId != _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId) return;

        // Handle cooldown
        if (_currentCooldown > 0) {
            _currentCooldown -= Time.deltaTime;
            return;
        }

        // Update the fishing rod tip position relative to player position
        _fishingRodTip = transform.position;

        // Manage fish biting timer
        if (_fishIsBiting && _currentState == FishingState.Fishing) {
            _currentTimeToStartMinigame -= Time.deltaTime;
            if (_currentTimeToStartMinigame < 0) {
                ResetBitingState();
            }
        }

        // Handle states
        switch (_currentState) {
            case FishingState.Casting:
                if (_isLeftClickHeld) {
                    _currentCastingDistance += CASTING_SPEED * Time.deltaTime;
                    _currentCastingDistance = Mathf.Clamp(_currentCastingDistance, 0, MAX_ROD_CASTING_DISTANCE);
                    UpdatePreviewThrowArc();
                }
                break;
            case FishingState.ReelingIn:
                _timeToCatchFish -= Time.deltaTime;
                if (_timeToCatchFish <= 0) {
                    ResetMinigame();
                }
                break;
        }
    }

    #region -------------------- Input Handlers --------------------
    // Input handlers focusing on state transitions rather than logic
    void OnLeftClickAction() {
        if (_currentCooldown > 0 || _fishingRod.ItemId != _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId) return;

        switch (_currentState) {
            case FishingState.Idle:
                _playerController.PlayerMovementController.SetCanMoveAndTurn(false);
                StartCastingPreview();
                break;

            case FishingState.Fishing:
                HandleFishingReel();
                break;

            case FishingState.ReelingIn:
                ProcessMinigamePress();
                break;
        }
    }

    void OnLeftClickStarted() {
        if (_currentCooldown > 0) return;
        _isLeftClickHeld = true;
    }

    void OnLeftClickCanceled() {
        if (_currentCooldown > 0 || _currentState != FishingState.Casting) return;
        _isLeftClickHeld = false;
        StopPreviewThrowArc();
        _currentState = FishingState.Fishing;
        _castLineCoroutine ??= StartCoroutine(CastLine());

    }
    #endregion -------------------- Input Handlers --------------------

    #region -------------------- State Handlers --------------------
    // State-specific handlers
    void StartCastingPreview() {
        _currentState = FishingState.Casting;
        ShowPreview();
    }

    // Handle when player tries to reel in during the Fishing state (with or without fish)
    void HandleFishingReel() {
        if (_fishIsBiting && _currentTimeToStartMinigame >= 0) {
            StartMinigame();
        } else {
            ReelInWithoutCatch();
        }
    }

    void ShowPreview() {
        if (_bobberInstance == null) {
            _bobberInstance = Instantiate(_bobberPrefab, _fishingRodTip, Quaternion.identity);
            if (_bobberInstance.TryGetComponent(out _bobberSpriteRenderer)) {
                var c = _bobberSpriteRenderer.color;
                _bobberSpriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a - _bobberAlphaAdjustment));
            }
        }

        if (_lineRenderer == null) {
            if (Instantiate(_lineRendererPrefab.gameObject, _fishingRodTip, Quaternion.identity).TryGetComponent(out _lineRenderer)) {
                _lineRenderer.startWidth = LINE_RENDER_WIDTH;
            }
        }
    }

    void UpdatePreviewThrowArc() {
        Vector3 castPos = GetCastPostion();
        _bobberInstance.transform.position = castPos;

        for (int i = 0; i < SEGMENT_COUNT; i++) {
            float t = i / (float)(SEGMENT_COUNT - 1);
            _linePositionsBuffer[i] = CalculateArcPoint(t, _fishingRodTip, castPos, CAST_ARC_HEIGHT);
        }

        _lineRenderer.positionCount = SEGMENT_COUNT;
        _lineRenderer.SetPositions(_linePositionsBuffer);
    }

    void StopPreviewThrowArc() {
        var c = _bobberSpriteRenderer.color;
        _bobberSpriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a + _bobberAlphaAdjustment));
    }

    IEnumerator CastLine() {
        Vector3 castPos = GetCastPostion();

        // Animate the bobber moving along the casting arc
        for (int i = 0; i < SEGMENT_COUNT; i++) {
            float t = i / (float)(SEGMENT_COUNT - 1);
            Vector3 arcPoint = CalculateArcPoint(t, _fishingRodTip, castPos, CAST_ARC_HEIGHT);
            _bobberInstance.transform.position = arcPoint;
            DrawFishingLine(_fishingRodTip, _bobberInstance.transform.position, SEGMENT_COUNT);
            yield return null;
        }

        var tile = _fishingTilemap.GetTile(_fishingTilemap.WorldToCell(castPos));
        if (tile == null) {
            Debug.Log("The landed tile is not a valid fishing position.");
            ReelInWithoutCatch();
            yield break;
        }

        // Retrieve the tile type based on its name using the TileType enum
        if (!Enum.TryParse(tile.name, out TileType tileType)) {
            Debug.LogError($"Landed tile '{tile.name}' has an invalid name.");
            tileType = TileType.Invalid;
        }
        _bobberTileId = (int)tileType;

        _audioManager.PlayOneShot(FMODEvents.Instance.WaterDropSFX, transform.position);
        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
        _castLineCoroutine = null;
    }

    Vector3 GetCastPostion() => _fishingRodTip + (Vector3)_playerMovementController.LastMotionDirection.normalized * _currentCastingDistance;
    #endregion -------------------- State Handlers --------------------

    #region -------------------- Fishing --------------------
    IEnumerator WaitForFish() {
        float biteRateAdjustment = 1f;
        var rarityId = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().RarityId - 1;
        if (rarityId >= 0 && rarityId < _fishingRod.BiteRate.Length) {
            biteRateAdjustment = 1 - (_fishingRod.BiteRate[rarityId] / 100f);
        }

        float timeToBite = UnityEngine.Random.Range(MIN_TIME_TO_BITE, MAX_TIME_TO_BITE) * biteRateAdjustment;
        yield return new WaitForSeconds(timeToBite);

        _currentFish = _fishDatabaseSO.GetFish(_fishingRod, _bobberTileId, CatchingMethod.FishingRod);
        if (_currentFish == null) {
            Debug.LogError($"No fish found for tileId {_bobberTileId} using FishingRod method.");
            ReelInWithoutCatch();
            yield break;
        }

        _fishIsBiting = true;
        _audioManager.PlayOneShot(FMODEvents.Instance.FishBitSFX, transform.position);
        _alertPopup.enabled = true;
        _waitForFishCoroutine = null;
    }

    void ReelInWithoutCatch() => ResetVariables();

    void StartMinigame() {
        _alertPopup.enabled = false;
        _currentState = FishingState.ReelingIn;
        _timeToCatchFish = TIME_TO_CATCH_FISH;

        var sizeIndex = (int)_currentFish.FishSize;
        sizeIndex = Mathf.Clamp(sizeIndex, 0, _pressRanges.Length - 1);
        _requiredButtonPresses = UnityEngine.Random.Range(_pressRanges[sizeIndex].MinPresses, _pressRanges[sizeIndex].MaxPresses + 1);
    }

    void ProcessMinigamePress() {
        _currentButtonPresses++;
        if (_currentButtonPresses < _requiredButtonPresses) return;

        _currentState = FishingState.Fishing;
        StartCoroutine(DisplayCatchMessage(
            $"You caught a {_currentFish.FishItem.ItemName}. It is {_currentFish.CalculateFishSize()} cm long.\n" +
            $"{_currentFish.CatchText[UnityEngine.Random.Range(0, _currentFish.CatchText.Length)]}"));

        _playerInventoryController.InventoryContainer.AddItem(new ItemSlot(_currentFish.FishItem.ItemId, 1, 0), false);
        _currentCooldown = COOLDOWN_TO_FISH_AGAIN;
        ResetVariables();
    }

    void ResetMinigame() {
        _currentButtonPresses = 0;
        _currentFish = null;
        _fishIsBiting = false;
        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
        _currentState = FishingState.Fishing;
        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
    }
    #endregion -------------------- Fishing --------------------

    #region -------------------- Utility --------------------
    Vector3 CalculateArcPoint(float t, Vector3 start, Vector3 end, float height) => Vector3.Lerp(start, end, t) + height * Mathf.Sin(t * Mathf.PI) * Vector3.up;

    Vector3 CalculateSagPoint(float t, Vector3 start, Vector3 end, float sagHeight) => Vector3.Lerp(start, end, t) - Mathf.Sin(t * Mathf.PI) * sagHeight * Vector3.up;

    void DrawFishingLine(Vector3 start, Vector3 end, int segmentCount) {
        for (int i = 0; i < segmentCount; i++) {
            float t = i / (float)(segmentCount - 1);
            _linePositionsBuffer[i] = CalculateSagPoint(t, start, end, LINE_SAG_HEIGHT);
        }
        _lineRenderer.positionCount = segmentCount;
        _lineRenderer.SetPositions(_linePositionsBuffer);
    }

    private IEnumerator DisplayCatchMessage(string text) {
        _fishCatchText.gameObject.SetActive(true);
        _catchTextTMP.text = text;
        _fishCatchText.canvasRenderer.SetAlpha(0f);
        _fishCatchText.CrossFadeAlpha(1f, UI_FADE_DURATION, false);
        yield return new WaitForSeconds(CATCH_TEXT_SHOW_TIME);
        _fishCatchText.CrossFadeAlpha(0f, UI_FADE_DURATION, false);
        yield return new WaitForSeconds(UI_FADE_DURATION);
        _fishCatchText.gameObject.SetActive(false);
    }

    void ResetVariables() {
        if (_bobberInstance != null) {
            Destroy(_bobberInstance);
            _bobberInstance = null;
            _bobberSpriteRenderer = null;
        }

        if (_lineRenderer != null) {
            Destroy(_lineRenderer.gameObject);
            _lineRenderer = null;
        }

        _fishIsBiting = false;
        _currentCastingDistance = 0f;
        _currentFish = null;
        _timeToCatchFish = TIME_TO_CATCH_FISH;
        _currentButtonPresses = 0;
        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
        _bobberTileId = -1;
        _currentState = FishingState.Idle;

        if (_castLineCoroutine != null) {
            StopCoroutine(_castLineCoroutine);
            _castLineCoroutine = null;
        }

        if (_waitForFishCoroutine != null) {
            StopCoroutine(_waitForFishCoroutine);
            _waitForFishCoroutine = null;
        }

        _playerMovementController.SetCanMoveAndTurn(true);
    }

    private void ResetBitingState() {
        _fishIsBiting = false;
        _alertPopup.enabled = false;
        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
    }
    #endregion -------------------- Utility --------------------
}
