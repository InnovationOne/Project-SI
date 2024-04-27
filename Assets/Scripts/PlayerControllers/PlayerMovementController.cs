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
    [SerializeField] private float _maxWalkSpeed = 1f;
    [SerializeField] private float _minWalkSpeed = 0.2f;
    [SerializeField] private float _maxRunSpeed = 3f;
    [SerializeField] private float _minRunSpeed = 0.6f;
    private const float MAX_DELAY_FOR_PLAYER_ROTATION = 0.2f;

    private const string MOVING = "Moving";
    private const string RUNNING = "Running";
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private const string LAST_HORIZONTAL = "LastHorizontal";
    private const string LAST_VERTICAL = "LastVertical";

    private float _currentWalkSpeed;
    private float _currentRunSpeed;
    private bool _canMoveAndTurn = true;
    private bool _isRunning;
    private Vector2 _inputDirection;
    private float _currentTimeForPlayerRotation;

    private Rigidbody2D _rigidBody2D;
    private Animator _animator;
    private InputManager _inputManager;

    // Audio
    private EventInstance _playerWalkGrass;


    private void Start() {
        _rigidBody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _inputManager = InputManager.Instance;

        _inputManager.OnRunAction += InputManager_OnRunAction;

        _playerWalkGrass = AudioManager.Instance.CreateEventInstance(FMODEvents.Instance.PlayerWalkGrassSFX);

        _currentWalkSpeed = _maxWalkSpeed;
        _currentRunSpeed = _maxRunSpeed;
    }

    private new void OnDestroy() {
        _inputManager.OnRunAction -= InputManager_OnRunAction;
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

    private void InputManager_OnRunAction() {
        _isRunning = !_isRunning;
    }

    private void FixedUpdate() {
        // Check if the player is the owner of this object
        if (!IsOwner) {
            return;
        }

        UpdateSound();

        // Diable move and turn
        if (!_canMoveAndTurn) {
            StopMovingAndTurning();
            return;
        }

        //Get the normalized input direction
        _inputDirection = _inputManager.GetMovementVectorNormalized();
        UpdateAnimationState();

        //Move the character
        _rigidBody2D.velocity = _inputDirection * GetCurrentSpeed();
    }

    private void StopMovingAndTurning() {
        if (_rigidBody2D.velocity != Vector2.zero) {
            _rigidBody2D.velocity = Vector2.zero;
            _animator.SetBool(MOVING, false);
        }
    }

    private void UpdateAnimationState() {
        _animator.SetBool(MOVING, _inputDirection.x != 0 || _inputDirection.y != 0);
        _animator.SetBool(RUNNING, _isRunning);
        UpdateAnimatorParameters();
    }

    private float GetCurrentSpeed() {
        return _animator.GetBool(MOVING) ? (_isRunning ? _currentRunSpeed : _currentWalkSpeed) : 0;
    }


    private void UpdateAnimatorParameters() {
        // Set the input direction in the animator
        _animator.SetFloat(HORIZONTAL, _inputDirection.x);
        _animator.SetFloat(VERTICAL, _inputDirection.y);

        // Save last motion to apply idle rotation
        if (_animator.GetBool(MOVING)) {
            LastMotionDirection = _inputManager.GetMovementVectorNormalized();
            _animator.SetFloat(LAST_HORIZONTAL, _inputDirection.x);
            _animator.SetFloat(LAST_VERTICAL, _inputDirection.y);

            // Reset the delayForPlayerRotation timer when the player starts moving
            _currentTimeForPlayerRotation = 0;
        } else {
            // Increment the timer when the player is not moving
            _currentTimeForPlayerRotation += Time.deltaTime;

            // Update the rotation after a short delay
            if (_currentTimeForPlayerRotation >= MAX_DELAY_FOR_PLAYER_ROTATION && 
                !(LastMotionDirection.x == 0 && LastMotionDirection.y == 0)) {

                _animator.SetFloat(LAST_HORIZONTAL, LastMotionDirection.x);
                _animator.SetFloat(LAST_VERTICAL, LastMotionDirection.y);
            }
        }
    }

    public void SetCanMoveAndTurn(bool canMoveAndTurn) {
        _canMoveAndTurn = canMoveAndTurn;
    }

    public void ChangeMoveSpeed(bool s) {
        _ = s ? _currentWalkSpeed = _maxWalkSpeed : _currentWalkSpeed = _minWalkSpeed;
        _ = s ? _currentRunSpeed = _maxRunSpeed : _currentRunSpeed = _minRunSpeed;
    }

    private void UpdateSound() {
        if (_rigidBody2D.velocity != Vector2.zero) {
            _playerWalkGrass.getPlaybackState(out PLAYBACK_STATE playbackState);
            if (playbackState.Equals(PLAYBACK_STATE.STOPPED)) {
                _playerWalkGrass.start();
            }
        } else {
            _playerWalkGrass.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }


    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.Position = transform.position;
        playerData.LastDirection = new Vector2(_animator.GetFloat(LAST_HORIZONTAL), _animator.GetFloat(LAST_VERTICAL));
    }

    public void LoadPlayer(PlayerData playerData) {
        transform.position = playerData.Position;
        _animator.SetFloat(LAST_HORIZONTAL, playerData.LastDirection.x);
        _animator.SetFloat(LAST_VERTICAL, playerData.LastDirection.y);
    }
    #endregion
}
