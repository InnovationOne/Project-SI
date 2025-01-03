using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the behavior of item prefabs placed on the map, including animations, movement, and network synchronization.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PickUpItem : NetworkBehaviour {
    const float DEFAULT_PARABOLA_Z = 5f;
    const float DEFAULT_END_POSITION_Z_MULTIPLIER = 0.0001f;

    [Header("Item Movement Settings")]
    [SerializeField] float _itemMoveSpeed = 0.5f;
    [SerializeField] float _itemSpeedAcceleration = 0.01f;
    [SerializeField] float _pickUpDistanceThreshold = 2f;

    [Header("Item Animation Settings")]
    [SerializeField] float _upAndDownSpeed = 1f;
    [SerializeField] float _timeForLoop = 1f;
    [SerializeField] float _upAndDownHeight = 0.01f;
    [SerializeField] float _maxParabolaAnimationTime = 0.4f;
    [SerializeField] float _parabolaAnimationHeight = 0.2f;

    [SerializeField] ItemSlot _itemSlot;
    [SerializeField] SpriteRenderer _itemRenderer;

    float _canPickUpTimer;
    float _currentPickUpTimer;
    float _parabolaAnimationTime;
    float _distanceToPlayer;
    PlayerController _closestPlayer;
    Vector3 _spawnPosition;
    Vector3 _endPosition;
    Vector3 _newPosition;

    // Cached References
    TimeManager _timeManager;
    ItemManager _itemManager;
    DragItemUI _dragItemUI;
    EventsManager _eventsManager;
    GameManager _gameManager;


    void Awake() {
        _parabolaAnimationTime = _maxParabolaAnimationTime;
        _itemSlot ??= new ItemSlot();
    }

    void Start() {
        _timeManager = TimeManager.Instance;
        _itemManager = ItemManager.Instance;
        _dragItemUI = DragItemUI.Instance;
        _eventsManager = EventsManager.Instance;
        _timeManager.OnNextDayStarted += OnNextDay;
        _gameManager = GameManager.Instance;

        _canPickUpTimer = ItemSpawnManager.LEAN_MOVE_TIME;
    }

    new void OnDestroy() {
        _timeManager.OnNextDayStarted -= OnNextDay;
        base.OnDestroy();
    }

    void Update() {
        PerformParabolaAnimation();
        UpdatePickUpTimer();
    }

    void FixedUpdate() {
        PerformFloatingAnimation();
        AttemptPickUp();
    }

    void OnNextDay() => DespawnItemServerRpc();

    [ServerRpc(RequireOwnership = false)]
    void DespawnItemServerRpc() => NetworkObject.Despawn(true);

    #region -------------------- Animation --------------------
    // Moves the item along a parabola for a short duration after it spawns.
    void PerformParabolaAnimation() {
        if (_parabolaAnimationTime >= _maxParabolaAnimationTime) return;
        _parabolaAnimationTime += Time.deltaTime;
        float t = _parabolaAnimationTime / _maxParabolaAnimationTime;
        transform.position = MathParabola.Parabola(_spawnPosition, _endPosition, _parabolaAnimationHeight, t);

    }

    // Creates a gentle floating animation by adjusting the item's vertical position.
    void PerformFloatingAnimation() {
        float yOffset = Mathf.PingPong(Time.time * _upAndDownSpeed, _timeForLoop) * (_upAndDownHeight * 2) - _upAndDownHeight;
        _newPosition = transform.position;
        _newPosition.y += yOffset;
        transform.position = _newPosition;
    }
    #endregion

    #region -------------------- Pick-Up --------------------
    // Counts down to when the item can be picked up, then checks if the player is close enough.
    void UpdatePickUpTimer() {
        _currentPickUpTimer += Time.deltaTime;
        if (_currentPickUpTimer < _canPickUpTimer) return;

        FindClosestPlayer();
        TryImmediatePickUpIfClose();
    }

    // If the item is pick-up ready, close, and the player can carry it, move it towards them.
    void AttemptPickUp() {
        if (_currentPickUpTimer < _canPickUpTimer || _closestPlayer == null || _distanceToPlayer > _pickUpDistanceThreshold) return;
        if (_closestPlayer.TryGetComponent(out PlayerInventoryController inventoryController) && inventoryController.InventoryContainer.CanAddItem(_itemSlot)) {
            MoveItemTowardsPlayerServerRpc();
        }
    }

    // If the closest player is right next to the item, immediately handle collection.
    void TryImmediatePickUpIfClose() {
        if (_closestPlayer == null || _distanceToPlayer >= 0.1f) return;
        if (_closestPlayer.TryGetComponent(out PlayerInventoryController inventoryController) && inventoryController.InventoryContainer.CanAddItem(_itemSlot)) {
            HandleItemCollection(inventoryController);
        }
    }

    // Finds the nearest player to the item. For better performance, consider maintaining a list of player positions.
    void FindClosestPlayer() {
        var playerControllers = _gameManager.PlayerControllers;
        float minDistance = float.MaxValue;
        _closestPlayer = null;

        foreach (var playerController in playerControllers) {
            float distance = Vector3.Distance(transform.position, playerController.transform.position);
            if (distance < minDistance) {
                minDistance = distance;
                _closestPlayer = playerController;
            }
        }

        _distanceToPlayer = minDistance;
    }

    // Adds the item to the player's inventory, triggers event notifications, and removes the item if exhausted.
    void HandleItemCollection(PlayerInventoryController inventoryController) {
        NotifyItemPickedUp();
        AttemptToAddToDragAndDrop();
        if (!_itemSlot.IsEmpty) {
            int remaining = inventoryController.InventoryContainer.AddItem(_itemSlot, false);
            int added = _itemSlot.Amount - remaining;
            _itemSlot.RemoveAmount(added);
        }

        if (_itemSlot.Amount <= 0) {
            Destroy(this.gameObject);
        }
    }

    // Triggers item picked-up events for each item in the stack.
    void NotifyItemPickedUp() {
        for (int i = 0; i < _itemSlot.Amount; i++) {
            _eventsManager.ItemPickedUpEvents.PickedUpItemId(_itemSlot.ItemId);
        }
    }

    // Attempts to add the item to the DragItemUI panel if it's currently active.
    void AttemptToAddToDragAndDrop() {
        if (_dragItemUI.gameObject.activeSelf && _closestPlayer.TryGetComponent(out PlayerItemDragAndDropController dragAndDropController)) {
            int remaining = dragAndDropController.TryToAddItemToDragItem(_itemSlot);
            int added = _itemSlot.Amount - remaining;
            _itemSlot.RemoveAmount(added);
        }
    }
    #endregion -------------------- Pick-Up --------------------

    #region -------------------- Initialization --------------------
    // Sets up the item's data, including its icon, based on the provided slot info.
    public void InitializeItem(ItemSlot itemSlot) {
        _itemSlot.Set(itemSlot);
        _itemRenderer.sprite = _itemManager.ItemDatabase[_itemSlot.ItemId].ItemIcon;
    }

    // Configures the item to start its parabolic "spawn" animation.
    public void StartParabolaAnimation(Vector3 start, Vector3 end) {
        _parabolaAnimationTime = 0f;
        _canPickUpTimer = 0.4f;
        _spawnPosition = start;
        _spawnPosition.z = DEFAULT_PARABOLA_Z;
        _endPosition = end;
        _endPosition.z = end.y * DEFAULT_END_POSITION_Z_MULTIPLIER;
    }

    #endregion -------------------- Initialization --------------------

    [ServerRpc(RequireOwnership = false)]
    private void MoveItemTowardsPlayerServerRpc() => MoveItemTowardsPlayerClientRpc();

    [ClientRpc]
    private void MoveItemTowardsPlayerClientRpc() {
        if (_closestPlayer == null) return;

        // Smoothly move item towards player and gradually accelerate.
        _newPosition = Vector3.MoveTowards(transform.position, _closestPlayer.transform.position, _itemMoveSpeed * Time.deltaTime);
        transform.position = _newPosition;
        _itemMoveSpeed += _itemSpeedAcceleration;
    }
}
