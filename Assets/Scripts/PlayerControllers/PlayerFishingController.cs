using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
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
    [SerializeField] Animator _weaponAnim;

    [Header("Animation Settings")]
    [SerializeField] private string bobberIdleAnimation = "BobberIdle";
    [SerializeField] private string bobberActionAnimation = "BobberAction";
    [SerializeField] private float bobberRotationSpeed = 360f; // Grad pro Sekunde
    [SerializeField] private float bobberRotationRadius = 0.2f;  // Radius des Kreises
    private Vector3 _bobberCenterPosition;


    [Header("UI Elements")]
    [SerializeField] SpriteRenderer _alertPopup;

    [Header("Visual Settings")]
    [SerializeField, Range(0f, 1f)] float _bobberAlphaAdjustment = 0.5f;

    [Header("Lateral Offset Settings")]
    [SerializeField, Tooltip("Maximum lateral offset in tiles")]
    float _maxLateralOffset = 0.5f;

    [SerializeField, Tooltip("Rate (in tiles per second) at which lateral offset increases")]
    float _lateralOffsetRate = 0.1f;
    float _currentLateralOffset = 0f;

    // Fishing system constants
    const string FISHING_TILEMAP_TAG = "FishingTilemap";
    const float MAX_ROD_CASTING_DISTANCE = 2.5f;
    const float CASTING_SPEED = 1.8f;
    const float LINE_RENDER_WIDTH = 0.04f;
    const float MIN_TIME_TO_BITE = 5f;
    const float MAX_TIME_TO_BITE = 15f;
    const float CAST_ARC_HEIGHT = 1.5f;
    const float LINE_SAG_HEIGHT = 0.1f;
    const int SEGMENT_COUNT = 20;
    const float TIME_TO_CATCH_FISH = 4.5f;
    const float TIME_TO_START_MINIGAME = 0.8f;
    const float COOLDOWN_TO_FISH_AGAIN = 0.8f;
    const float MAX_OFFSET_DISTANCE = 1.0f;

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
    PlayerToolbeltController _playerToolbeltController;
    PlayerMovementController _playerMovementController;
    PlayerInventoryController _playerInventoryController;
    PlayerAnimationController _playerAnimationController;
    Tilemap _fishingTilemap;
    AudioManager _audioManager;
    InputManager _inputManager;    

    // Coroutine references
    Coroutine _castLineCoroutine;
    Coroutine _waitForFishCoroutine;

    // Preallocated array to reduce allocations each frame
    readonly Vector3[] _linePositionsBuffer = new Vector3[SEGMENT_COUNT];

    void Awake() {
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerInventoryController = GetComponent<PlayerInventoryController>();
        _playerAnimationController = GetComponent<PlayerAnimationController>();
        _alertPopup.enabled = false;
    }

    void Start() {
        // Retrieve the fishing tilemap reference
        _fishingTilemap = GameObject.FindGameObjectWithTag(FISHING_TILEMAP_TAG).GetComponent<Tilemap>();
        _audioManager = GameManager.Instance.AudioManager;
        _inputManager = GameManager.Instance.InputManager;
        _inputManager.OnLeftClickAction += OnLeftClickAction;
        _inputManager.OnLeftClickStarted += OnLeftClickStarted;
        _inputManager.OnLeftClickCanceled += OnLeftClickCanceled;

        // Initialize fish data from the database
        _fishDatabaseSO.InitializeFishData();
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

                    // Update lateral offset based on input
                    Vector2 offsetInput = _inputManager.GetMovementVectorNormalized(); // WASD input
                    Vector2 throwDir = _playerMovementController.LastMotionDirection.normalized;
                    if (throwDir == Vector2.zero) {
                        throwDir = Vector2.up; // Fallback direction if needed
                    }
                    // Get the perpendicular axis (only lateral movement)
                    Vector2 perpAxis = new Vector2(-throwDir.y, throwDir.x);
                    // Determine target offset (dot product gives sign and magnitude, clamped between -1 and 1)
                    float targetOffset = 0f;
                    if (offsetInput.sqrMagnitude > 0.01f) {
                        float dot = Vector2.Dot(offsetInput, perpAxis);
                        targetOffset = Mathf.Clamp(dot, -1f, 1f) * _maxLateralOffset;
                    }
                    // Gradually move the current lateral offset toward the target offset
                    _currentLateralOffset = Mathf.MoveTowards(_currentLateralOffset, targetOffset, _lateralOffsetRate * Time.deltaTime);

                    UpdatePreviewThrowArc();
                }
                break;
            case FishingState.ReelingIn:
                if (_bobberInstance != null) {
                    float angle = bobberRotationSpeed * Time.time;
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * bobberRotationRadius;
                    _bobberInstance.transform.position = _bobberCenterPosition + offset;
                }

                _timeToCatchFish -= Time.deltaTime;
                if (_timeToCatchFish <= 0) {
                    ResetMinigame();
                }
                break;
        }
    }

    #region -------------------- Animation Handling --------------------

    private void SetFishingState(FishingState newState) {
        _currentState = newState;
        SwitchAnimation(newState);
    }

    private void SwitchAnimation(FishingState st) {
        if (_playerAnimationController == null) return;

        switch (st) {
            case FishingState.Idle:
                _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.Idle, true);
                break;
            case FishingState.Casting:
                _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.FishingHold, true);
                break;
            case FishingState.Fishing:
                StartCoroutine(PlayFishingThrowAnimation());
                break;
            case FishingState.ReelingIn:
                _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.FishingReelLoop, true);
                break;
        }
    }

    IEnumerator PlayFishingThrowAnimation() {
        _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.FishingThrow, true);

        var animInfo = _weaponAnim.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animInfo.length / animInfo.speed);

        _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.FishingReelLoop, true);
    }

    #endregion

    #region -------------------- Input Handlers --------------------
    // Input handlers focusing on state transitions rather than logic
    void OnLeftClickAction() {
        if (_currentCooldown > 0 || _fishingRod.ItemId != _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId) return;

        switch (_currentState) {
            case FishingState.Idle:
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
        SetFishingState(FishingState.Fishing);
        _castLineCoroutine ??= StartCoroutine(CastLine());

    }
    #endregion -------------------- Input Handlers --------------------

    #region -------------------- State Handlers --------------------
    // State-specific handlers
    void StartCastingPreview() {
        _inputManager.BlockPlayerActions(true);
        SetFishingState(FishingState.Casting);
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
        Vector3 castPos = GetCastPosition();
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
        Vector3 castPos = GetCastPosition();
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
            ReelInWithoutCatch();
            yield break;
        }

        // Retrieve the tile type based on its name using the TileType enum
        if (!Enum.TryParse(tile.name, out TileType tileType)) {
            Debug.LogError($"CastLine: Landed tile '{tile.name}' has an invalid name.");
            tileType = TileType.Invalid;
        }
        _bobberTileId = (int)tileType;

        _audioManager.PlayOneShot(GameManager.Instance.FMODEvents.Fishing_Water_Drop, transform.position);

        // Setze den Zentrumspunkt des Bobbers und spiele die Idle-Animation
        _bobberCenterPosition = _bobberInstance.transform.position;
        var bobberAnim = _bobberInstance.GetComponent<Animator>();
        if (bobberAnim != null) {
            bobberAnim.Play(bobberIdleAnimation);
        }

        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
        _castLineCoroutine = null;
    }

    #endregion -------------------- State Handlers --------------------

    #region -------------------- Fishing --------------------
    IEnumerator WaitForFish() {
        float biteRateAdjustment = 1f;
        var rarityId = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().RarityId - 1;
        if (rarityId >= 0 && rarityId < _fishingRod.BiteRate.Length) {
            biteRateAdjustment = 1 - (_fishingRod.BiteRate[rarityId] / 100f);
        }

        float timeToBite = UnityEngine.Random.Range(MIN_TIME_TO_BITE, MAX_TIME_TO_BITE) * biteRateAdjustment;
        _audioManager.PlayOneShot(GameManager.Instance.FMODEvents.Fishing_Reel_Backwards, transform.position);
        yield return new WaitForSeconds(timeToBite);

        _currentFish = _fishDatabaseSO.GetFish(_fishingRod, _bobberTileId, CatchingMethod.FishingRod);
        if (_currentFish == null) {
            Debug.LogError($"WaitForFish: No fish found for tileId {_bobberTileId} using FishingRod method.");
            ReelInWithoutCatch();
            yield break;
        }

        _fishIsBiting = true;
        //TODO: _audioManager.PlayOneShot(GameManager.Instance.FMODEvents.FishBitSFX, transform.position);
        _alertPopup.enabled = true;
        _waitForFishCoroutine = null;
    }

    void ReelInWithoutCatch() {
        StartCoroutine(EndFishingWithAnimation(false));
    }

    void StartMinigame() {
        _alertPopup.enabled = false;
        SetFishingState(FishingState.ReelingIn);
        _timeToCatchFish = TIME_TO_CATCH_FISH;

        _audioManager.PlayLoopingSound(GameManager.Instance.FMODEvents.Fishing_Quickly_Reel_In, transform.position);

        // Spielt die Bobber Action Animation ab
        var bobberAnim = _bobberInstance.GetComponent<Animator>();
        if (bobberAnim != null) {
            bobberAnim.Play(bobberActionAnimation);
        }

        var sizeIndex = (int)_currentFish.FishSize;
        sizeIndex = Mathf.Clamp(sizeIndex, 0, _pressRanges.Length - 1);
        _requiredButtonPresses = UnityEngine.Random.Range(_pressRanges[sizeIndex].MinPresses, _pressRanges[sizeIndex].MaxPresses + 1);
        _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.FishingReelLoop, true);
    }

    void ProcessMinigamePress() {
        _currentButtonPresses++;
        if (_currentButtonPresses < _requiredButtonPresses) return;

        StartCoroutine(EndFishingWithAnimation(true));
    }

    IEnumerator EndFishingWithAnimation(bool caughtFish) {
        _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.FishingLand, true);
        _audioManager.StopSound(GameManager.Instance.FMODEvents.Fishing_Quickly_Reel_In);

        var animInfo = _weaponAnim.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animInfo.length / animInfo.speed);

        if (caughtFish) {
            string catchMessage = $"You caught a {_currentFish.FishItem.ItemName}. It is {_currentFish.CalculateFishSize()} cm long.\n" +
                                  $"{_currentFish.CatchText[UnityEngine.Random.Range(0, _currentFish.CatchText.Length)]}";
            UIManager.Instance.FishCatchUI.ShowFishCatchUI(catchMessage);

            _playerInventoryController.InventoryContainer.AddItem(new ItemSlot(_currentFish.FishItem.ItemId, 1, 0), false);
        }

        _currentCooldown = COOLDOWN_TO_FISH_AGAIN;
        ResetVariables();
    }

    void ResetMinigame() {
        _currentButtonPresses = 0;
        _currentFish = null;
        _fishIsBiting = false;
        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
        SetFishingState(FishingState.Fishing);

        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
    }

    void ResetVariables() {
        _inputManager.BlockPlayerActions(false);
        _audioManager.StopSound(GameManager.Instance.FMODEvents.Fishing_Quickly_Reel_In);

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
        SetFishingState(FishingState.Idle);

        if (_castLineCoroutine != null) {
            StopCoroutine(_castLineCoroutine);
            _castLineCoroutine = null;
        }

        if (_waitForFishCoroutine != null) {
            StopCoroutine(_waitForFishCoroutine);
            _waitForFishCoroutine = null;
        }
    }

    private void ResetBitingState() {
        _fishIsBiting = false;
        _alertPopup.enabled = false;
        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
    }
    #endregion -------------------- Fishing --------------------

    #region -------------------- Utility --------------------
    Vector3 CalculateArcPoint(float t, Vector3 start, Vector3 end, float height) {
        Vector3 arcPoint = Vector3.Lerp(start, end, t) + height * Mathf.Sin(t * Mathf.PI) * Vector3.up;
        return arcPoint;
    }

    Vector3 CalculateSagPoint(float t, Vector3 start, Vector3 end, float sagHeight) {
        Vector3 sagPoint = Vector3.Lerp(start, end, t) - Mathf.Sin(t * Mathf.PI) * sagHeight * Vector3.up;
        return sagPoint;
    }

    void DrawFishingLine(Vector3 start, Vector3 end, int segmentCount) {
        for (int i = 0; i < segmentCount; i++) {
            float t = i / (float)(segmentCount - 1);
            _linePositionsBuffer[i] = CalculateSagPoint(t, start, end, LINE_SAG_HEIGHT);
        }
        _lineRenderer.positionCount = segmentCount;
        _lineRenderer.SetPositions(_linePositionsBuffer);
    }

    Vector3 GetCastPosition() {
        Vector2 direction = _playerMovementController.LastMotionDirection.normalized;
        if (direction == Vector2.zero)
            direction = Vector2.up; // Fallback if no direction is set
        // Compute the final cast position without lateral offset (this version doesn't include lateral offset logic)
        Vector3 finalPos = _fishingRodTip + (Vector3)direction * _currentCastingDistance;
        return finalPos;
    }
    #endregion
}
