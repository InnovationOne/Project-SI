using Unity.Netcode;
using UnityEngine;
using FMOD.Studio;
using System.Collections;
using static WeatherManager;

// This Script handles player movement, animations, and data persistance for a 2D character
[RequireComponent(typeof(NetworkObject))]
public class PlayerMovementController : NetworkBehaviour, IPlayerDataPersistance {
    // Movement Directions
    public Vector2 LastMotionDirection { get; private set; } = Vector2.right;

    [Header("Movement Settings")]
    [SerializeField] float _walkSpeed = 2f;
    [SerializeField] float _runSpeed = 4f;
    const float MAX_DELAY_FOR_PLAYER_ROTATION = 0.2f;

    [Header("Dash Settings")]
    [SerializeField] float _dashSpeedMultiplier = 3f; // How much faster than run speed
    [SerializeField] float _dashDuration = 0.2f;
    [SerializeField] float _dashCooldown = 1f;

    [Header("Visual Effects")]
    [SerializeField] GameObject _footprintPrefab;
    [SerializeField] float _effectSpawnInterval = 0.2f;
    [SerializeField] float _footprintSpread = 0.1f;
    const float EFFECT_FADE_OUT_TIMER = 1f;
    const float EFFECT_DESPAWN_TIMER = 5f;
    float _effectSpawnTimer = 0f;

    static readonly int MovingHash = Animator.StringToHash("Moving");
    static readonly int RunningHash = Animator.StringToHash("Running");
    static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    static readonly int VerticalHash = Animator.StringToHash("Vertical");
    static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");
    static readonly int DashingHash = Animator.StringToHash("Dashing");

    // Movement State
    bool _canMoveAndTurn = true;
    bool _isRunning;
    Vector2 _inputDirection;
    float _timeSinceLastMovement;

    // Dash State
    bool _isDashing = false;
    float _dashTimeRemaining;
    float _dashCooldownRemaining;
    Vector2 _dashDirection = Vector2.zero;

    // Components
    Rigidbody2D _rb2D;
    Animator _animator;
    InputManager _inputManager;
    BoxCollider2D _boxCollider;
    AudioManager _audioManager;

    // Audio
    EventInstance _playerWalkGrassEvent;
    EventInstance _playerDashEvent;


    void Awake() {
        _rb2D = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();

        // Initialize audio event
    }

    void Start() {
        _inputManager = InputManager.Instance;
        _audioManager = AudioManager.Instance;
        _playerWalkGrassEvent = _audioManager.CreateEventInstance(FMODEvents.Instance.PlayerWalkGrassSFX);
        //TODO _playerDashEvent = AudioManager.Instance.CreateEventInstance(FMODEvents.Instance.PlayerDashSFX);
        _inputManager.OnRunAction += ToggleRunState;
        _inputManager.OnDashAction += TryStartDash;
    }

    new void OnDestroy() {
        _playerWalkGrassEvent.release();
        //TODO _playerDashEvent.release();
        _inputManager.OnRunAction -= ToggleRunState;
        _inputManager.OnDashAction -= TryStartDash;

        base.OnDestroy();
    }

    void ToggleRunState() => _isRunning = !_isRunning;

    void Update() {
        if (!IsOwner) return;
        HandleInput();
        HandleDashTimers();
        UpdateAnimationState();
        UpdateSound();
        HandleRunningEffects();
    }

    void FixedUpdate() {
        if (!IsOwner || !_canMoveAndTurn) return;
        MoveCharacter();
    }

    void HandleInput() {
        Vector2 newInputDirection = _inputManager.GetMovementVectorNormalized();
        bool directionChanged = newInputDirection != _inputDirection;

        if (directionChanged) {
            _inputDirection = newInputDirection;
            _animator.SetFloat(HorizontalHash, _inputDirection.x);
            _animator.SetFloat(VerticalHash, _inputDirection.y);

            bool isMoving = _inputDirection != Vector2.zero;
            _animator.SetBool(MovingHash, isMoving);

            if (isMoving) {
                LastMotionDirection = _inputDirection;
                _animator.SetFloat(LastHorizontalHash, _inputDirection.x);
                _animator.SetFloat(LastVerticalHash, _inputDirection.y);
                _timeSinceLastMovement = 0f;
            } else {
                _timeSinceLastMovement += Time.deltaTime;
                if (_timeSinceLastMovement >= MAX_DELAY_FOR_PLAYER_ROTATION && LastMotionDirection != Vector2.zero) {
                    _animator.SetFloat(LastHorizontalHash, LastMotionDirection.x);
                    _animator.SetFloat(LastVerticalHash, LastMotionDirection.y);
                }
            }
        }
    }

    #region -------------------- Dashing --------------------
    void TryStartDash() {
        if (_dashCooldownRemaining > 0f || _isDashing) return;

        Vector2 dashDir = _inputDirection == Vector2.zero ? LastMotionDirection : _inputDirection;
        if (dashDir == Vector2.zero) return;

        StartDash(dashDir);
    }

    void StartDash(Vector2 direction) {
        _isDashing = true;
        _dashTimeRemaining = _dashDuration;
        _dashCooldownRemaining = _dashCooldown;
        _dashDirection = direction.normalized;

        _animator.SetBool(DashingHash, true);
        _playerDashEvent.start();

        // (Optional) Trigger visual effects like motion blur or trails here.

        PerformDashServerRpc();
    }

    void EndDash() {
        _isDashing = false;
        _animator.SetBool(DashingHash, false);
    }

    private void HandleDashTimers() {
        if (_dashCooldownRemaining > 0f) _dashCooldownRemaining -= Time.deltaTime;

        if (_isDashing) {
            _dashTimeRemaining -= Time.deltaTime;
            if (_dashTimeRemaining <= 0f) {
                EndDash();
            }
        }
    }
    #endregion -------------------- Dashing --------------------

    #region -------------------- Movement --------------------
    void MoveCharacter() {
        if (_isDashing) {
            float dashSpeed = _runSpeed * _dashSpeedMultiplier;
            _rb2D.linearVelocity = _dashDirection * dashSpeed;
        } else {
            float speed = GetCurrentSpeed();
            _rb2D.linearVelocity = _inputDirection * speed;
        }
    }

    void UpdateAnimationState() {
        bool isMoving = _inputDirection != Vector2.zero && !_isDashing;
        _animator.SetBool(MovingHash, isMoving);
        _animator.SetBool(RunningHash, _isRunning && !_isDashing);
    }

    float GetCurrentSpeed() {
        if (_isDashing) {
            return _runSpeed * _dashSpeedMultiplier;
        }
        return _animator.GetBool(MovingHash) ? (_isRunning ? _runSpeed : _walkSpeed) : 0f;
    }

    public void SetCanMoveAndTurn(bool canMove) {
        _canMoveAndTurn = canMove;
        if (!_canMoveAndTurn && _rb2D.linearVelocity != Vector2.zero) {
            _rb2D.linearVelocity = Vector2.zero;
            _animator.SetBool(MovingHash, false);
        }
    }

    void UpdateSound() {
        bool isMoving = _rb2D.linearVelocity != Vector2.zero;
        _playerWalkGrassEvent.getPlaybackState(out PLAYBACK_STATE playbackState);

        if (isMoving && playbackState == PLAYBACK_STATE.STOPPED) {
            _playerWalkGrassEvent.start();
        } else if (!isMoving && playbackState == PLAYBACK_STATE.PLAYING) {
            _playerWalkGrassEvent.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
    #endregion -------------------- Movement --------------------

    #region -------------------- Running Effects --------------------
    void HandleRunningEffects() {
        if (!IsOwner) return;
        if (_animator.GetBool(MovingHash) && !_isDashing) {
            _effectSpawnTimer -= Time.deltaTime;
            if (_effectSpawnTimer <= 0f) {
                SpawnFootprints();
                _effectSpawnTimer = _effectSpawnInterval;
            }
        } else {
            _effectSpawnTimer = 0f;
        }
    }

    void SpawnFootprints() {
        bool spawnAllowed = WeatherManager.Instance.CurrentWeather switch {
            WeatherName.Rain => true,
            WeatherName.Thunder => true,
            WeatherName.Snow => true,
            _ => false
        };
        if (!spawnAllowed) return;

        float offsetX = Random.Range(-_footprintSpread, _footprintSpread);
        float offsetY = Random.Range(-_footprintSpread, _footprintSpread);
        var spawnPosition = _boxCollider.bounds.center + new Vector3(offsetX, offsetY, 0f);

        // TODO: Object pooling could be used here to avoid repeated instantiation
        var spawnedEffect = Instantiate(_footprintPrefab, spawnPosition, Quaternion.identity);
        StartCoroutine(FadeOutAndDestroy(spawnedEffect, EFFECT_DESPAWN_TIMER, EFFECT_FADE_OUT_TIMER));
    }

    IEnumerator FadeOutAndDestroy(GameObject obj, float delay, float fadeDuration) {
        yield return new WaitForSeconds(delay);
        if (!obj.TryGetComponent<SpriteRenderer>(out var sr)) {
            Destroy(obj);
            yield break;
        }

        var c = sr.color;
        float startAlpha = c.a;
        float time = 0f;

        while (time < fadeDuration) {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, t);
            sr.color = new Color(c.r, c.g, c.b, newAlpha);
            yield return null;
        }

        Destroy(obj);
    }
    #endregion -------------------- Running Effects --------------------

    #region -------------------- Networking (Dash Synchronization) --------------------
    [ServerRpc]
    void PerformDashServerRpc() => PerformDashClientRpc();

    [ClientRpc]
    void PerformDashClientRpc() {
        if (IsOwner) return;

        _animator.SetBool(DashingHash, true);
        _playerDashEvent.start();
        StartCoroutine(RemoteDashEndRoutine(_dashDuration));
    }

    IEnumerator RemoteDashEndRoutine(float duration) {
        yield return new WaitForSeconds(duration);
        _animator.SetBool(DashingHash, false);
    }

    #endregion -------------------- Networking (Dash Synchronization) --------------------

    #region -------------------- Save & Load --------------------
    public void SavePlayer(PlayerData playerData) {
        playerData.Position = transform.position;
        playerData.LastDirection = new Vector2(_animator.GetFloat(LastHorizontalHash), _animator.GetFloat(LastVerticalHash));
    }

    public void LoadPlayer(PlayerData playerData) {
        transform.position = playerData.Position;
        LastMotionDirection = playerData.LastDirection;
        _animator.SetFloat(LastHorizontalHash, LastMotionDirection.x);
        _animator.SetFloat(LastVerticalHash, LastMotionDirection.y);
    }
    #endregion -------------------- Save & Load --------------------
}
