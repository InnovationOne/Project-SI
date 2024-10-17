using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the behavior of item prefabs placed on the map, including animations, movement, and network synchronization.
/// </summary>
public class PickUpItem : NetworkBehaviour {
    private const float DEFAULT_CAN_PICK_UP_TIMER = 0.75f; // Sync with ItemSpawnManager.cs
    private const float DEFAULT_PARABOLA_Z = 5f;
    private const float DEFAULT_END_POSITION_Z_MULTIPLIER = 0.0001f;

    [Header("Item Movement Settings")]
    [SerializeField] private float _itemMoveSpeed = 0.5f;
    [SerializeField] private float _itemSpeedAcceleration = 0.01f;
    [SerializeField] private float _pickUpDistanceThreshold = 1.25f;

    [Header("Item Animation Settings")]
    [SerializeField] private float _upAndDownSpeed = 1f;
    [SerializeField] private float _timeForLoop = 1f;
    [SerializeField] private float _upAndDownHeight = 0.01f;
    [SerializeField] private float _maxParabolaAnimationTime = 0.4f;
    [SerializeField] private float _parabolaAnimationHeight = 0.2f;

    [SerializeField] private ItemSlot _itemSlot;
    [SerializeField] private SpriteRenderer _itemRenderer;

    private float _canPickUpTimer = DEFAULT_CAN_PICK_UP_TIMER;
    private float _currentPickUpTimer;
    private float _parabolaAnimationTime;
    private Vector3 _spawnPosition;
    private Vector3 _endPosition;

    private Player _closestPlayer;
    private float _distanceToPlayer;

    // Cached References
    private TimeManager _timeManager;
    private PlayerDataManager _playerDataManager;
    private ItemManager _itemManager;
    private DragItemUI _dragItemUI;
    private EventsManager _eventsManager;

    // Reusable Vector3 to minimize allocations
    private Vector3 _newPosition;


    #region Unity Callbacks

    private void Awake() {
        _parabolaAnimationTime = _maxParabolaAnimationTime;
        _itemSlot = new ItemSlot();

        // Cache singleton references
        _timeManager = TimeManager.Instance;
        _playerDataManager = PlayerDataManager.Instance;
        _itemManager = ItemManager.Instance;
        _dragItemUI = DragItemUI.Instance;
        _eventsManager = EventsManager.Instance;

        if (_timeManager != null) {
            _timeManager.OnNextDayStarted += OnNextDayStarted;
        }
    }

    private new void OnDestroy() {
        if (_timeManager != null) {
            _timeManager.OnNextDayStarted -= OnNextDayStarted;
        }
    }

    /// <summary>
    /// This method is called every frame and is responsible for performing the parabola animation and updating the pick-up timer.
    /// </summary>
    private void Update() {
        PerformParabolaAnimation();
        UpdatePickUpTimer();
    }

    /// <summary>
    /// This method is called every fixed framerate frame. It is used for physics calculations and updates.
    /// </summary>
    private void FixedUpdate() {
        PerformFloatingAnimation();
        AttemptPickUp();
    }

    #endregion

    #region Network Callbacks

    /// <summary>
    /// ServerRpc method that despawns the item.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void DespawnItemServerRpc() {
        NetworkObject.Despawn(true);
    }


    #endregion

    #region Event Handlers

    /// <summary>
    /// Event handler for when the next day starts in the TimeManager.
    /// Despawns the item if the game object is active.
    /// </summary>
    private void OnNextDayStarted() {
        if (gameObject.activeSelf) {
            DespawnItemServerRpc();
        }
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Performs a parabolic animation by updating the position of the transform over time.
    /// </summary>
    private void PerformParabolaAnimation() {
        if (_parabolaAnimationTime < _maxParabolaAnimationTime) {
            _parabolaAnimationTime += Time.deltaTime;
            float t = _parabolaAnimationTime / _maxParabolaAnimationTime;
            transform.position = MathParabola.Parabola(_spawnPosition, _endPosition, _parabolaAnimationHeight, t);
        }
    }

    /// <summary>
    /// Performs a floating animation by moving the object up and down.
    /// </summary>
    private void PerformFloatingAnimation() {
        float yOffset = Mathf.PingPong(Time.time * _upAndDownSpeed, _timeForLoop) * (_upAndDownHeight * 2) - _upAndDownHeight;
        _newPosition = transform.position;
        _newPosition.y += yOffset;
        transform.position = _newPosition;
    }

    #endregion

    #region Pick-Up Methods

    /// <summary>
    /// Updates the pick-up timer and triggers the pick-up action when the timer reaches the specified duration.
    /// </summary>
    private void UpdatePickUpTimer() {
        _currentPickUpTimer += Time.deltaTime;
        if (_currentPickUpTimer >= _canPickUpTimer) {
            CalculateDistanceAndPickUp();
        }
    }

    /// <summary>
    /// Attempts to pick up an item if the conditions are met.
    /// </summary>
    private void AttemptPickUp() {
        if (_currentPickUpTimer >= _canPickUpTimer &&
            _distanceToPlayer <= _pickUpDistanceThreshold &&
            _closestPlayer != null &&
            _closestPlayer.TryGetComponent(out PlayerInventoryController inventoryController) &&
            inventoryController.InventoryContainer.CanAddItem(_itemSlot)) {
            MoveItemTowardsPlayerServerRpc();
        }
    }

    /// <summary>
    /// Calculates the distance to the closest player and picks up the item if the player is within a certain range.
    /// </summary>
    private void CalculateDistanceAndPickUp() {
        FindClosestPlayer();

        if (_closestPlayer == null || _distanceToPlayer >= 0.1f) {
            return;
        }

        if (_closestPlayer.TryGetComponent(out PlayerInventoryController inventoryController) &&
            inventoryController.InventoryContainer.CanAddItem(_itemSlot)) {
            HandleItemCollection(inventoryController);
        }
    }

    /// <summary>
    /// Finds the closest player to the current object.
    /// </summary>
    private void FindClosestPlayer() {
        var minDistance = float.MaxValue;
        _closestPlayer = null;

        foreach (var player in _playerDataManager.CurrentlyConnectedPlayers) {
            var distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance) {
                _closestPlayer = player;
                minDistance = distance;
            }
        }
        _distanceToPlayer = minDistance;
    }

    /// <summary>
    /// Handles the collection of an item by the player.
    /// </summary>
    /// <param name="inventoryController">The player's inventory controller.</param>
    private void HandleItemCollection(PlayerInventoryController inventoryController) {
        NotifyItemPickedUp();

        AttemptToAddToDragAndDrop();

        if (!_itemSlot.IsEmpty) {
            // Attempt to add items to the inventory and get the remaining amount
            int remainingAmount = inventoryController.InventoryContainer.AddItem(_itemSlot, false);

            // Calculate the amount that was successfully added
            int addedAmount = _itemSlot.Amount - remainingAmount;

            // Remove the added amount from the current item slot
            _itemSlot.RemoveAmount(addedAmount);
        }

        if (_itemSlot.Amount <= 0) {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// Notifies that an item has been picked up.
    /// </summary>
    private void NotifyItemPickedUp() {
        for (int i = 0; i < _itemSlot.Amount; i++) {
            _eventsManager.ItemPickedUpEvents.PickedUpItemId(_itemSlot.ItemId);
        }
    }

    /// <summary>
    /// Attempts to add the item to the drag and drop panel.
    /// </summary>
    private void AttemptToAddToDragAndDrop() {
        if (DragItemUI.Instance.gameObject.activeSelf) {
            var dragAndDropController = _closestPlayer.GetComponent<PlayerItemDragAndDropController>();

            // Attempt to add items to the drag-and-drop slot and get the remaining amount
            int remainingAmount = dragAndDropController.TryToAddItemToDragItem(_itemSlot);

            // Calculate the amount that was successfully added
            int addedAmount = _itemSlot.Amount - remainingAmount;

            // Remove the added amount from the current item slot
            _itemSlot.RemoveAmount(addedAmount);
        }
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Initializes the item slot with the provided item slot data.
    /// </summary>
    /// <param name="itemSlot">The item slot data to initialize with.</param>
    public void InitializeItem(ItemSlot itemSlot) {
        _itemSlot.Set(itemSlot);
        if (_itemManager == null) {
            Debug.Log("1");
        }
        if (_itemManager.ItemDatabase == null) {
            Debug.Log("2");
        }
        if (_itemManager.ItemDatabase[_itemSlot.ItemId] == null) {
            Debug.Log("3");
        }
        if (_itemManager.ItemDatabase[_itemSlot.ItemId].ItemIcon == null) {
            Debug.Log("4");
        }
        _itemRenderer.sprite = _itemManager.ItemDatabase[_itemSlot.ItemId].ItemIcon;
    }

    /// <summary>
    /// Starts the parabola animation from the specified start position to the specified end position.
    /// </summary>
    /// <param name="start">The start position of the animation.</param>
    /// <param name="end">The end position of the animation.</param>
    public void StartParabolaAnimation(Vector3 start, Vector3 end) {
        _parabolaAnimationTime = 0f;
        _canPickUpTimer = 0.4f;

        _spawnPosition = start;
        _spawnPosition.z = DEFAULT_PARABOLA_Z;
        _endPosition = end;
        _endPosition.z = end.y * DEFAULT_END_POSITION_Z_MULTIPLIER;
    }

    #endregion





    /// <summary>
    /// Moves the item towards the player on the server and calls the client RPC to move the item towards the player.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void MoveItemTowardsPlayerServerRpc() => MoveItemTowardsPlayerClientRpc();


    /// <summary>
    /// Moves the item towards the closest player's position using a client RPC (Remote Procedure Call).
    /// </summary>
    [ClientRpc]
    private void MoveItemTowardsPlayerClientRpc() {
        if (_closestPlayer == null) {
            return;
        }

        _newPosition = Vector3.MoveTowards(transform.position, _closestPlayer.transform.position, _itemMoveSpeed * Time.deltaTime);
        transform.position = _newPosition;
        _itemMoveSpeed += _itemSpeedAcceleration;
    }
}
