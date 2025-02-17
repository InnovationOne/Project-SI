using Unity.Netcode;
using UnityEngine;
using FMOD.Studio;
using System.Collections;
using static WeatherManager;
using static PlayerAnimationController;
using System.Collections.Generic;

// This Script handles player movement, animations, and data persistance for a 2D character
[RequireComponent(typeof(NetworkObject))]
public class PlayerMovementController : NetworkBehaviour, IPlayerDataPersistance {
    public Vector2 LastMotionDirection { get; private set; } = Vector2.right;

    [Header("Movement Settings")]
    [SerializeField] float _walkSpeed = 2f;
    [SerializeField] float _runSpeed = 4f;
    const float MAX_DELAY_FOR_PLAYER_ROTATION = 0.2f;

    [Header("Dash Settings")]
    [SerializeField] float _dashSpeedMultiplier = 3f; // How much faster than run speed
    [SerializeField] float _dashDuration = 0.2f;
    [SerializeField] float _dashCooldown = 1f;
    const float _dashEnergyCost = 2f;

    [Header("Visual Effects")]
    [SerializeField] GameObject _footprintPrefab;
    [SerializeField] float _effectSpawnInterval = 0.2f;
    [SerializeField] float _footprintSpread = 0.1f;
    const float EFFECT_FADE_OUT_TIMER = 1f;
    const float EFFECT_DESPAWN_TIMER = 5f;
    float _effectSpawnTimer = 0f;

    [Header("Audio Settings")]
    [Tooltip("Minimum interval between footstep sounds.")]
    [SerializeField] private float _footstepInterval = 0.3f;
    private float _footstepTimer = 0f;

    [Header("Pooling Settings")]
    [SerializeField] private int _footprintPoolSize = 20;
    private Queue<GameObject> _footprintPool = new();

    // State variables for movement and dashing.
    bool _canMoveAndTurn = true;
    bool _isRunning = true;
    Vector2 _inputDirection;
    public Vector3 Position;
    float _timeSinceLastMovement;

    // Dash-specific state.
    private float _dashCooldownRemaining;
    private Vector2 _dashDirection = Vector2.zero;

    // Components
    Rigidbody2D _rb;
    InputManager _inputManager;
    BoxCollider2D _boxCollider;
    AudioManager _audioManager;
    FMODEvents _fmodEvents;
    PlayerHealthAndEnergyController _playerHealthAndEnergyController;
    PlayerAnimationController _playerAnimationController;

    #region -------------------- Unity Lifecycle --------------------

    void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _playerHealthAndEnergyController = GetComponent<PlayerHealthAndEnergyController>();
        _playerAnimationController = GetComponent<PlayerAnimationController>();
    }

    // Initialize component references and object pool.
    void Start() {
        _inputManager = GameManager.Instance.InputManager;
        _audioManager = GameManager.Instance.AudioManager;
        _fmodEvents = GameManager.Instance.FMODEvents;

        _inputManager.OnRunAction += ToggleRunState;
        _inputManager.OnDashAction += TryStartDash;

        InitializeFootprintPool();
    }

    // Unsubscribe from events to prevent memory leaks.
    new void OnDestroy() {
        if (_inputManager != null) {
            _inputManager.OnRunAction -= ToggleRunState;
            _inputManager.OnDashAction -= TryStartDash;
        }
        base.OnDestroy();
    }

    // Pre-instantiate footprint objects for pooling.
    void InitializeFootprintPool() {
        for (int i = 0; i < _footprintPoolSize; i++) {
            var footprint = Instantiate(_footprintPrefab);
            footprint.SetActive(false);
            _footprintPool.Enqueue(footprint);
        }
    }

    // Toggle running state and update animation.
    void ToggleRunState() {
        _isRunning = !_isRunning;

        bool isMoving = _inputDirection != Vector2.zero;
        if (isMoving) _playerAnimationController.ChangeState(PlayerState.Walkcycle);
        else _playerAnimationController.ChangeState(PlayerState.Idle);
    }

    void Update() {
        if (!IsOwner) return;

        Position = _boxCollider.bounds.center;
        HandleInput();

        if (_dashCooldownRemaining > 0f) _dashCooldownRemaining -= Time.deltaTime;

        UpdateSound();
        HandleRunningEffects();
    }

    void FixedUpdate() {
        if (!IsOwner || !_canMoveAndTurn) return;
        MoveCharacter();
    }

    // Process and update movement input.
    void HandleInput() {
        // Only accept input when movement is allowed.
        if (!CanAcceptMovement()) return;

        Vector2 newInputDirection = _inputManager.GetMovementVectorNormalized();
        bool directionChanged = newInputDirection != _inputDirection;
        bool isMoving = newInputDirection != Vector2.zero;

        // Update animation based on whether the player is moving.
        _playerAnimationController.ChangeState(isMoving ? PlayerState.Walkcycle : PlayerState.Idle);

        if (directionChanged) {
            _inputDirection = newInputDirection;
            _playerAnimationController.SetAnimatorDirection(_inputDirection);

            if (isMoving) {
                LastMotionDirection = _inputDirection;
                _playerAnimationController.SetAnimatorLastDirection(_inputDirection);
                _timeSinceLastMovement = 0f;
            } else {
                _timeSinceLastMovement += Time.deltaTime;
                if (_timeSinceLastMovement >= MAX_DELAY_FOR_PLAYER_ROTATION && LastMotionDirection != Vector2.zero) {
                    _playerAnimationController.SetAnimatorLastDirection(LastMotionDirection);
                }
            }
        }
    }

    #endregion -------------------- Unity Lifecycle --------------------

    #region -------------------- Dashing --------------------

    // Try to initiate a dash if conditions are met.
    void TryStartDash() {
        if (_dashCooldownRemaining > 0f || !CanAcceptMovement()) return;

        _dashDirection = _inputDirection == Vector2.zero ? LastMotionDirection : _inputDirection;
        if (_dashDirection == Vector2.zero) return;

        _dashCooldownRemaining = _dashCooldown;
        _playerHealthAndEnergyController.AdjustEnergy(-_dashEnergyCost);
        StartCoroutine(StartDash());
    }

    // Coroutine that manages the dash duration and state transition.
    IEnumerator StartDash() {
        var lastState = _playerAnimationController.ActivePlayerState;
        _playerAnimationController.ChangeState(PlayerState.Dashing);

        // TODO: Play dash sound
        //_audioManager.PlayOneShot(_fmodEvents.Dash, transform.position);

        // TODO: Trigger visual effects like motion blur or trails here.

        yield return new WaitForSeconds(_dashDuration);
        _playerAnimationController.ChangeState(lastState);
    }

    // Check if the player is allowed to accept movement input.
    bool CanAcceptMovement() => _canMoveAndTurn && (_playerAnimationController.ActivePlayerState == PlayerState.Idle || _playerAnimationController.ActivePlayerState == PlayerState.Walkcycle);

    #endregion -------------------- Dashing --------------------

    #region -------------------- Movement --------------------

    // Update the player’s position based on input and state.
    void MoveCharacter() {
        if (_playerAnimationController.ActivePlayerState == PlayerState.Dashing) {
            float dashSpeed = _runSpeed * _dashSpeedMultiplier;
            _rb.linearVelocity = _dashDirection * dashSpeed;
        } else if (CanAcceptMovement()) {
            float speed = GetCurrentSpeed();
            _rb.linearVelocity = _inputDirection * speed;
        }
    }

    // Return the movement speed based on current state and run toggle.
    float GetCurrentSpeed() =>
        _playerAnimationController.ActivePlayerState switch {
            PlayerState.Idle => 0f,
            PlayerState.Walkcycle => _isRunning ? _runSpeed : _walkSpeed,
            PlayerState.Dashing => _walkSpeed * _dashSpeedMultiplier,
            _ => 0f,
        };

    // Enable or disable the player's ability to move.
    public void SetCanMoveAndTurn(bool canMove) {
        _canMoveAndTurn = canMove;
        if (!_canMoveAndTurn) {
            _playerAnimationController.ChangeState(PlayerState.Idle);
            if (_rb.linearVelocity != Vector2.zero)
                _rb.linearVelocity = Vector2.zero;
        }
    }

    void UpdateSound() {
        if (_rb.linearVelocity != Vector2.zero) {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f) {
                // TODO: Sync footstep sound with specific animation frames if needed.
                _audioManager.PlayOneShot(_fmodEvents.Footsteps, transform.position);
                _footstepTimer = _footstepInterval;
            }
        }
    }

    #endregion -------------------- Movement --------------------

    #region -------------------- Running Effects --------------------

    // Handle the visual effect when the player is running.
    void HandleRunningEffects() {
        if (!IsOwner) return;
        if (_rb.linearVelocity != Vector2.zero && _playerAnimationController.ActivePlayerState != PlayerState.Dashing) {
            _effectSpawnTimer -= Time.deltaTime;
            if (_effectSpawnTimer <= 0f) {
                SpawnFootprints();
                _effectSpawnTimer = _effectSpawnInterval;
            }
        } else _effectSpawnTimer = 0f;
    }

    // Spawn a footprint effect using object pooling.
    void SpawnFootprints() {
        return;
        // Allow footprint spawn only under specific weather conditions.
        bool spawnAllowed = GameManager.Instance.WeatherManager.CurrentWeather switch {
            WeatherName.Rain or WeatherName.Thunder or WeatherName.Snow => true,
            _ => false,
        };
        if (!spawnAllowed) return;

        // Calculate spawn position with a random offset.
        float offsetX = Random.Range(-_footprintSpread, _footprintSpread);
        float offsetY = Random.Range(-_footprintSpread, _footprintSpread);
        Vector3 spawnPosition = _boxCollider.bounds.center + new Vector3(offsetX, offsetY, 0f);

        // Get a footprint from the pool or create a new one if necessary.
        GameObject footprint = GetFootprintFromPool();
        footprint.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        footprint.SetActive(true);

        StartCoroutine(FadeOutAndReturnToPool(footprint, EFFECT_DESPAWN_TIMER, EFFECT_FADE_OUT_TIMER));
    }


    // Retrieve a footprint object from the pool.
    GameObject GetFootprintFromPool() {
        if (_footprintPool.Count > 0)
            return _footprintPool.Dequeue();
        return Instantiate(_footprintPrefab);
    }

    // Fade out the footprint and return it to the object pool.
    IEnumerator FadeOutAndReturnToPool(GameObject footprint, float delay, float fadeDuration) {
        yield return new WaitForSeconds(delay);
        if (!footprint.TryGetComponent<SpriteRenderer>(out var spriteRenderer)) {
            footprint.SetActive(false);
            _footprintPool.Enqueue(footprint);
            yield break;
        }

        Color originalColor = spriteRenderer.color;
        float startAlpha = originalColor.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null;
        }

        // Reset color and return the footprint to the pool.
        spriteRenderer.color = originalColor;
        footprint.SetActive(false);
        _footprintPool.Enqueue(footprint);
    }

    #endregion -------------------- Running Effects --------------------


    #region -------------------- Save & Load --------------------

    // Save the player's current position.
    public void SavePlayer(PlayerData playerData) {
        playerData.Position = transform.position;
    }

    // Load the player's saved position and last movement direction.
    public void LoadPlayer(PlayerData playerData) {
        transform.position = playerData.Position;
        LastMotionDirection = playerData.LastDirection;
    }
    #endregion -------------------- Save & Load --------------------
}
