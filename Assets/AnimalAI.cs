using UnityEngine;

public enum AnimalStateMachine {
    Moving,
    Waiting,
}


public class AnimalAI: MonoBehaviour {
    [Header("Debugging - Move Settings")]
    [SerializeField] private float _minMoveDistance = 1f;
    [SerializeField] private float _maxMoveDistance = 5f;
    [SerializeField] private float _moveSpeed = 1f;
    [SerializeField] private BoxCollider2D _triggerBoxCollider2D;
    private const float REACHED_POSITION_DISTANCE = 0.5f;
    private bool _collision = false;

    [Header("Debugging - Waiting Setting")]
    [SerializeField] private float _minWaitTime = 5f;
    [SerializeField] private float _maxWaitTime = 10f;
    private float _currentTime;
    private float _currentTimeToWait = 5f;

    private AnimalStateMachine _currentState = AnimalStateMachine.Waiting;
    private Vector2 _targetPosition;
    

    private void Update() {
        switch (_currentState) {
            case AnimalStateMachine.Moving:
                transform.position = Vector2.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);
                if (Vector2.Distance(transform.position, _targetPosition) < REACHED_POSITION_DISTANCE || _collision) {
                    _currentState = AnimalStateMachine.Waiting;
                    _currentTimeToWait = Random.Range(_minWaitTime, _maxWaitTime);
                    _collision = false;
                }
                break;
            case AnimalStateMachine.Waiting:
                _currentTime += Time.deltaTime;
                if (_currentTime >= _currentTimeToWait) {
                    _currentTime = 0f;
                    _currentState = AnimalStateMachine.Moving;
                    _targetPosition = transform.position
                        + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * (int)Random.Range(_minMoveDistance, _maxMoveDistance);
                }
                break;
            default:
                Debug.LogError("Animal is in an unvalid state");
                break;
        }
    }
}
