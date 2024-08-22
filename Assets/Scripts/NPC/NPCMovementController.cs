using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCMovementController : MonoBehaviour {
    [SerializeField] private Transform _target;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _turnDst = 0f;
    [SerializeField] private float _stoppingDst = 0f;

    private const float PATH_UPDATE_MOVE_THRESHOLD = 0.5f;
    private const float MIN_PATH_UPDATE_TIME = 0.2f;

    private PathfinderPath _path;
    private Rigidbody2D _rigidBody2D;

    private void Awake() {
        _rigidBody2D = GetComponent<Rigidbody2D>();
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
            }


            yield return null;
        }

        _rigidBody2D.velocity = Vector2.zero;
    }

    /// <summary>
    /// Called by Unity's OnDrawGizmos method to draw the path with Gizmos.
    /// </summary>
    public void OnDrawGizmos() {
        _path?.DrawWithGizmos();
    }
}
