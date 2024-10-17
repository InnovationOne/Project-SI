using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour {
    public enum EnemyState { Idle, Roaming, Pursuing, Attacking, Retreating, Hit }

    private EnemyState _currentState = EnemyState.Idle;
    private float _idleToRoamingTimer;
    private Rigidbody2D _rb;
    private EnemySO _enemySO;

    // Roaming
    private Vector2 _targetPosition;
    [SerializeField] private PolygonCollider2D _patrolArea; // Change this later to be set by a spawner

    // Pathfinding
    private const float PATH_UPDATE_MOVE_THRESHOLD = 0.5f;
    private const float MIN_PATH_UPDATE_TIME = 0.2f;
    private PathfinderPath _path;

    // Animation
    private Animator _animator;
    private const string MOVING = "Moving";
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private const string LAST_HORIZONTAL = "LastHorizontal";
    private const string LAST_VERTICAL = "LastVertical";
    private const float MAX_DELAY_FOR_ROTATION = 0.2f;
    private Vector2 _currentDirection;
    private Vector2 _lastMotionDirection;
    private float _currentTimeForRotation;


    private void Awake() {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        //_enemySO = GetComponent<Enemy>().EnemySO;
        _targetPosition = transform.position;
    }

    void Update() {
        switch (_currentState) {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Roaming:
                HandleRoamingState();
                break;
            case EnemyState.Pursuing:
                // Handle pursuing the player
                break;
            case EnemyState.Attacking:
                // Handle attacking
                break;
            case EnemyState.Retreating:
                // Handle retreating
                break;
            case EnemyState.Hit:
                HandleHitState();
                break;
        }

        UpdateAnimatorParameters();
    }


    #region Idle
    private void HandleIdleState() {
        if (_animator.GetBool(MOVING)) {
            _animator.SetBool(MOVING, false);
            _rb.linearVelocity = Vector2.zero;
        }

        _idleToRoamingTimer -= Time.deltaTime;
        if (_idleToRoamingTimer <= 0) {
            ChangeState(EnemyState.Roaming);
            //Debug.Log("Changing to Roaming state");
            //_idleToRoamingTimer = Random.Range(_enemySO.MinIdleTime, _enemySO.MaxIdleTime);;
        }
    }
    #endregion


    #region Roaming
    private void HandleRoamingState() {
        if (_animator.GetBool(MOVING)) {
            return;
        }

        _animator.SetBool(MOVING, true);
        Vector2 potentialTarget = FindPotentialTarget();
        while (!_patrolArea.OverlapPoint(potentialTarget)) {
            potentialTarget = FindPotentialTarget();
        }

        _targetPosition = potentialTarget;
        StartCoroutine(UpdatePath());
    }

    private Vector2 FindPotentialTarget() => new Vector2(
        Random.Range(_patrolArea.bounds.min.x, _patrolArea.bounds.max.x),
        Random.Range(_patrolArea.bounds.min.y, _patrolArea.bounds.max.y));


    #endregion

    #region Pursuing
    // Follow me :)
    #endregion

    #region Attacking
    // ATTACK!!!    
    #endregion

    #region Retreating
    // IDK
    #endregion

    #region Hit
    private void HandleHitState() {
        if (_animator.GetBool(MOVING)) {
            StopCoroutine(FollowPath());
            _rb.linearVelocity = Vector2.zero;
            _animator.SetBool(MOVING, false);
        } else if (_rb.linearVelocity == Vector2.zero) {
            ChangeState(EnemyState.Roaming);
            Debug.Log("Changing to last state");
        }
    }
    #endregion


    #region Pathfinding
    private void OnPathFound(Vector3[] waypoints, bool pathSuccessful) {
        if (pathSuccessful) {
            _path = new PathfinderPath(waypoints, transform.position);

            StartCoroutine(FollowPath());
        }
    }

    private IEnumerator UpdatePath() {
        if (Time.timeSinceLevelLoad < 0.3f) {
            yield return new WaitForSeconds(0.3f);
        }
        PathRequestManager.RequestPath(new PathRequest(transform.position, _targetPosition, OnPathFound));

        float sqrMoveThreshold = PATH_UPDATE_MOVE_THRESHOLD * PATH_UPDATE_MOVE_THRESHOLD;
        Vector2 targetPosOld = _targetPosition;

        while (true) {
            yield return new WaitForSeconds(MIN_PATH_UPDATE_TIME);
            if ((_targetPosition - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
                PathRequestManager.RequestPath(new PathRequest(transform.position, _targetPosition, OnPathFound));
                targetPosOld = _targetPosition;
            }
        }
    }

    private IEnumerator FollowPath() {
        bool followingPath = true;
        int pathIndex = 0;

        while (followingPath) {
            Vector2 pos2d = new Vector2(transform.position.x, transform.position.y);
            while (_path.TurnBoundaries[pathIndex].HasCrossedLine(pos2d)) {
                if (pathIndex == _path.FinishLineIndex) {
                    followingPath = false;
                    ChangeState(EnemyState.Idle);
                    //Debug.Log("Changing to Idle state");
                    break;
                } else {
                    pathIndex++;
                }
            }

            _currentDirection = ((Vector2)_path.LookPoints[pathIndex] - pos2d).normalized;
            //_rb.linearVelocity = _enemySO.Speed * _currentDirection;
            yield return null;
        }
    }
    #endregion


    #region Animation
    private void UpdateAnimatorParameters() {
        _animator.SetFloat(HORIZONTAL, _currentDirection.x);
        _animator.SetFloat(VERTICAL, _currentDirection.y);

        // Save last motion to apply idle rotation
        if (_animator.GetBool(MOVING)) {
            _lastMotionDirection = _currentDirection;
            _animator.SetFloat(LAST_HORIZONTAL, _currentDirection.x);
            _animator.SetFloat(LAST_VERTICAL, _currentDirection.y);

            // Reset the delayForPlayerRotation timer when the player starts moving
            _currentTimeForRotation = 0;
        } else {
            _currentTimeForRotation += Time.deltaTime;

            if (_currentTimeForRotation >= MAX_DELAY_FOR_ROTATION &&
                !(_lastMotionDirection.x == 0 && _lastMotionDirection.y == 0)) {

                _animator.SetFloat(LAST_HORIZONTAL, _lastMotionDirection.x);
                _animator.SetFloat(LAST_VERTICAL, _lastMotionDirection.y);
            }
        }
    }
    #endregion

    public void ChangeState(EnemyState newState) {
        _currentState = newState;
    }

    /// <summary>
    /// Called by Unity's OnDrawGizmos method to draw the path with Gizmos.
    /// </summary>
    public void OnDrawGizmos() {
        _path?.DrawWithGizmos();
    }
}

