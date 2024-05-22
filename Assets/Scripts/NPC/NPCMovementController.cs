using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCMovementController : MonoBehaviour {
    [SerializeField] private Transform _target;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _turnDst = 0f;
    [SerializeField] private float _stoppingDst = 0f;

    private const string MOVING = "Moving";
    private const string RUNNING = "Running";
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private const string LAST_HORIZONTAL = "LastHorizontal";
    private const string LAST_VERTICAL = "LastVertical";
    private const float MAX_DELAY_FOR_ROTATION = 0.2f;
    private float _currentTimeForPlayerRotation;

    private const float PATH_UPDATE_MOVE_THRESHOLD = 0.5f;
    private const float MIN_PATH_UPDATE_TIME = 0.2f;

    private PathfinderPath _path;
    private Rigidbody2D _rigidBody2D;
    private Animator _animator;
    private Vector2 _lastMotionDirection;

    

    private void Awake() {
        _rigidBody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start() {
        //StartCoroutine(UpdatePath());
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.LeftAlt)) {
            StartCoroutine(UpdatePath());
        }
    }

    /// <summary>
    /// Callback method called when a path is found.
    /// </summary>
    /// <param name="waypoints">The array of waypoints representing the path.</param>
    /// <param name="pathSuccessful">A boolean indicating whether the path was successfully found.</param>
    private void OnPathFound(Vector3[] waypoints, bool pathSuccessful) {
        if (pathSuccessful) {
            _path = new PathfinderPath(waypoints, transform.position, _turnDst, _stoppingDst);

            StopCoroutine(FollowPath());
            StartCoroutine(FollowPath());
        }
    }

    /// <summary>
    /// Supports a simple iteration over a non-generic collection.
    /// </summary>
    /// <remarks>
    /// The <see cref="IEnumerator"/> interface provides a way to iterate over a collection of objects without exposing the underlying structure of the collection. 
    /// It is commonly used in conjunction with the <see cref="IEnumerable"/> interface, which represents a collection that can be enumerated.
    /// </remarks>
    private IEnumerator UpdatePath() {
        if (Time.timeSinceLevelLoad < 0.3f) {
            yield return new WaitForSeconds(0.3f);
        }
        PathRequestManager.RequestPath(new PathRequest(transform.position, _target.position, OnPathFound));

        float sqrMoveThreshold = PATH_UPDATE_MOVE_THRESHOLD * PATH_UPDATE_MOVE_THRESHOLD;
        Vector3 targetPosOld = _target.position;

        while (true) {
            yield return new WaitForSeconds(MIN_PATH_UPDATE_TIME);
            if ((_target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
                PathRequestManager.RequestPath(new PathRequest(transform.position, _target.position, OnPathFound));
                targetPosOld = _target.position;
            }
        }
    }

    /// <summary>
    /// Private IEnumerator method that follows the path set for the NPC.
    /// </summary>
    /// <returns>A coroutine that follows the path.</returns>
    private IEnumerator FollowPath() {
        bool followingPath = true;
        int pathIndex = 0;
        float speedPercent = 1;

        while (followingPath) {
            Vector2 pos2D = new Vector2(transform.position.x, transform.position.y);
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
                    if (speedPercent < 0.01f) {
                        followingPath = false;
                    }
                }

                Vector2 direction = ((Vector2)_path.LookPoints[pathIndex] - pos2D).normalized;
                _rigidBody2D.velocity = _speed * speedPercent * direction;
                UpdateAnimatorParameters(direction);
            }


            yield return null;
        }

        _rigidBody2D.velocity = Vector2.zero;
        UpdateAnimatorParameters(Vector2.zero);
    }

    /// <summary>
    /// Updates the animator parameters based on the input direction.
    /// </summary>
    /// <param name="direction">The direction the NPC is moving towards.</param>
    private void UpdateAnimatorParameters(Vector2 direction) {
        // Set the input direction in the animator
        _animator.SetFloat(HORIZONTAL, direction.x);
        _animator.SetFloat(VERTICAL, direction.y);

        // Save last motion to apply idle rotation
        if (_animator.GetBool(MOVING)) {
            _lastMotionDirection = direction;
            _animator.SetFloat(LAST_HORIZONTAL, direction.x);
            _animator.SetFloat(LAST_VERTICAL, direction.y);

            // Reset the delayForPlayerRotation timer when the player starts moving
            _currentTimeForPlayerRotation = 0;
        } else {
            // Increment the timer when the player is not moving
            _currentTimeForPlayerRotation += Time.deltaTime;

            // Update the rotation after a short delay
            if (_currentTimeForPlayerRotation >= MAX_DELAY_FOR_ROTATION &&
                !(_lastMotionDirection.x == 0 && _lastMotionDirection.y == 0)) {

                _animator.SetFloat(LAST_HORIZONTAL, _lastMotionDirection.x);
                _animator.SetFloat(LAST_VERTICAL, _lastMotionDirection.y);
            }
        }
    }

    /// <summary>
    /// Called by Unity's OnDrawGizmos method to draw the path with Gizmos.
    /// </summary>
    public void OnDrawGizmos() {
        _path?.DrawWithGizmos();
    }
}
