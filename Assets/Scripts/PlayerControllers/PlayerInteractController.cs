using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerInteractController : NetworkBehaviour {
    const float MAX_INTERACT_DISTANCE = 0.4f;

    Component _currentInteractable;
    BoxCollider2D _playerCollider;
    PlayerController _player;
    InputManager _inputManager;

    void Awake() {
        _playerCollider = GetComponent<BoxCollider2D>();
        _player = GetComponent<PlayerController>();
    }

    void Start() {
        _inputManager = GameManager.Instance.InputManager;
        _inputManager.OnInteractAction += HandleInteractAction;
    }

    void OnDestroy() {
        _inputManager.OnInteractAction -= HandleInteractAction;
        base.OnDestroy();
    }

    void Update() {
        if (IsOwner) {
            CheckInteractionDistance();
        }
    }

    // Called when the player presses the interact button.
    void HandleInteractAction() {
        RequestInteractServerRpc();
    }

    // Server finds the closest interactable and triggers interaction.
    [ServerRpc(RequireOwnership = false)]
    void RequestInteractServerRpc(ServerRpcParams serverRpcParams = default) {
        FindClosestInteractable();
        if (_currentInteractable != null) {
            _currentInteractable.GetComponent<IInteractable>()?.Interact(_player);
        }
    }

    // Checks if the current interactable is too far; if yes, interact and clear it.
    void CheckInteractionDistance() {
        if (_currentInteractable == null || _currentInteractable.GetComponent<IInteractable>().MaxDistanceToPlayer <= 0f) return;

        Vector2 playerPosition = transform.position;
        Vector2 interactablePosition = (_currentInteractable).transform.position;

        float sqrDistance = (playerPosition - interactablePosition).sqrMagnitude;
        float allowedSqrDist = _currentInteractable.GetComponent<IInteractable>().MaxDistanceToPlayer * _currentInteractable.GetComponent<IInteractable>().MaxDistanceToPlayer;

        if (sqrDistance > allowedSqrDist) {
            _currentInteractable.GetComponent<IInteractable>().Interact(_player);
            _currentInteractable = null;
        }
    }

    // Finds the closest interactable object within the maximum interaction distance.
    void FindClosestInteractable() {
        Vector2 center = _playerCollider.bounds.center;
        var colliders = Physics2D.OverlapCircleAll(center, MAX_INTERACT_DISTANCE);

        IInteractable closest = null;
        float closestSqrDistance = float.MaxValue;
        Vector2 playerPos = transform.position;

        foreach (var collider in colliders) {
            // Attempt to retrieve the IInteractable component from the collider or its parent.
            if (!collider.TryGetComponent<IInteractable>(out var interactable)) {
                interactable = collider.GetComponentInParent<IInteractable>();
            }

            if (interactable == null) continue;

            Vector2 interactablePos = ((Component)interactable).transform.position;
            float sqrDist = (playerPos - interactablePos).sqrMagnitude;

            if (sqrDist < closestSqrDistance) {
                closestSqrDistance = sqrDist;
                closest = interactable;
            }
        }

        _currentInteractable = (Component)closest;
    }

    public void ReInteract() {
        _currentInteractable.GetComponent<IInteractable>()?.Interact(_player);
        _currentInteractable = null;
    }
}

