using Unity.Netcode;
using UnityEngine;
using FMOD.Studio;

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

    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int RunningHash = Animator.StringToHash("Running");
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");

    // Movement State
    private bool _canMoveAndTurn = true;
    private bool _isRunning;
    private Vector2 _inputDirection;
    private float _timeSinceLastMovement;

    // Components
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
            enabled = false;
            return;
        }

        _inputManager.OnRunAction += ToggleRunState;
    }

    private new void OnDestroy() {
        _inputManager.OnRunAction -= ToggleRunState;
        _playerWalkGrassEvent.release();
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
        UpdateAnimationState();
        UpdateSound();
    }

    private void FixedUpdate() {
        if (!IsOwner || !_canMoveAndTurn) { 
            return; 
        }

        MoveCharacter();
    }

    private void HandleInput() {
        Vector2 newInputDirection = _inputManager.GetMovementVectorNormalized();

        if (newInputDirection != _inputDirection) {
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

    private void MoveCharacter() {
        float speed = GetCurrentSpeed();
        _rb2D.linearVelocity = _inputDirection * speed;
    }

    private void UpdateAnimationState() {
        bool isMoving = _inputDirection != Vector2.zero;
        _animator.SetBool(MovingHash, isMoving);
        _animator.SetBool(RunningHash, _isRunning);
    }

    private float GetCurrentSpeed() { 
        return _animator.GetBool(MovingHash) ? (_isRunning? _runSpeed : _walkSpeed) : 0f;
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

    #region Save & Load
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
    #endregion
}
