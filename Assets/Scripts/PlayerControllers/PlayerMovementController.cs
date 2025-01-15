using Unity.Netcode;
using UnityEngine;
using FMOD.Studio;
using System.Collections;
using static WeatherManager;
using static PlayerAnimationController;

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

    // Movement State
    bool _canMoveAndTurn = true;
    bool _isRunning;
    Vector2 _inputDirection;
    public Vector3 Position;
    float _timeSinceLastMovement;

    // Dash State
    const float _dashEnergyCost = 2f;
    float _dashTimeRemaining;
    float _dashCooldownRemaining;
    Vector2 _dashDirection = Vector2.zero;

    // Components
    Rigidbody2D _rb;
    InputManager _inputManager;
    BoxCollider2D _boxCollider;
    AudioManager _audioManager;
    PlayerHealthAndEnergyController _playerHealthAndEnergyController;
    PlayerToolsAndWeaponController _playerToolsAndWeaponController;
    PlayerToolbeltController _playerToolbeltController;
    PlayerAnimationController _pAC;

    // Audio
    EventInstance _playerWalkGrassEvent;
    EventInstance _playerDashEvent;

    void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();

        // Initialize audio event
    }

    void Start() {
        _inputManager = GameManager.Instance.InputManager;
        _audioManager = GameManager.Instance.AudioManager;
        _playerWalkGrassEvent = _audioManager.CreateEventInstance(GameManager.Instance.FMODEvents.PlayerWalkGrassSFX);
        _playerHealthAndEnergyController = GetComponent<PlayerHealthAndEnergyController>();
        _playerToolsAndWeaponController = GetComponent<PlayerToolsAndWeaponController>();
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _pAC = GetComponent<PlayerAnimationController>();

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

    void ToggleRunState() {
        _isRunning = !_isRunning;

        bool isMoving = _inputDirection != Vector2.zero;
        if (isMoving) {
            _pAC.ChangeState(PlayerState.Walkcycle);
        } else {
            _pAC.ChangeState(PlayerState.Idle);
        }
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

    void HandleInput() {
        Vector2 newInputDirection = _inputManager.GetMovementVectorNormalized();
        bool directionChanged = newInputDirection != _inputDirection;

        bool isMoving = newInputDirection != Vector2.zero;
        if (isMoving) {
            _pAC.ChangeState(PlayerState.Walkcycle);
        } else {
            _pAC.ChangeState(PlayerState.Idle);
        }

        if (directionChanged) {
            _inputDirection = newInputDirection;
            _pAC.SetAnimatorDirection(_inputDirection);

            if (isMoving) {
                LastMotionDirection = _inputDirection;
                _pAC.SetAnimatorLastDirection(_inputDirection);
                _timeSinceLastMovement = 0f;
            } else {
                _timeSinceLastMovement += Time.deltaTime;
                if (_timeSinceLastMovement >= MAX_DELAY_FOR_PLAYER_ROTATION && LastMotionDirection != Vector2.zero) {
                    _pAC.SetAnimatorLastDirection(LastMotionDirection);
                }
            }
        }
    }

    #region -------------------- Dashing --------------------
    void TryStartDash() {
        if (_dashCooldownRemaining > 0f && !CanDash()) return;

        Vector2 dashDir = _inputDirection == Vector2.zero ? LastMotionDirection : _inputDirection;
        if (dashDir == Vector2.zero) return;

        StartCoroutine(StartDash(dashDir));
    }

    IEnumerator StartDash(Vector2 direction) {
        _dashTimeRemaining = _dashDuration;
        _dashCooldownRemaining = _dashCooldown;
        _dashDirection = direction.normalized;

        var lastState = _pAC.ActivePlayerState;
        _pAC.ChangeState(PlayerState.Dashing);
        _playerDashEvent.start();
        _playerHealthAndEnergyController.AdjustEnergy(-_dashEnergyCost);

        // (Optional) Trigger visual effects like motion blur or trails here.

        yield return new WaitForSeconds(_dashDuration);
        _pAC.ChangeState(lastState);
    }

    bool CanDash() => _pAC.ActivePlayerState == PlayerState.Idle || _pAC.ActivePlayerState == PlayerState.Walkcycle;
    #endregion -------------------- Dashing --------------------

    #region -------------------- Movement --------------------
    void MoveCharacter() {
        if (CanDash() || _pAC.ActivePlayerState == PlayerState.Dashing) {

            if (_pAC.ActivePlayerState == PlayerState.Dashing) {
                float dashSpeed = _runSpeed * _dashSpeedMultiplier;
                _rb.linearVelocity = _dashDirection * dashSpeed;
            } else {
                float speed = GetCurrentSpeed();
                _rb.linearVelocity = _inputDirection * speed;
            }
        }
    }

    float GetCurrentSpeed() {
        return _pAC.ActivePlayerState switch {
            PlayerState.Idle => 0f,
            PlayerState.Walkcycle => _walkSpeed,
            PlayerState.Dashing => _walkSpeed * _dashSpeedMultiplier,
            _ => 0f,
        };
    }

    public void SetCanMoveAndTurn(bool canMove) {
        _canMoveAndTurn = canMove;
        if (!_canMoveAndTurn && _rb.linearVelocity != Vector2.zero) {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    void UpdateSound() {
        bool isMoving = _rb.linearVelocity != Vector2.zero;
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
        if (_rb.linearVelocity != Vector2.zero && _pAC.ActivePlayerState != PlayerState.Dashing) {
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
        bool spawnAllowed = GameManager.Instance.WeatherManager.CurrentWeather switch {
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


    #region -------------------- Save & Load --------------------
    public void SavePlayer(PlayerData playerData) {
        playerData.Position = transform.position;
    }

    public void LoadPlayer(PlayerData playerData) {
        transform.position = playerData.Position;
        LastMotionDirection = playerData.LastDirection;
    }
    #endregion -------------------- Save & Load --------------------
}
