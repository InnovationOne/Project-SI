using Unity.Netcode;
using UnityEngine;
using FMOD.Studio;

// This Script handles player movement, animations, and data persistance for a 2D character
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovementController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerMovementController LocalInstance { get; private set; }

    public Vector2 LastMotionDirection { get; private set; } = Vector2.right;

    [Header("Movement Settings")]
    [SerializeField] private float _maxWalkSpeed = 2f;
    [SerializeField] private float _minWalkSpeed = 0.2f;
    [SerializeField] private float _maxRunSpeed = 5f;
    [SerializeField] private float _minRunSpeed = 0.6f;
    private const float MAX_DELAY_FOR_PLAYER_ROTATION = 0.2f;

    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int RunningHash = Animator.StringToHash("Running");
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");

    private float _currentWalkSpeed;
    private float _currentRunSpeed;
    private bool _canMoveAndTurn = true;
    private bool _isRunning;
    private Vector2 _inputDirection;
    private float _timeSinceLastMovement;

    private Rigidbody2D _rb2D;
    private Animator _animator;
    private InputManager _inputManager;

    // Audio
    private EventInstance _playerWalkGrassEvent;


    private void Awake() {
        _rb2D = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();

        // Initialize audio event
        _playerWalkGrassEvent = AudioManager.Instance.CreateEventInstance(FMODEvents.Instance.PlayerWalkGrassSFX);
    }

    private void Start() {
        _inputManager = InputManager.Instance;
        if (_inputManager == null) {
            Debug.LogError("InputManager instance not found!");
        }

        _inputManager.OnRunAction += ToggleRunState;

        _currentWalkSpeed = _maxWalkSpeed;
        _currentRunSpeed = _maxRunSpeed;
    }

    private new void OnDestroy() {
        _inputManager.OnRunAction -= ToggleRunState;
        _playerWalkGrassEvent.release();
    }

    public override void OnNetworkSpawn() {
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

    private void FixedUpdate() {
        if (!IsOwner) {
            return;
        }

        UpdateSound();

        if (!_canMoveAndTurn) {
            StopMovingAndTurning();
            return;
        }

        // Get the normalized input direction
        _inputDirection = _inputManager.GetMovementVectorNormalized();
        UpdateAnimationState();

        // Move the character
        _rb2D.linearVelocity = _inputDirection * GetCurrentSpeed();
    }

    private void StopMovingAndTurning() {
        if (_rb2D.linearVelocity != Vector2.zero) {
            _rb2D.linearVelocity = Vector2.zero;
            _animator.SetBool(MovingHash, false);
        }
    }

    private void UpdateAnimationState() {
        bool isMoving = _inputDirection != Vector2.zero;
        _animator.SetBool(MovingHash, isMoving);
        _animator.SetBool(RunningHash, _isRunning);

        UpdateAnimatorParameters(isMoving);
    }

    private float GetCurrentSpeed() => _animator.GetBool(MovingHash) ? (_isRunning ? _currentRunSpeed : _currentWalkSpeed) : 0f;


    private void UpdateAnimatorParameters(bool isMoving) {
        // Set the input direction in the animator
        _animator.SetFloat(HorizontalHash, _inputDirection.x);
        _animator.SetFloat(VerticalHash, _inputDirection.y);

        if (isMoving) {
            LastMotionDirection = _inputDirection;
            _animator.SetFloat(LastHorizontalHash, _inputDirection.x);
            _animator.SetFloat(LastVerticalHash, _inputDirection.y);

            // Reset the rotation delay timer when the player starts moving
            _timeSinceLastMovement = 0f;
        } else {
            // Increment the timer when the player is not moving
            _timeSinceLastMovement += Time.deltaTime;

            // Update the rotation after a short delay
            if (_timeSinceLastMovement >= MAX_DELAY_FOR_PLAYER_ROTATION && LastMotionDirection != Vector2.zero) {
                _animator.SetFloat(LastHorizontalHash, LastMotionDirection.x);
                _animator.SetFloat(LastVerticalHash, LastMotionDirection.y);
            }
        }
    }

    public void SetCanMoveAndTurn(bool canMove) {
        _canMoveAndTurn = canMove;
    }

    public void ChangeMoveSpeed(bool isMaxSpeed) {
        _currentWalkSpeed = isMaxSpeed ? _maxWalkSpeed : _minWalkSpeed;
        _currentRunSpeed = isMaxSpeed ? _maxRunSpeed : _minRunSpeed;
    }

    private void UpdateSound() {
        bool isMoving = _rb2D.linearVelocity != Vector2.zero;

        if (isMoving) {
            _playerWalkGrassEvent.getPlaybackState(out PLAYBACK_STATE playbackState);
            if (playbackState == PLAYBACK_STATE.STOPPED) {
                _playerWalkGrassEvent.start();
            }
        } else {
            _playerWalkGrassEvent.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }

    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.Position = transform.position;
        playerData.LastDirection = new Vector2(_animator.GetFloat(LastHorizontalHash), _animator.GetFloat(LastVerticalHash));
    }

    public void LoadPlayer(PlayerData playerData) {
        transform.position = playerData.Position;
        _animator.SetFloat(LastHorizontalHash, playerData.LastDirection.x);
        _animator.SetFloat(LastVerticalHash, playerData.LastDirection.y);
    }
    #endregion
}
