using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static FishSO;

/// <summary>
/// Controls the player's fishing mechanics, including casting, waiting for fish bites,
/// and reeling in fish through a button press minigame.
/// </summary>
public class PlayerFishingController : MonoBehaviour {

    #region -------------------- Inspector Fields --------------------

    // Prefab and configuration references assigned via the inspector
    [Header("Fishing Setup")]
    [SerializeField] GameObject _bobberPrefab;
    [SerializeField] LineRenderer _lineRendererPrefab;
    [SerializeField] FishDatabaseSO _fishDatabaseSO;
    [SerializeField] FishingRodToolSO _fishingRod;
    [SerializeField] Animator _weaponAnim;

    [Header("Animation Settings")]
    const string bobberIdleAnimation = "BobberIdle";
    const string bobberActionAnimation = "BobberAction";
    const float bobberRotationSpeed = 360f; // degrees per second
    const float bobberRotationRadius = 0.15f;  // rotation radius
    Vector3 _bobberCenterPosition;

    [Header("UI Elements")]
    [SerializeField] SpriteRenderer _alertPopup;

    [Header("Visual Settings")]
    [SerializeField, Range(0f, 1f)] float _bobberAlphaAdjustment = 0.5f;

    [Header("Lateral Offset Settings")]
    [SerializeField, Tooltip("Maximum lateral offset in tiles")]
    float _maxLateralOffset = 0.5f;
    [SerializeField, Tooltip("Rate (in tiles per second) at which lateral offset increases")]
    float _lateralOffsetRate = 0.1f;

    #endregion -------------------- Inspector Fields --------------------

    #region -------------------- Constants & Private Fields --------------------

    // Fishing system constants
    const float MAX_ROD_CASTING_DISTANCE = 5f;
    const float CASTING_SPEED = 1.8f;
    const float LINE_RENDER_WIDTH = 0.04f;
    const float MIN_TIME_TO_BITE = 5f;
    const float MAX_TIME_TO_BITE = 15f;
    const float CAST_ARC_HEIGHT = 1.2f;
    const float LINE_SAG_HEIGHT = 0.1f;
    const int SEGMENT_COUNT = 40;
    const float TIME_TO_CATCH_FISH = 4.5f;
    const float TIME_TO_START_MINIGAME = 0.8f;
    const float COOLDOWN_TO_FISH_AGAIN = 0.8f;
    const float MAX_OFFSET_DISTANCE = 1.0f;

    // Animation offset arrays
    readonly Vector2[] _fishingHoldAnimationPositionsLeft = new Vector2[] { new(-1.4692f, 1.1874f), new(1.6876f, 1.0937f) };
    readonly Vector2[] _fishingThrowAnimationPositionsLeft = new Vector2[] { new(0.5625f, 1.0313f), new(-1.9373f, 1.1872f) };
    readonly Vector2[] _fishingReelLoopAnimationPositionsLeft = new Vector2[] { new(-1.87503f, 0.90627f), new(-1.87503f, 0.90627f), new(-1.87503f, 0.90627f), new(-1.9062f, 0.7812f) };
    readonly Vector2[] _fishingLandAnimationPositionsLeft = new Vector2[] { new(-1.9062f, 0.9064f), new(-0.7185f, 1.6252f), new(-0.5935f, 1.5622f), new(1.6876f, 1.0937f) };

    readonly Vector2[] _fishingHoldAnimationPositionsDown = new Vector2[] { new(0.1564f, 2.0625f), new(-1.0623f, 1.719f) };
    readonly Vector2[] _fishingThrowAnimationPositionsDown = new Vector2[] { new(-0.6565f, 1.6249f), new(0.906f, 0.8434f) };
    readonly Vector2[] _fishingReelLoopAnimationPositionsDown = new Vector2[] { new(0.8749f, 0.8126f), new(0.8752f, 0.8437f), new(0.8752f, 0.8437f), new(0.8748f, 0.8124f) };
    readonly Vector2[] _fishingLandAnimationPositionsDown = new Vector2[] { new(0.6562f, 0.9692f), new(0.2499f, 1.5936f), new(-0.1249f, 1.7186f), new(-1.0623f, 1.719f) };

    readonly Vector2[] _fishingHoldAnimationPositionsRight = new Vector2[] { new(1.4692f, 1.1874f), new(-1.6876f, 1.0937f) };
    readonly Vector2[] _fishingThrowAnimationPositionsRight = new Vector2[] { new(-0.5625f, 1.0313f), new(1.9373f, 1.1872f) };
    readonly Vector2[] _fishingReelLoopAnimationPositionsRight = new Vector2[] { new(1.87503f, 0.90627f), new(1.87503f, 0.90627f), new(1.87503f, 0.90627f), new(1.9062f, 0.7812f) };
    readonly Vector2[] _fishingLandAnimationPositionsRight = new Vector2[] { new(1.9062f, 0.9064f), new(0.7185f, 1.6252f), new(0.5935f, 1.5622f), new(-1.6876f, 1.0937f) };

    readonly Vector2[] _fishingHoldAnimationPositionsUp = new Vector2[] { new(0.6251f, 1.9372f), new(-0.7809f, 1.7501f) };
    readonly Vector2[] _fishingThrowAnimationPositionsUp = new Vector2[] { new(-0.2496f, 1.7813f), new(1.4063f, 1.7188f) };
    readonly Vector2[] _fishingReelLoopAnimationPositionsUp = new Vector2[] { new(1.4063f, 1.5004f), new(1.4063f, 1.5004f), new(1.4063f, 1.5004f), new(1.4063f, 1.6878f) };
    readonly Vector2[] _fishingLandAnimationPositionsUp = new Vector2[] { new(1.2813f, 1.844f), new(0.4064f, 2.0934f), new(-0.1564f, 2f), new(-0.7809f, 1.7501f) };

    // Enums for tile types and fishing states
    enum TileType { Invalid = -1, Coast = 0, Sea = 1, DeepSea = 2, River = 3, Lake = 4 }
    enum FishingState { Idle, Casting, Fishing, ReelingIn }

    // Structure for fishing minigame button press range
    [Serializable]
    public struct FishButtonPressRange {
        public int MinPresses;
        public int MaxPresses;
        public FishButtonPressRange(int min, int max) { MinPresses = min; MaxPresses = max; }
    }
    static readonly FishButtonPressRange[] _pressRanges = new FishButtonPressRange[] {
        new(3, 5), new(5, 8), new(8, 12), new(12, 16), new(16, 20), new(20, 30)
    };

    // Internal fishing process fields
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
    float _currentLateralOffset = 0f;

    // Cached rod tip for fixed line start after casting
    Vector3 _cachedRodTip = Vector3.zero;

    // Frame timing info for fishing animations (FPS and frame counts)
    float _animationStateStartTime = 0f;
    string _currentFishingAnimName = "";
    readonly int _animationFPS = 15;
    readonly Dictionary<string, int> _animationFrameCounts = new()
    {
        { "FishingHold", 2 },
        { "FishingThrow", 2 },
        { "FishingReelLoop", 4 },
        { "FishingLand", 4 }
    };

    // References to other components
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

    // Buffer for LineRenderer positions
    readonly Vector3[] _linePositionsBuffer = new Vector3[SEGMENT_COUNT];

    #endregion -------------------- Constants & Private Fields --------------------

    #region -------------------- Unity Lifecycle --------------------

    void Awake() {
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerInventoryController = GetComponent<PlayerInventoryController>();
        _playerAnimationController = GetComponent<PlayerAnimationController>();
        _alertPopup.enabled = false;
    }

    void Start() {
        _fishingTilemap = GameObject.FindGameObjectWithTag("FishingTilemap").GetComponent<Tilemap>();
        _audioManager = GameManager.Instance.AudioManager;
        _inputManager = GameManager.Instance.InputManager;
        _inputManager.OnLeftClickAction += OnLeftClickAction;
        _inputManager.OnLeftClickStarted += OnLeftClickStarted;
        _inputManager.OnLeftClickCanceled += OnLeftClickCanceled;
        _fishDatabaseSO.InitializeFishData();
    }

    void OnDestroy() {
        _inputManager.OnLeftClickAction -= OnLeftClickAction;
        _inputManager.OnLeftClickStarted -= OnLeftClickStarted;
        _inputManager.OnLeftClickCanceled -= OnLeftClickCanceled;
    }

    void Update() {
        // Process fishing only if the current tool is the fishing rod.
        if (_fishingRod.ItemId != _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId) return;

        if (_currentCooldown > 0) {
            _currentCooldown -= Time.deltaTime;
            return;
        }

        _fishingRodTip = transform.position;

        if (_fishIsBiting && _currentState == FishingState.Fishing) {
            _currentTimeToStartMinigame -= Time.deltaTime;
            if (_currentTimeToStartMinigame < 0)
                ResetBitingState();
        }

        // Handle states
        switch (_currentState) {
            case FishingState.Casting:
                if (_isLeftClickHeld) {
                    _currentCastingDistance += CASTING_SPEED * Time.deltaTime;
                    _currentCastingDistance = Mathf.Clamp(_currentCastingDistance, 0, MAX_ROD_CASTING_DISTANCE);

                    Vector2 offsetInput = _inputManager.GetMovementVectorNormalized();
                    Vector2 throwDir = _playerMovementController.LastMotionDirection.normalized;
                    if (throwDir == Vector2.zero)
                        throwDir = Vector2.up;
                    Vector2 perpAxis = new(-throwDir.y, throwDir.x);
                    float targetOffset = 0f;
                    if (offsetInput.sqrMagnitude > 0.01f) {
                        float dot = Vector2.Dot(offsetInput, perpAxis);
                        targetOffset = Mathf.Clamp(dot, -1f, 1f) * _maxLateralOffset;
                    }
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
                    Vector3 lineStart = GetLineStartPosition();
                    DrawFishingLine(lineStart, _bobberInstance.transform.position, SEGMENT_COUNT);
                }
                _timeToCatchFish -= Time.deltaTime;
                if (_timeToCatchFish <= 0) ResetMinigame();
                break;
        }
    }

    #endregion -------------------- Unity Lifecycle --------------------

    #region -------------------- Animation Handling --------------------

    void SetFishingState(FishingState newState) {
        _currentState = newState;
        _currentFishingAnimName = newState switch {
            FishingState.Casting => "FishingHold",
            FishingState.Fishing => "FishingThrow",
            FishingState.ReelingIn => "FishingReelLoop",
            _ => "",
        };
        SwitchAnimation(newState);
    }

    void SwitchAnimation(FishingState st) {
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

    #endregion -------------------- Animation Handling --------------------

    #region -------------------- Input Handlers --------------------

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

    void HandleFishingReel() {
        if (_fishIsBiting && _currentTimeToStartMinigame >= 0) StartMinigame();
        else ReelInWithoutCatch();
    }

    #endregion -------------------- Input Handlers --------------------

    #region -------------------- Casting & Preview Methods --------------------

    void StartCastingPreview() {
        _inputManager.BlockPlayerActions(true);
        SetFishingState(FishingState.Casting);
        ShowPreview();
    }

    void ShowPreview() {
        if (_bobberInstance == null) {
            _bobberInstance = Instantiate(_bobberPrefab, _fishingRodTip, Quaternion.identity);
            if (_bobberInstance.TryGetComponent(out _bobberSpriteRenderer)) {
                var c = _bobberSpriteRenderer.color;
                _bobberSpriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a - _bobberAlphaAdjustment));
            }
        } else {
            _bobberInstance.transform.position = _fishingRodTip;
            _bobberCenterPosition = _fishingRodTip;
        }
        _cachedRodTip = _fishingRodTip;
        if (_lineRenderer == null) {
            if (Instantiate(_lineRendererPrefab.gameObject, _fishingRodTip, Quaternion.identity).TryGetComponent(out _lineRenderer)) {
                _lineRenderer.startWidth = LINE_RENDER_WIDTH;
            }
        }
    }

    void UpdatePreviewThrowArc() {
        Vector3 castPos = GetCastPosition();
        _bobberInstance.transform.position = castPos;
        Vector3 lineStart = GetLineStartPosition();
        for (int i = 0; i < SEGMENT_COUNT; i++) {
            float t = i / (float)(SEGMENT_COUNT - 1);
            _linePositionsBuffer[i] = CalculateArcPoint(t, lineStart, castPos, CAST_ARC_HEIGHT);
        }
        _lineRenderer.positionCount = SEGMENT_COUNT;
        _lineRenderer.SetPositions(_linePositionsBuffer);
    }

    void StopPreviewThrowArc() {
        if (_bobberSpriteRenderer != null) {
            var c = _bobberSpriteRenderer.color;
            _bobberSpriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a + _bobberAlphaAdjustment));
        }
    }

    IEnumerator CastLine() {
        Vector3 castPos = GetCastPosition();
        Vector3 lineStart = GetLineStartPosition();
        for (int i = 0; i < SEGMENT_COUNT; i++) {
            float t = i / (float)(SEGMENT_COUNT - 1);
            Vector3 arcPoint = CalculateArcPoint(t, lineStart, castPos, CAST_ARC_HEIGHT);
            _bobberInstance.transform.position = arcPoint;
            DrawFishingLine(lineStart, _bobberInstance.transform.position, SEGMENT_COUNT);
            yield return null;
        }

        var tile = _fishingTilemap.GetTile(_fishingTilemap.WorldToCell(castPos));
        if (tile == null) {
            ReelInWithoutCatch();
            yield break;
        }

        if (!Enum.TryParse(tile.name, out TileType tileType)) {
            Debug.LogError($"CastLine: Landed tile '{tile.name}' has an invalid name.");
            tileType = TileType.Invalid;
        }
        _bobberTileId = (int)tileType;

        _audioManager.PlayOneShot(GameManager.Instance.FMODEvents.Fishing_Water_Drop, transform.position);

        _bobberCenterPosition = _bobberInstance.transform.position;
        if (_bobberInstance.TryGetComponent<Animator>(out var bobberAnim)) {
            bobberAnim.Play(bobberIdleAnimation);
        }

        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
        _castLineCoroutine = null;
    }

    #endregion -------------------- Casting & Preview Methods --------------------

    #region -------------------- Fishing & Minigame Methods --------------------

    IEnumerator WaitForFish() {
        float biteRateAdjustment = 1f;
        int rarityId = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().RarityId - 1;
        if (rarityId >= 0 && rarityId < _fishingRod.BiteRate.Length) {
            biteRateAdjustment = 1 - (_fishingRod.BiteRate[rarityId] / 100f);
        }
        float timeToBite = UnityEngine.Random.Range(MIN_TIME_TO_BITE, MAX_TIME_TO_BITE) * biteRateAdjustment;
        _audioManager.PlayLoopingSound(GameManager.Instance.FMODEvents.Fishing_Reel_Backwards, transform.position);
        yield return new WaitForSeconds(timeToBite);
        _currentFish = _fishDatabaseSO.GetFish(_fishingRod, _bobberTileId, CatchingMethod.FishingRod);

        if (_currentFish == null) {
            Debug.LogError($"WaitForFish: No fish found for tileId {_bobberTileId} using FishingRod method.");
            ReelInWithoutCatch();
            yield break;
        }
        Debug.Log($"Fish is biting! {_currentFish.name}");

        _fishIsBiting = true;
        //TODO: _audioManager.PlayOneShot(GameManager.Instance.FMODEvents.FishBitSFX, transform.position);
        _alertPopup.enabled = true;
        _waitForFishCoroutine = null;
    }

    void ReelInWithoutCatch() => StartCoroutine(EndFishingWithAnimation(false));

    void StartMinigame() {
        _alertPopup.enabled = false;
        SetFishingState(FishingState.ReelingIn);
        _timeToCatchFish = TIME_TO_CATCH_FISH;
        if (_bobberInstance.TryGetComponent<Animator>(out var bobberAnim)) {
            bobberAnim.Play(bobberActionAnimation);
        }
        int sizeIndex = (int)_currentFish.FishSize;
        sizeIndex = Mathf.Clamp(sizeIndex, 0, _pressRanges.Length - 1);
        _requiredButtonPresses = UnityEngine.Random.Range(_pressRanges[sizeIndex].MinPresses, _pressRanges[sizeIndex].MaxPresses + 1);
    }

    void ProcessMinigamePress() {
        _currentButtonPresses++;
        if (_currentButtonPresses < _requiredButtonPresses) return;
        StartCoroutine(EndFishingWithAnimation(true));
    }

    IEnumerator EndFishingWithAnimation(bool caughtFish) {
        _playerAnimationController.ChangeState(PlayerAnimationController.PlayerState.FishingLand, true);
        _audioManager.PlayOneShot(GameManager.Instance.FMODEvents.Fishing_Quickly_Reel_In, transform.position);
        _audioManager.StopSound(GameManager.Instance.FMODEvents.Fishing_Reel_Backwards);

        if (caughtFish) {
            string catchMessage = $"You caught a {_currentFish.FishItem.ItemName}. It is {_currentFish.CalculateFishSize()} cm long.\n" +
                                  $"{_currentFish.CatchText[UnityEngine.Random.Range(0, _currentFish.CatchText.Length)]}";
            UIManager.Instance.FishCatchUI.ShowFishCatchUI(catchMessage);
            _playerInventoryController.InventoryContainer.AddItem(new ItemSlot(_currentFish.FishItem.ItemId, 1, 0), false);
        }

        ResetVariables();
        _currentCooldown = COOLDOWN_TO_FISH_AGAIN;
        SetFishingState(FishingState.Idle);
        var animInfo = _weaponAnim.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animInfo.length / animInfo.speed);
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
        if (_castLineCoroutine != null) {
            StopCoroutine(_castLineCoroutine);
            _castLineCoroutine = null;
        }
        if (_waitForFishCoroutine != null) {
            StopCoroutine(_waitForFishCoroutine);
            _waitForFishCoroutine = null;
        }
    }

    void ResetBitingState() {
        _fishIsBiting = false;
        _alertPopup.enabled = false;
        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
    }

    #endregion -------------------- Fishing & Minigame Methods --------------------

    #region -------------------- Utility Methods --------------------

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

    Vector3 GetCastPosition() {
        Vector2 direction = _playerMovementController.LastMotionDirection.normalized;
        if (direction == Vector2.zero) direction = Vector2.up;
        return _fishingRodTip + (Vector3)direction * _currentCastingDistance;
    }


    Vector2 GetFishingLineStartOffset() {
        string animName = _currentFishingAnimName;
        Vector2 direction = _playerMovementController.LastMotionDirection.normalized;
        if (direction == Vector2.zero) direction = Vector2.up;
        string dirKey = (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                        ? (direction.x < 0 ? "Left" : "Right")
                        : (direction.y < 0 ? "Down" : "Up");
        Vector2[] positionsArray = null;
        switch (animName) {
            case "FishingHold":
                switch (dirKey) {
                    case "Left": positionsArray = _fishingHoldAnimationPositionsLeft; break;
                    case "Right": positionsArray = _fishingHoldAnimationPositionsRight; break;
                    case "Up": positionsArray = _fishingHoldAnimationPositionsUp; break;
                    case "Down": positionsArray = _fishingHoldAnimationPositionsDown; break;
                }
                break;

            case "FishingThrow":
                switch (dirKey) {
                    case "Left": positionsArray = _fishingThrowAnimationPositionsLeft; break;
                    case "Right": positionsArray = _fishingThrowAnimationPositionsRight; break;
                    case "Up": positionsArray = _fishingThrowAnimationPositionsUp; break;
                    case "Down": positionsArray = _fishingThrowAnimationPositionsDown; break;
                }
                break;

            case "FishingReelLoop":
                switch (dirKey) {
                    case "Left": positionsArray = _fishingReelLoopAnimationPositionsLeft; break;
                    case "Right": positionsArray = _fishingReelLoopAnimationPositionsRight; break;
                    case "Up": positionsArray = _fishingReelLoopAnimationPositionsUp; break;
                    case "Down": positionsArray = _fishingReelLoopAnimationPositionsDown; break;
                }
                break;

            case "FishingLand":
                switch (dirKey) {
                    case "Left": positionsArray = _fishingLandAnimationPositionsLeft; break;
                    case "Right": positionsArray = _fishingLandAnimationPositionsRight; break;
                    case "Up": positionsArray = _fishingLandAnimationPositionsUp; break;
                    case "Down": positionsArray = _fishingLandAnimationPositionsDown; break;
                }
                break;
        }
        if (positionsArray == null || positionsArray.Length == 0) return Vector2.zero;
        int frameCount = _animationFrameCounts.ContainsKey(animName) ? _animationFrameCounts[animName] : positionsArray.Length;

        int frameIndex;
        if (animName == "FishingHold") frameIndex = 1;
        else {
            var animInfo = _weaponAnim.GetCurrentAnimatorStateInfo(0);
            float cycleTime = animInfo.normalizedTime % 1f;
            frameIndex = Mathf.FloorToInt(cycleTime * frameCount);
        }
        frameIndex = Mathf.Clamp(frameIndex, 0, positionsArray.Length - 1);
        Debug.Log($"[GetFishingLineStartOffset] Animation={animName}, direction={dirKey}, frame={frameIndex}, offset={positionsArray[frameIndex]}");
        return positionsArray[frameIndex];
    }

    Vector3 GetLineStartPosition() {
        Vector3 pos = _cachedRodTip + (Vector3)GetFishingLineStartOffset();
        Debug.Log($"LineStartPosition = {pos} (cachedRodTip={_cachedRodTip})");
        return pos;
    }

    #endregion -------------------- Utility Methods --------------------
}
