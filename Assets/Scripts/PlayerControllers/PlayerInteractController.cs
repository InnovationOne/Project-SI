using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerInteractController : MonoBehaviour {
    // Defines how far from the player an object can be for interaction checks.
    const float MAX_INTERACT_DISTANCE = 1.0f;

    // Dot product threshold to decide if the interactable is in the player's forward-facing direction.
    // Adjust this value as needed (1.0 = exact same direction, -1.0 = exact opposite).
    const float DIRECTION_DOT_THRESHHOLD = 0.7f; // 45 degrees
        

    // References to essential components and managers.
    BoxCollider2D _playerCollider;
    PlayerController _player;
    PlayerMovementController _playerMovementController;
    InputManager _inputManager;

    // Tracks the closest interactable object currently in range and facing direction.
    IInteractable _currentInteractable;

    // Initializes component references when the script instance is being loaded.
    void Awake() {
        _playerCollider = GetComponent<BoxCollider2D>();
        _player = GetComponent<PlayerController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
    }

    // Subscribes to user input events once the object is initialized.
    void Start() {
        _inputManager = GameManager.Instance.InputManager;
        _inputManager.OnInteractAction += HandleInteractAction;
    }

    // Unsubscribes from input events to avoid memory leaks or errors when the object is destroyed.
    void OnDestroy() {
        if (_inputManager != null) _inputManager.OnInteractAction -= HandleInteractAction;
    }

    // Continuously checks the distance to the currently targeted interactable.
    void Update() {
        CheckInteractionDistance();
    }

    // Triggered by the interact button; finds and interacts with the closest valid interactable.
    void HandleInteractAction() {
        FindClosestInteractable();
        _currentInteractable?.Interact(_player);
    }


    // Removes the interactable target if it moves out of valid range.
    void CheckInteractionDistance() {
        if (_currentInteractable == null || _currentInteractable.MaxDistanceToPlayer <= 0f) return;

        // Attempt to retrieve the transform from the interactable via its Component inheritance.
        var interactableComponent = _currentInteractable as Component;
        if (interactableComponent == null) {
            _currentInteractable = null;
            return;
        }

        float distanceSqr = (transform.position - interactableComponent.transform.position).sqrMagnitude;
        float maxAllowedSqr = _currentInteractable.MaxDistanceToPlayer * _currentInteractable.MaxDistanceToPlayer;

        // If player is too far, clear the current interactable.
        if (distanceSqr > maxAllowedSqr) _currentInteractable = null;
    }

    // Finds the closest interactable object within the maximum interaction distance.
    void FindClosestInteractable() {
        Vector2 center = _playerCollider.bounds.center;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, MAX_INTERACT_DISTANCE);

        IInteractable closest = null;
        float closestSqrDistance = float.MaxValue;
        Vector2 playerPos = transform.position;
        Vector2 facingDirection = _playerMovementController.LastMotionDirection.normalized;

        foreach (var col in colliders) {
            // Attempt to retrieve an interactable from the collider or its parent.
            IInteractable interactable = col.GetComponent<IInteractable>() ?? col.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            // Attempt to get a transform reference from the interactable.
            var interactableComp = interactable as Component;
            if (interactableComp == null) continue;

            // Skip if the interactable is not in front of the player.
            Vector2 playerToObj = (Vector2)interactableComp.transform.position - playerPos;
            float dot = Vector2.Dot(playerToObj.normalized, facingDirection);
            if (dot < DIRECTION_DOT_THRESHHOLD) continue;

            // Keep track of whichever interactable is closest to the player.
            float sqrDist = playerToObj.sqrMagnitude;
            if (sqrDist < closestSqrDistance) {
                closestSqrDistance = sqrDist;
                closest = interactable;
            }
        }

        _currentInteractable = closest;
    }

    // Stops or re-triggers interaction by calling Interact() again and then clearing the reference.
    public void StopInteract() {
        _currentInteractable?.Interact(_player);
        _currentInteractable = null;
    }
}

