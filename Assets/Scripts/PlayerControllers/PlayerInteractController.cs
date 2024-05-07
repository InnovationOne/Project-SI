using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Player))]
public class PlayerInteractController : NetworkBehaviour {
    public static PlayerInteractController LocalInstance { get; private set; }

    private const float MAX_INTERACT_DISTANCE = 0.4f;
    private Interactable _lastInteractable;
    private BoxCollider2D _playerCollider;
    private Player _player;


    private void Awake() {
        _playerCollider = GetComponent<BoxCollider2D>();
        _player = GetComponent<Player>();        
    }

    private void Start() {
        InputManager.Instance.OnInteractAction += InputManager_OnInteractAction;
    }

    private new void OnDestroy() {
        InputManager.Instance.OnInteractAction -= InputManager_OnInteractAction;
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
            ProcessInteractionCheck();
            DiscoverInteractables();
        }
    }

    /// <summary>
    /// Handles the interaction action triggered by the input manager.
    /// </summary>
    private void InputManager_OnInteractAction() {
        if (_lastInteractable != null) {
            _lastInteractable.Interact(_player);
        }
    }

    /// <summary>
    /// Processes the interaction check and triggers the interaction with the last interactable object if it is out of the maximum distance to the player.
    /// </summary>
    private void ProcessInteractionCheck() {
        if (_lastInteractable != null && 
            _lastInteractable.MaxDistanceToPlayer > 0f && 
            Vector2.Distance(transform.position, _lastInteractable.transform.position) > _lastInteractable.MaxDistanceToPlayer) {

            _lastInteractable.Interact(_player);
            _lastInteractable = null;
        }
    }

    /// <summary>
    /// Discovers interactable objects within a certain distance from the player.
    /// </summary>
    private void DiscoverInteractables() {
        var boundsCenter = _playerCollider.bounds.center;
        var colliders = Physics2D.OverlapCircleAll(boundsCenter, MAX_INTERACT_DISTANCE);
        var closestCollider = FindClosestInteractable(colliders);

        if (closestCollider != null) {
            _lastInteractable = closestCollider.GetComponent<Interactable>();
        } else {
            _lastInteractable = null;
        }
    }

    /// <summary>
    /// Represents a 2D collider component attached to a game object.
    /// </summary>
    private Collider2D FindClosestInteractable(Collider2D[] colliders) {
        Collider2D closestCollider = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D collider in colliders) {
            if (collider.TryGetComponent<Interactable>(out var interactable)) {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestCollider = collider;
                }
            }
        }

        return closestCollider;
    }
}

