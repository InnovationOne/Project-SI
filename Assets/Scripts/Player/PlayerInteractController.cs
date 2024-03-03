using Unity.Netcode;
using UnityEngine;

// This class handels interactions of the player with the world (e.g. chest, smelter, gate, animals)
public class PlayerInteractController : NetworkBehaviour {
    public static PlayerInteractController LocalInstance { get; private set; }

    public Interactable LastInteractable { get; private set; }

    [SerializeField] private float _maxInteractDistance = 0.4f;

    private Interactable _lastPossibleInteraction;
    private BoxCollider2D _boxCollider2d;
    private Player _localPlayer;


    private void Awake() {
        _boxCollider2d = GetComponent<BoxCollider2D>();
        _localPlayer = GetComponent<Player>();
    }

    private void Start() {
        InputManager.Instance.OnInteractAction += InputManager_OnInteractAction;
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
        CheckForInteractables();
    }

    // Interact with the collider
    private void InputManager_OnInteractAction() {
        InteractAction();
    }

    private void CheckForInteractables() {
        Collider2D[] collider2DArray = Physics2D.OverlapCircleAll(_boxCollider2d.bounds.center, _maxInteractDistance);

        Collider2D closestCollider = null;
        float closestDistance = float.MaxValue;
        foreach (Collider2D collider in collider2DArray) {
            if (collider.TryGetComponent<Interactable>(out var interact)) {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestCollider = collider;
                }
            }
        }

        UpdatePossibleInteraction(closestCollider);
    }

    private void UpdatePossibleInteraction(Collider2D closestCollider) {
        if (closestCollider != null) {
            // Show the possible interaction e.g. ui or highlight etc.
            if (_lastPossibleInteraction != null && _lastPossibleInteraction.gameObject.GetInstanceID() != closestCollider.gameObject.GetInstanceID()) {
                _lastPossibleInteraction.ShowPossibleInteraction(false);
            }
            _lastPossibleInteraction = closestCollider.GetComponent<Interactable>();
            _lastPossibleInteraction.ShowPossibleInteraction(true);

        } else if (_lastPossibleInteraction != null) {
            // Hide the last possible interaction
            _lastPossibleInteraction.ShowPossibleInteraction(false);
            _lastPossibleInteraction = null;
        }
    }

    public void InteractAction() {
        if (_lastPossibleInteraction != null) {
            _lastPossibleInteraction.Interact(_localPlayer);

            LastInteractable = LastInteractable == null ? _lastPossibleInteraction : null;
        }
    }
}

