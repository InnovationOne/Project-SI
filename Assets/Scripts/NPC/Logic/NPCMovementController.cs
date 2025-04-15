using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCMovementController : MonoBehaviour {
    [SerializeField] private float _walkSpeed = 1f;
    public float WalkSpeed => _walkSpeed;
    [SerializeField] private float _runSpeed = 3.0f;
    [SerializeField] private float _turnDst = 0.5f;
    [SerializeField] private float _stoppingDst = 0.2f;

    private PathfinderPath _path;
    private Rigidbody2D _rb;
    private Coroutine _followPathCoroutine;
    private Coroutine _updatePathCoroutine;

    private Vector3 _target;
    private bool _hasTarget;
    private bool _isRunning;
    private bool _isMoving;
    private bool _isPausedExternally;

    private const float PATH_UPDATE_MOVE_THRESHOLD = 0.5f;
    private const float MIN_PATH_UPDATE_TIME = 0.2f;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void MoveTo(Vector3 targetPosition, bool run = false) {
        _target = targetPosition;
        _isRunning = run;
        _hasTarget = true;

        if (_updatePathCoroutine != null) StopCoroutine(_updatePathCoroutine);
        _updatePathCoroutine = StartCoroutine(UpdatePath());
    }

    public bool IsMoving() => _isMoving;

    private IEnumerator UpdatePath() {
        if (Time.timeSinceLevelLoad < 0.3f) yield return new WaitForSeconds(0.3f);

        PathRequestManager.RequestPath(new PathRequest(transform.position, _target, OnPathFound));

        float sqrMoveThreshold = PATH_UPDATE_MOVE_THRESHOLD * PATH_UPDATE_MOVE_THRESHOLD;
        Vector3 targetPosOld = _target;

        while (_hasTarget) {
            yield return new WaitForSeconds(MIN_PATH_UPDATE_TIME);

            if ((_target - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
                PathRequestManager.RequestPath(new PathRequest(transform.position, _target, OnPathFound));
                targetPosOld = _target;
            }
        }
    }

    private void OnPathFound(Vector3[] waypoints, bool pathSuccessful) {
        if (!pathSuccessful) return;

        _path = new PathfinderPath(waypoints, transform.position, _turnDst, _stoppingDst);

        if (_followPathCoroutine != null) StopCoroutine(_followPathCoroutine);
        _followPathCoroutine = StartCoroutine(FollowPath());
    }

    private IEnumerator FollowPath() {
        bool followingPath = true;
        int pathIndex = 0;
        _isMoving = true;

        float speedPercent = 1f;
        float speed = _isRunning ? _runSpeed : _walkSpeed;

        while (followingPath) {
            var pos2D = new Vector2(transform.position.x, transform.position.y);

            while (_path.TurnBoundaries[pathIndex].HasCrossedLine(pos2D)) {
                if (pathIndex == _path.FinishLineIndex) {
                    followingPath = false;
                    break;
                } else {
                    pathIndex++;
                }
            }

            if (followingPath) {
                if (pathIndex >= _path.SlowDownIndex && _stoppingDst > 0) {
                    speedPercent = Mathf.Clamp01(_path.TurnBoundaries[pathIndex].DistanceFromPoint(pos2D) / _stoppingDst);
                    if (speedPercent < 0.01f) followingPath = false;
                }

                Vector2 dir = ((Vector2)_path.LookPoints[pathIndex] - pos2D).normalized;

                if (_isPausedExternally) {
                    _rb.linearVelocity = Vector2.zero;
                    yield return null;
                    continue;
                }

                _rb.linearVelocity = speed * speedPercent * dir;
            }

            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;
        _isMoving = false;
    }

    public void PauseMovement(bool state) {
        _isPausedExternally = state;
    }

    private void OnDrawGizmos() {
        _path?.DrawWithGizmos();
    }
}
