using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerInteractController : NetworkBehaviour {
    // Maximum distance within which interactions can occur
    private const float MAX_INTERACT_DISTANCE = 0.4f;

    // Cached references
    private IInteractable _currentIInteractable;
    private BoxCollider2D _playerCollider;
    private PlayerController _player;

    private new void OnDestroy() {
        InputManager.Instance.OnInteractAction -= HandleInteractAction;

        base.OnDestroy();
    }

    public override void OnNetworkSpawn() {

        InputManager.Instance.OnInteractAction += HandleInteractAction;

        _playerCollider = GetComponent<BoxCollider2D>();
        _player = GetComponent<PlayerController>();

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
        _currentIInteractable?.Interact(_player);
    }

    /// <summary>
    /// Checks if the current interactable is still within the allowed interaction distance.
    /// If not, it triggers the interaction and clears the reference.
    /// </summary>
    private void CheckInteractionDistance() {
        if (_currentIInteractable == null || _currentIInteractable.MaxDistanceToPlayer <= 0f) {
            return;
        }

        Vector2 playerPosition = transform.position;
        Vector2 interactablePosition = ((Component)_currentIInteractable).transform.position;
        float sqrDistance = (playerPosition - interactablePosition).sqrMagnitude;
        float sqrMaxDistance = _currentIInteractable.MaxDistanceToPlayer * _currentIInteractable.MaxDistanceToPlayer;

        if (sqrDistance > sqrMaxDistance) {
            _currentIInteractable.Interact(_player);
            _currentIInteractable = null;
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
        IInteractable closestInteractable = null;
        float closestSqrDistance = float.MaxValue;
        Vector2 playerPosition = transform.position;

        foreach (Collider2D collider in colliders) {
            if (collider == null) {
                continue;
            }

            // Attempt to get Interactable component from the collider or its parent
            if (!collider.TryGetComponent(out IInteractable interactable)) {
                interactable = collider.GetComponentInParent<IInteractable>();
            }

            if (interactable == null) {
                continue;
            }

            // Calculate squared distance to avoid unnecessary square root computation
            Vector2 interactablePosition = ((Component)interactable).transform.position;
            float sqrDistance = (playerPosition - interactablePosition).sqrMagnitude;

            if (sqrDistance < closestSqrDistance) {
                closestSqrDistance = sqrDistance;
                closestInteractable = interactable;
            }
        }

        _currentIInteractable = closestInteractable;
    }
}

