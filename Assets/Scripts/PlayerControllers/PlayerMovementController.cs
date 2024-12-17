using Unity.Netcode;
using UnityEngine;
using FMOD.Studio;
using System.Collections;
using static WeatherManager;

// This Script handles player movement, animations, and data persistance for a 2D character
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovementController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerMovementController LocalInstance { get; private set; }

    // Movement Directions
    public Vector2 LastMotionDirection { get; private set; } = Vector2.right;

    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 4f;
    private const float MAX_DELAY_FOR_PLAYER_ROTATION = 0.2f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeedMultiplier = 3f; // How much faster than run speed
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooldown = 1f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject _footprintPrefab;
    [SerializeField] private float _effectSpawnInterval = 0.2f;
    [SerializeField] private float _footprintSpread = 0.1f;
    private float _effectSpawnTimer = 0f;
    private const float EFFECT_FADE_OUT_TIMER = 1f;
    private const float EFFECT_DESPAWN_TIMER = 5f;

    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int RunningHash = Animator.StringToHash("Running");
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");
    private static readonly int DashingHash = Animator.StringToHash("Dashing");

    // Movement State
    private bool _canMoveAndTurn = true;
    private bool _isRunning;
    private Vector2 _inputDirection;
    private float _timeSinceLastMovement;

    // Dash State
    private bool _isDashing = false;
    private float _dashTimeRemaining;
    private float _dashCooldownRemaining;
    private Vector2 _dashDirection = Vector2.zero;

    // Components
    private Rigidbody2D _rb2D;
    private Animator _animator;
    private InputManager _inputManager;
    private BoxCollider2D _boxCollider;

    // Audio
    private EventInstance _playerWalkGrassEvent;
    private EventInstance _playerDashEvent;


    private void Awake() {
        _rb2D = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();

        // Initialize audio event
        _playerWalkGrassEvent = AudioManager.Instance.CreateEventInstance(FMODEvents.Instance.PlayerWalkGrassSFX);
        //TODO _playerDashEvent = AudioManager.Instance.CreateEventInstance(FMODEvents.Instance.PlayerDashSFX);
    }

    private void Start() {
        _inputManager = InputManager.Instance;
        if (_inputManager == null) {
            Debug.LogError("InputManager instance not found!");
            enabled = false;
            return;
        }

        _inputManager.OnRunAction += ToggleRunState;
        _inputManager.OnDashAction += TryStartDash;
    }

    private new void OnDestroy() {
        _inputManager.OnRunAction -= ToggleRunState;
        _playerWalkGrassEvent.release();
        //TODO _playerDashEvent.release();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerMovementController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    private void ToggleRunState() {
        _isRunning = !_isRunning;
    }

    private void Update() {
        if (!IsOwner) {
            return;
        }

        HandleInput();
        HandleDashTimers();
        UpdateAnimationState();
        UpdateSound();
        HandleRunningEffects();
    }

    private void FixedUpdate() {
        if (!IsOwner || !_canMoveAndTurn) {
            return;
        }

        MoveCharacter();
    }

    private void HandleInput() {
        Vector2 newInputDirection = _inputManager.GetMovementVectorNormalized();

        bool directionChanged = newInputDirection != _inputDirection;

        // Detect movement input changes
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

    private void TryStartDash() {
        if (_dashCooldownRemaining > 0f || _isDashing) return;

        // Direction for dash:
        // If currently not moving but we have a last motion direction, dash in that direction.
        Vector2 dashDir = _inputDirection == Vector2.zero ? LastMotionDirection : _inputDirection;
        if (dashDir == Vector2.zero) return; // if still zero, no dash

        StartDash(dashDir);
    }

    private void StartDash(Vector2 direction) {
        _isDashing = true;
        _dashTimeRemaining = _dashDuration;
        _dashCooldownRemaining = _dashCooldown;
        _dashDirection = direction.normalized;

        // Trigger dash animation
        _animator.SetBool(DashingHash, true);

        // Audio feedback for dash
        _playerDashEvent.start();

        // (Optional) Trigger visual effects like motion blur or trails here.

        // Notify server and other clients about the dash if needed
        PerformDashServerRpc();
    }

    private void EndDash() {
        _isDashing = false;
        _animator.SetBool(DashingHash, false);
    }

    private void HandleDashTimers() {
        if (_dashCooldownRemaining > 0f) {
            _dashCooldownRemaining -= Time.deltaTime;
        }

        if (_isDashing) {
            _dashTimeRemaining -= Time.deltaTime;
            if (_dashTimeRemaining <= 0f) {
                EndDash();
            }
        }
    }

    #endregion -------------------- Dashing --------------------

    #region -------------------- Movement --------------------

    private void MoveCharacter() {
        if (_isDashing) {
            // While dashing, movement is overridden by dash speed
            float dashSpeed = _runSpeed * _dashSpeedMultiplier;
            _rb2D.linearVelocity = _dashDirection * dashSpeed;
        } else {
            float speed = GetCurrentSpeed();
            _rb2D.linearVelocity = _inputDirection * speed;
        }
    }

    private void UpdateAnimationState() {
        bool isMoving = _inputDirection != Vector2.zero && !_isDashing;
        _animator.SetBool(MovingHash, isMoving);
        _animator.SetBool(RunningHash, _isRunning && !_isDashing);
    }

    private float GetCurrentSpeed() {
        if (_isDashing) {
            return _runSpeed * _dashSpeedMultiplier;
        }
        return _animator.GetBool(MovingHash) ? (_isRunning ? _runSpeed : _walkSpeed) : 0f;
    }


    public void SetCanMoveAndTurn(bool canMove) {
        _canMoveAndTurn = canMove;
        if (!_canMoveAndTurn) {
            StopMoving();
        }
    }

    private void StopMoving() {
        if (_rb2D.linearVelocity != Vector2.zero) {
            _rb2D.linearVelocity = Vector2.zero;
            _animator.SetBool(MovingHash, false);
        }
    }

    private void UpdateSound() {
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
    private void HandleRunningEffects() {
        if (!IsOwner) return;

        bool isMoving = _animator.GetBool(MovingHash) && !_isDashing;

        if (isMoving) {
            _effectSpawnTimer -= Time.deltaTime;
            if (_effectSpawnTimer <= 0f) {
                SpawnRunningEffect();
                _effectSpawnTimer = _effectSpawnInterval;
            }
        } else {
            _effectSpawnTimer = 0f;
        }
    }

    private void SpawnRunningEffect() {
        WeatherName currentWeather = Instance.CurrentWeather;
        if (currentWeather != WeatherName.Rain && currentWeather != WeatherName.Thunder && currentWeather != WeatherName.Snow) return;
        float offsetX = Random.Range(-_footprintSpread, _footprintSpread);
        float offsetY = Random.Range(-_footprintSpread, _footprintSpread);
        var spawnPosition = _boxCollider.bounds.center + new Vector3(offsetX, offsetY, 0f);
        var spawnedEffect = Instantiate(_footprintPrefab, spawnPosition, Quaternion.identity);
        StartCoroutine(FadeOutAndDestroy(spawnedEffect, EFFECT_DESPAWN_TIMER, EFFECT_FADE_OUT_TIMER));
    }

    private IEnumerator FadeOutAndDestroy(GameObject obj, float delay, float fadeDuration) {
        // Erst warten wir die vorgesehene Lebensdauer ab
        yield return new WaitForSeconds(delay);

        if (!obj.TryGetComponent<SpriteRenderer>(out var sr)) {
            // Falls kein SpriteRenderer vorhanden ist, direkt zerstören
            Destroy(obj);
            yield break;
        }

        // Ausblenden über fadeDuration
        Color initialColor = sr.color;
        float startAlpha = initialColor.a;
        float time = 0f;

        while (time < fadeDuration) {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, t);
            sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, newAlpha);
            yield return null; // Warten auf nächsten Frame
        }

        // Wenn komplett ausgeblendet, das Objekt zerstören
        Destroy(obj);
    }
    #endregion -------------------- Running Effects --------------------

    #region -------------------- Networking (Dash Synchronization) --------------------
    [ServerRpc]
    private void PerformDashServerRpc() {
        PerformDashClientRpc();
    }

    [ClientRpc]
    private void PerformDashClientRpc() {
        if (IsOwner) return;
        _animator.SetBool(DashingHash, true);
        _playerDashEvent.start();
        StartCoroutine(RemoteDashEndRoutine(_dashDuration));
    }

    private IEnumerator RemoteDashEndRoutine(float duration) {
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
