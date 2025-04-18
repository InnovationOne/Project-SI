using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a chest that players can interact with to store or retrieve items.
/// The chest maintains its own state (contents) and only commits an update to the server when it is closed.
/// </summary>
public class Chest : PlaceableObject {
    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    // Network variables to control open state and identify the client that opened the chest.
    private readonly NetworkVariable<bool> _isOpenNetworked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<ulong> _chestOpenedByClientId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // The item container holds the chest's contents.
    private ItemContainerSO _itemContainer;
    private ChestSO _chestSO;
    private SpriteRenderer _spriteRenderer;

    // Caches the last committed state. Updates to the server occur only on close.
    private string _lastChestState = "";
    private bool _isSubscribed = false;

    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Subscribe to networked open-state changes for visual and UI updates.
        _isOpenNetworked.OnValueChanged += (oldValue, newValue) => {
            UpdateVisual(newValue);
            UpdateUI(newValue);
        };

        // Initialize visuals with the current state.
        UpdateVisual(_isOpenNetworked.Value);
        UpdateUI(_isOpenNetworked.Value);
    }

    private void Update() {
        // Auto-close chest if the opener has moved too far away.
        if (_isOpenNetworked.Value && _chestOpenedByClientId.Value == NetworkManager.Singleton.LocalClientId) {
            var localPlayer = PlayerController.LocalInstance;
            if (localPlayer != null && Vector3.Distance(transform.position, localPlayer.transform.position) > MaxDistanceToPlayer) {
                if (IsServer) TryToggleChestServer(NetworkManager.Singleton.LocalClientId);
                else RequestToggleChestServerRpc(NetworkManager.Singleton.LocalClientId, localPlayer.transform.position);
            }
        }
    }

    /// <summary>
    /// Initializes the item container if it has not been created already.
    /// </summary>
    private void InitializeItemContainer() {
        if (_itemContainer != null) return;
        _itemContainer = ScriptableObject.CreateInstance<ItemContainerSO>();
        _itemContainer.Initialize(_chestSO.ItemSlotCount);
        _itemContainer.MarkAsChestContainer();
        if (!_isSubscribed) {
            _itemContainer.OnItemsUpdated += OnChestItemsUpdated;
            _isSubscribed = true;
        }
    }

    private void OnChestItemsUpdated() {
        UIManager.Instance.ChestUI.ShowUIButtonContains();
    }

    /// <summary>
    /// Pre-load initialization for the chest.
    /// </summary>
    /// <param name="itemId">The chest’s item identifier.</param>
    public override void InitializePreLoad(int itemId) {
        _chestSO = ItemManager.Instance.ItemDatabase[itemId] as ChestSO;
        _spriteRenderer.sprite = _chestSO.ClosedSprite;
        InitializeItemContainer();
    }

    /// <summary>
    /// Called when a player interacts with the chest.
    /// This toggles the chest's open/close state.
    /// </summary>
    public override void Interact(PlayerController player) {
        if (IsServer) TryToggleChestServer(player.OwnerClientId);
        else RequestToggleChestServerRpc(player.OwnerClientId, player.transform.position);
    }


    /// <summary>
    /// RPC called on the server to handle chest toggle requests.
    /// </summary>
    /// <param name="requestingClientId">ID of the client requesting the toggle.</param>
    /// <param name="playerPosition">Position of the player.</param>
    [ServerRpc(RequireOwnership = false)]
    private void RequestToggleChestServerRpc(ulong requestingClientId, Vector3 playerPosition) {
        if (Vector3.Distance(transform.position, playerPosition) <= MaxDistanceToPlayer) {
            TryToggleChestServer(requestingClientId);
        }
    }

    /// <summary>
    /// Toggles the chest state on the server.
    /// </summary>
    /// <param name="requestingClientId">ID of the requesting client.</param>
    private void TryToggleChestServer(ulong requestingClientId) {
        if (!_isOpenNetworked.Value) {
            // Open the chest.
            _isOpenNetworked.Value = true;
            _chestOpenedByClientId.Value = requestingClientId;
        } else if (_chestOpenedByClientId.Value == requestingClientId) {
            // Close the chest and commit its current state.
            _isOpenNetworked.Value = false;
            _chestOpenedByClientId.Value = 0;
            CommitChestState();
        }
    }

    /// <summary>
    /// Commits the chest's current state by calling the manager's update RPC.
    /// This method is invoked only when the chest is closed.
    /// </summary>
    private void CommitChestState() {
        if (_itemContainer != null) {
            string currentState = SaveObject();
            if (currentState == _lastChestState) return;

            _lastChestState = currentState;
            PlaceableObjectsManager.Instance.UpdateObjectStateServerRpc(NetworkObject.NetworkObjectId, currentState);            
        }
    }

    /// <summary>
    /// Updates the chest's visual appearance based on whether it is open.
    /// </summary>
    /// <param name="isOpen">True if the chest is open; otherwise, false.</param>
    private void UpdateVisual(bool isOpen) {
        if (isOpen) _spriteRenderer.sprite = _chestSO.OpenedSprite;
        else _spriteRenderer.sprite = _chestSO.ClosedSprite;
    }

    /// <summary>
    /// Updates the chest's UI based on the open state.
    /// </summary>
    /// <param name="isOpen">True if the chest is open; otherwise, false.</param>
    private void UpdateUI(bool isOpen) {
        if (isOpen) {
            PlaceableObjectsManager.Instance.RequestObjectStateServerRpc(NetworkObject.NetworkObjectId, "OpenUI");
        } else {
            UIManager.Instance.CloseChestUI();
        }
    }

    /// <summary>
    /// Transfers items from the chest to the player's inventory.
    /// Any remaining items that cannot be added are spawned in the world.
    /// </summary>
    /// <param name="player">The player picking up the items.</param>
    public override void PickUpItemsInPlacedObject(PlayerController player) {
        PlaceableObjectsManager.Instance.RequestObjectStateServerRpc(NetworkObject.NetworkObjectId, "PickUpItems");
    }

    public override void OnStateReceivedCallback(string callbackName) {
        switch (callbackName) {
            case "OpenUI":
                UIManager.Instance.OpenChestUI(_itemContainer);
                break;
            case "PickUpItems":
                PickUpItems();
                break;
        }
    }

    private void PickUpItems() {
        foreach (var slot in _itemContainer.ItemSlots) {
            int leftover = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(slot, false);
            if (leftover > 0) {
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    new ItemSlot(slot.ItemId, leftover, slot.RarityId),
                    transform.position,
                    PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    #region -------------------- Save & Load --------------------

    /// <summary>
    /// Serializes the chest's current item container state to a JSON string.
    /// </summary>
    /// <returns>JSON string representing the chest's state.</returns>
    public override string SaveObject() {
        return _itemContainer.SaveItemContainer();
    }

    /// <summary>
    /// Loads the chest's state from the given JSON string.
    /// </summary>
    /// <param name="data">JSON string containing the chest's state.</param>
    public override void LoadObject(string data) {
        if (string.IsNullOrEmpty(data) || data == _lastChestState) return;
        InitializeItemContainer();

        // Load the item container state using the provided JSON.
        _itemContainer.LoadItemContainer(data);

        _lastChestState = data;
    }

    #endregion -------------------- Save & Load --------------------

    public override void InitializePostLoad() { }
}
