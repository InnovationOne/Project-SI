using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour {
    public enum EnemyState {
        Idle,
        Roaming,
        Pursuing,
        Searching,
        Attacking,
        Retreating,
        Hit,
        Stunned,
        Blocking,
        Death
    }

    const string IDLE = "Idle";
    const string WALK = "Walk";
    const string RUN = "Run";
    const string ATTACK = "Attack";
    const string DEATH = "Death";
    const string HIT = "Hit";

    const string X_AXIS = "xAxis";
    const string Y_AXIS = "yAxis";
    const string LAST_X_AXIS = "lastXAxis";
    const string LAST_Y_AXIS = "lastYAxis";

    readonly Dictionary<EnemyState, string> EnemyStateToAnimation = new() {
        { EnemyState.Idle, IDLE },
        { EnemyState.Roaming, WALK },
        { EnemyState.Pursuing, RUN },
        { EnemyState.Searching, RUN },
        { EnemyState.Attacking, ATTACK },
        { EnemyState.Retreating, RUN },
        { EnemyState.Hit, HIT },
        { EnemyState.Stunned, "Stunned" },
        { EnemyState.Blocking, "Block" },
        { EnemyState.Death, DEATH }
    };

    [Header("AI References")]
    [SerializeField] private EnemySO _enemySO;
    [SerializeField] private PolygonCollider2D _spawnArea; // TODO: Change this later to be set by a spawner
    [SerializeField] private float _minIdleDuration = 2f;
    [SerializeField] private float _maxIdleDuration = 5f;

    [Header("Roaming Settings")]
    [SerializeField] private float _minRoamingDistance = 2f;

    private CircleCollider2D _pursueRangeCollider;

    [Tooltip("Walls / obstacles")]
    [SerializeField] private LayerMask _obstacleMask;

    private EnemyState _currentState = EnemyState.Idle;
    private float _idleTimer;

    private Rigidbody2D _rb;
    private Animator _anim;
    private Enemy _enemy;

    // Movement / Pfadfinding
    private Vector2 _currentTarget;
    private bool _followingPath;
    private Vector3[] _path;
    private int _pathIndex;

    // Player-Referenz
    private Transform _playerTransform;
    private PlayerMovementController _playerMovementController;
    private bool _playerInRange;
    private Vector2 _lastKnownPlayerPos;

    // Animation
    Vector2 _currentDirection = Vector2.left;
    Vector2 _lastDirection;

    float _attackTimer;

    // Blocking
    private bool _isBlocking;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _enemy = GetComponent<Enemy>();

        _pursueRangeCollider = gameObject.AddComponent<CircleCollider2D>();
    }

    private void Start() {
        ChangeState(EnemyState.Idle);
        _idleTimer = Random.Range(_minIdleDuration, _maxIdleDuration);

        if (_enemySO != null) {
            _pursueRangeCollider.isTrigger = true;
            _pursueRangeCollider.radius = _enemySO.PursueRange;
        }
    }

    void Update() {
        if (_enemy == null) return;

        if (_attackTimer > 0) _attackTimer -= Time.deltaTime;
        

        switch (_currentState) {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Roaming:
                HandleRoamingState();
                break;
            case EnemyState.Pursuing:
                HandlePursuingState();
                break;
            case EnemyState.Searching:
                HandleSearchingState();
                break;
            case EnemyState.Attacking:
                // Wait for attack animation / or projectiles
                break;
            case EnemyState.Retreating:
                HandleRetreatingState();
                break;
            case EnemyState.Hit:
                // E.g. stay in this state for a short time until anim is finished.
                break;
            case EnemyState.Stunned:
                // No movements
                break;
            case EnemyState.Blocking:
                // maybe block animation, just wait here
                break;
        }

        UpdateAnimator();
    }

    private void HandleIdleState() {
        _idleTimer -= Time.deltaTime;
        StopMovement();

        if (_idleTimer <= 0) {
            _idleTimer = Random.Range(_minIdleDuration, _maxIdleDuration);
            ChangeState(EnemyState.Roaming);
        }
    }
    private void HandleRoamingState() {
        if (_currentTarget == Vector2.zero) {
            _currentTarget = GetRandomPointInSpawnArea();
            RequestPathTo(_currentTarget);
        }
        FollowPathOrArrival(EnemyState.Idle);
    }

    private void HandlePursuingState() {
        // 1) Check if player is still in range
        if (_playerTransform == null) {
            // No player transform => switch to searching, run to last known position
            ChangeState(EnemyState.Searching);
            return;
        }

        // 2) Check Retreat Condition
        if (_enemy.CurrentHealth < _enemySO.Health * 0.1f) {
            ChangeState(EnemyState.Retreating);
            return;
        }

        // 3) Check direct line of sight
        if (HasLineOfSight(_playerTransform.position)) {
            if (Vector2.Distance(_lastKnownPlayerPos, _playerTransform.position) > 0.2f) {
                _lastKnownPlayerPos = _playerTransform.position;
                RequestPathTo(_playerTransform.position);
            }
        } else {
            ChangeState(EnemyState.Searching);
            return;
        }

        // 4) Distance check -> Attack
        float dist = Vector2.Distance(transform.position, _playerTransform.position);
        if (dist <= _enemySO.AttackRange * 0.75f) {
            StopMovement();
            if (ShouldBlock()) {
                ChangeState(EnemyState.Blocking);
                StartCoroutine(DoBlockThenAttack());
                return;
            } else {
                StartCoroutine(DoAttack());
                return;
            }
        }

        // 5) Path follow
        FollowPathOrArrival(EnemyState.Idle);
    }

    private void HandleSearchingState() {
        if (_playerTransform != null && HasLineOfSight(_playerTransform.position)) {
            ChangeState(EnemyState.Pursuing);
            return;
        }

        if (_currentTarget == Vector2.zero) {
            if (_lastKnownPlayerPos == Vector2.zero) {
                ChangeState(EnemyState.Roaming);
                return;
            }

            RequestPathTo(_lastKnownPlayerPos);
        }

        if (_followingPath) {
            if (_pathIndex >= _path.Length) {
                _followingPath = false;
                StartCoroutine(DoSearchSpin());
            } else {
                FollowPathOrArrival(EnemyState.Idle);
            }
        }
    }

    private IEnumerator DoSearchSpin() {
        float spinTime = 2f;
        float elapsed = 0f;
        while (elapsed < spinTime) {
            elapsed += Time.deltaTime;

            if (_playerTransform != null && HasLineOfSight(_playerTransform.position)) {
                ChangeState(EnemyState.Pursuing);
                yield break;
            }
            yield return null;
        }

        _currentTarget = Vector2.zero;
        ChangeState(EnemyState.Roaming);
    }

    private IEnumerator DoAttack() {
        if (_attackTimer > 0f) yield break;

        ChangeState(EnemyState.Attacking);

        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length / 2);

        _enemy.Attack(_playerTransform != null ? _playerTransform.position : transform.position);

        // Wait anim-cooldown
        _attackTimer = 1 / _enemySO.AttackSpeed;

        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length / 2);

        ChangeState(EnemyState.Pursuing);
    }

    private IEnumerator DoBlockThenAttack() {
        _isBlocking = true;
        yield return new WaitForSeconds(1f);
        _isBlocking = false;

        yield return StartCoroutine(DoAttack());
    }

    private void HandleRetreatingState() {
        // Run away from the player
        if (_playerTransform != null) {
            Vector2 dir = (transform.position - _playerTransform.position).normalized;
            Vector2 retreatPos = (Vector2)transform.position + dir * 3f;
            RequestPathTo(retreatPos);
        } else {
            ChangeState(EnemyState.Idle);
            return;
        }

        FollowPathOrArrival(EnemyState.Idle);

        // If we are far enough away, switch to Idle
        if (_playerTransform != null) {
            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist > _enemySO.PursueRange * 1.5f) {
                ChangeState(EnemyState.Idle);
                // TODO: Regen hp or something
            }
        }
    }

    public void OnHitState() {
        ChangeState(EnemyState.Hit);
        StartCoroutine(HitRecover());
    }

    private IEnumerator HitRecover() {
        yield return new WaitForSeconds(0.5f);
        if (_playerInRange && _playerTransform != null) {
            ChangeState(EnemyState.Pursuing);
        } else {
            ChangeState(EnemyState.Idle);
        }
    }

    public void OnStunned(float duration) {
        ChangeState(EnemyState.Stunned);
        StopMovement();
        StartCoroutine(StunTimer(duration));
    }

    private IEnumerator StunTimer(float t) {
        yield return new WaitForSeconds(t);
        if (_playerInRange && _playerTransform != null) {
            ChangeState(EnemyState.Pursuing);
        } else {
            ChangeState(EnemyState.Idle);
        }
    }

    public bool IsBlocking() {
        return _isBlocking;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            _playerInRange = true;
            _playerTransform = other.transform;
            _playerMovementController = other.gameObject.GetComponent<PlayerMovementController>();

            if (_currentState == EnemyState.Idle || _currentState == EnemyState.Roaming || _currentState == EnemyState.Searching) {
                ChangeState(EnemyState.Pursuing);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            _playerInRange = false;
            if (_currentState == EnemyState.Pursuing) {
                ChangeState(EnemyState.Searching);
            }
            _playerTransform = null;
        }
    }

    private bool HasLineOfSight(Vector3 targetPos) {
        Vector2 dir = (targetPos - transform.position).normalized;
        float dist = Vector2.Distance(transform.position, targetPos);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, _obstacleMask);
        return hit.collider == null;
    }

    private void RequestPathTo(Vector2 targetPos) {
        _currentTarget = targetPos;
        _followingPath = false;
        PathRequestManager.RequestPath(new PathRequest(transform.position, targetPos, OnPathFound));
    }

    private void OnPathFound(Vector3[] newPath, bool success) {
        if (!success || newPath.Length == 0) {
            if (_currentState == EnemyState.Pursuing) {
                ChangeState(EnemyState.Searching);
            } else {
                ChangeState(EnemyState.Idle);
            }
            return;
        }
        _path = newPath;
        _pathIndex = 0;
        _followingPath = true;
    }

    private void FollowPathOrArrival(EnemyState fallbackState) {
        if (!_followingPath || _path == null) return;
        if (_pathIndex >= _path.Length) {
            _followingPath = false;
            _currentTarget = Vector2.zero;
            ChangeState(fallbackState);
            return;
        }
        Vector3 nextWaypoint = _path[_pathIndex];
        Vector2 dir = (nextWaypoint - transform.position).normalized;
        _rb.linearVelocity = _enemySO.Speed * dir;

        if (Vector2.Distance(transform.position, nextWaypoint) < 0.2f) {
            _pathIndex++;
        }
    }

    private void StopMovement() {
        _rb.linearVelocity = Vector2.zero;
    }

    private Vector2 GetRandomPointInSpawnArea() {
        if (_spawnArea == null) return transform.position;
        Bounds b = _spawnArea.bounds;
        Vector2 candidate;
        int tries = 0;
        do {
            candidate = new Vector2(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y)
            );
            tries++;
            if (tries > 20) break;
        } while (!_spawnArea.OverlapPoint(candidate) || Vector2.Distance(candidate, transform.position) < _minRoamingDistance);
        return candidate;
    }

    private bool ShouldBlock() {
        return Random.value < 0.0f;
    }

    private void UpdateAnimator() {
        Vector2 newInputDirection = _rb.linearVelocity.normalized;
        bool directionChanged = newInputDirection != _currentDirection;

        if (directionChanged) {
            _currentDirection = newInputDirection;
            _anim.SetFloat(X_AXIS, _currentDirection.x);
            _anim.SetFloat(Y_AXIS, _currentDirection.y);

            bool isMoving = _currentDirection != Vector2.zero;

            if (isMoving) {
                _lastDirection = _currentDirection;
                _anim.SetFloat(LAST_X_AXIS, _currentDirection.x);
                _anim.SetFloat(LAST_Y_AXIS, _currentDirection.y);
            } else {
                _anim.SetFloat(LAST_X_AXIS, _lastDirection.x);
                _anim.SetFloat(LAST_Y_AXIS, _lastDirection.y);
            }
        }
    }

    public void ChangeState(EnemyState newState) {
        if (_currentState == newState) return;
        _currentState = newState;
        if (EnemyStateToAnimation.ContainsKey(newState)) {
            _anim.Play(EnemyStateToAnimation[newState]);
        }
    }

    public EnemyState GetState() {
        return _currentState;
    }

    private void OnDrawGizmosSelected() {
        // Draw Pursue-Range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _enemySO != null ? _enemySO.PursueRange : 1f);

        // Draw AttackRange
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _enemySO != null ? _enemySO.AttackRange : 1f);
    }
}