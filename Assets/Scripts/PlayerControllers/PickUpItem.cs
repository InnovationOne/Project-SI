using Unity.Netcode;
using UnityEngine;

// This script handels the item prefab that is a item on the map
public class PickUpItem : NetworkBehaviour {
    [Header("Item Movement Settings")]
    [SerializeField] private float _itemMoveSpeed = 0.2f;
    [SerializeField] private float _itemSpeedAcceleration = 0.01f;
    [SerializeField] private float _pickUpDistanceThreshold = 1.25f;

    [Header("Item Animation Settings")]
    [SerializeField] private float _upAndDownSpeed = 1f;
    [SerializeField] private float _timeForLoop = 1f;
    [SerializeField] private float _upAndDownHeight = 0.01f;
    [SerializeField] private float _maxParabolaAnimationTime = 0.4f;
    [SerializeField] private float _parabolaAnimationHeight = 0.2f;

    private float _canPickUpTimer = 0.75f; // When changing the time also change the time in ItemSpawnManager.cs
    private float _currentPickUpTimer;
    private float _parabolaAnimationTime;
    private Vector3 _spawnPosition, _endPosition;
    private ItemSlot _itemSlot;
    private Player _closestPlayer;
    private float _distanceToPlayer;
    private SpriteRenderer _itemRenderer;

    private void Awake() {
        _itemRenderer = GetComponentInChildren<SpriteRenderer>();
        _parabolaAnimationTime = _maxParabolaAnimationTime;
        _itemSlot = new ItemSlot();
    }

    private void Start() => TimeAndWeatherManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;


    private new void OnDestroy() => TimeAndWeatherManager.Instance.OnNextDayStarted -= TimeAndWeatherManager_OnNextDayStarted;


    /// <summary>
    /// Event handler for when the next day starts in the TimeAndWeatherManager.
    /// Despawns the item if the game object is not null.
    /// </summary>
    private void TimeAndWeatherManager_OnNextDayStarted() {
        if (gameObject != null) {
            DespawnItemServerRpc();
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
        transform.position = Vector3.MoveTowards(transform.position, _closestPlayer.transform.position, _itemMoveSpeed);
        _itemMoveSpeed += _itemSpeedAcceleration;
    }

    /// <summary>
    /// Performs a parabolic animation by updating the position of the transform over time.
    /// </summary>
    private void PerformParabolaAnimation() {
        if (_parabolaAnimationTime < _maxParabolaAnimationTime) {
            _parabolaAnimationTime += Time.deltaTime;
            transform.position = MathParabola.Parabola(_spawnPosition, _endPosition, _parabolaAnimationHeight, _parabolaAnimationTime / _maxParabolaAnimationTime);
        }
    }

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
    /// Performs a floating animation by moving the object up and down.
    /// </summary>
    private void PerformFloatingAnimation() {
        float yPos = Mathf.PingPong(Time.time * _upAndDownSpeed, _timeForLoop) * (_upAndDownHeight * 2) - _upAndDownHeight;
        transform.position = new Vector3(transform.position.x, transform.position.y + yPos, transform.position.z);
    }

    /// <summary>
    /// Attempts to pick up an item if the conditions are met.
    /// </summary>
    private void AttemptPickUp() {
        if (_currentPickUpTimer >= _canPickUpTimer &&
            _distanceToPlayer <= _pickUpDistanceThreshold &&
            _closestPlayer != null &&
            _closestPlayer.GetComponent<PlayerInventoryController>().InventoryContainer.CanAddItem(_itemSlot)) {
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

        var inventoryController = _closestPlayer.GetComponent<PlayerInventoryController>();
        bool canAddItem = inventoryController.InventoryContainer.CanAddItem(_itemSlot);

        if (canAddItem) {
            HandleItemCollection(inventoryController);
        }
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
    }


    /// <summary>
    /// Notifies that an item has been picked up.
    /// </summary>
    private void NotifyItemPickedUp() {
        for (int i = 0; i < _itemSlot.Amount; i++) {
            EventsManager.Instance.ItemPickedUpEvents.PickedUpItemId(_itemSlot.ItemId);
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

    /// <summary>
    /// ServerRpc method that despawns the item.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void DespawnItemServerRpc() => GetComponent<NetworkObject>().Despawn(true);


    /// <summary>
    /// Finds the closest player to the current object.
    /// </summary>
    private void FindClosestPlayer() {
        var distanceToClosestPlayer = float.MaxValue;
        foreach (var player in PlayerDataManager.Instance.CurrentlyConnectedPlayers) {
            var distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < distanceToClosestPlayer) {
                _closestPlayer = player;
                distanceToClosestPlayer = distance;
            }
        }
        _distanceToPlayer = distanceToClosestPlayer;
    }

    /// <summary>
    /// Initializes the item slot with the provided item slot data.
    /// </summary>
    /// <param name="itemSlot">The item slot data to initialize with.</param>
    public void InitializeItem(ItemSlot itemSlot) {
        _itemSlot.Set(itemSlot);
        _itemRenderer.sprite = ItemManager.Instance.ItemDatabase[_itemSlot.ItemId].ItemIcon;
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
        _spawnPosition.z = 5f;
        _endPosition = end;
        _endPosition.z = _endPosition.y * 0.0001f;
    }
}
