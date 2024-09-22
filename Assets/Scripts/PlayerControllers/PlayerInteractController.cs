using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Player))]
public class PlayerInteractController : NetworkBehaviour {
    public static PlayerInteractController LocalInstance { get; private set; }

    // Maximum distance within which interactions can occur
    private const float MAX_INTERACT_DISTANCE = 0.4f;

    // Cached references
    private Interactable _currentInteractable;
    private BoxCollider2D _playerCollider;
    private Player _player;

    // Preallocated buffer for non-allocating physics queries
    private static readonly Collider2D[] _interactablesBuffer = new Collider2D[10]; // Adjust size as needed

    private void Awake() {
        _playerCollider = GetComponent<BoxCollider2D>();
        _player = GetComponent<Player>();
    }

    private void Start() {
        InputManager.Instance.OnInteractAction += HandleInteractAction;
    }

    private new void OnDestroy() {
        // Ensure event is unsubscribed to prevent memory leaks
        if (InputManager.Instance != null) {
            InputManager.Instance.OnInteractAction -= HandleInteractAction;
        }

        base.OnDestroy();
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerInteractController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    private void Update() {
        if (IsOwner) {
            CheckInteractionDistance();
        }
    }

    /// <summary>
    /// Handles the interaction action triggered by the input manager.
    /// </summary>
    private void HandleInteractAction() {
        FindClosestInteractable();

        if (_currentInteractable != null) {
            _currentInteractable.Interact(_player);
        }
    }

    /// <summary>
    /// Checks if the current interactable is still within the allowed interaction distance.
    /// If not, it triggers the interaction and clears the reference.
    /// </summary>
    private void CheckInteractionDistance() {
        if (_currentInteractable == null || _currentInteractable.MaxDistanceToPlayer <= 0f) {
            return;
        }

        Vector2 playerPosition = transform.position;
        Vector2 interactablePosition = _currentInteractable.transform.position;
        float sqrDistance = (playerPosition - interactablePosition).sqrMagnitude;
        float sqrMaxDistance = _currentInteractable.MaxDistanceToPlayer * _currentInteractable.MaxDistanceToPlayer;

        if (sqrDistance > sqrMaxDistance) {
            _currentInteractable.Interact(_player);
            _currentInteractable = null;
        }
    }

    /// <summary>
    /// Finds the closest interactable object within the maximum interaction distance.
    /// Utilizes non-allocating physics queries to enhance performance.
    /// </summary>
    private void FindClosestInteractable() {
        Vector2 center = _playerCollider.bounds.center;
        float radius = MAX_INTERACT_DISTANCE;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
        Interactable closestInteractable = null;
        float closestSqrDistance = float.MaxValue;
        Vector2 playerPosition = transform.position;

        foreach (Collider2D collider in colliders) {
            if (collider == null) {
                continue;
            }

            // Attempt to get Interactable component from the collider or its parent
            if (!collider.TryGetComponent(out Interactable interactable)) {
                interactable = collider.GetComponentInParent<Interactable>();
            }

            if (interactable == null) {
                continue;
            }

            // Calculate squared distance to avoid unnecessary square root computation
            Vector2 interactablePosition = interactable.transform.position;
            float sqrDistance = (playerPosition - interactablePosition).sqrMagnitude;

            if (sqrDistance < closestSqrDistance) {
                closestSqrDistance = sqrDistance;
                closestInteractable = interactable;
            }
        }

        _currentInteractable = closestInteractable;
    }

    // Optional: Visualize the interaction radius in the Unity Editor
    private void OnDrawGizmosSelected() {
        if (_playerCollider != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_playerCollider.bounds.center, MAX_INTERACT_DISTANCE);
        }
    }
}

