using Newtonsoft.Json;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Represents a chest that players can interact with to store or retrieve items.
public class Chest : PlaceableObject {
    [Header("References")]
    [SerializeField] SpriteRenderer _closedSprite;
    [SerializeField] SpriteRenderer _openedSprite;

    // Maximum distance at which players can interact with this chest.
    public override float MaxDistanceToPlayer => 2f;

    // Tracks if the chest is currently open. Updated by the server, read by clients.
    NetworkVariable<bool> _isOpenNetworked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<ulong> _chestOpenedByClientId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    ItemContainerSO _itemContainer;
    UIManager _uIManager;
    int _itemId;

    string _lastChestState = "";
    bool _isLoading = false;
    bool _isSubscribed = false;

    void OnChestItemsUpdated() {
        if (_isLoading) return;
        var data = SaveObject();
        if (data == _lastChestState) return;
        _lastChestState = data;
        Vector3Int cellPos = Vector3Int.FloorToInt(transform.position - new Vector3(0f, 0.5f, 0f));
        if (PlaceableObjectsManager.Instance != null) {
            PlaceableObjectsManager.Instance.UpdatePlaceableObjectStateServerRpc(new Vector3IntSerializable(cellPos), data);
        }
        UpdateVisual(_isOpenNetworked.Value);
        UIManager.Instance.ChestUI.ShowUIButtonContains();
        //UpdateUI(_isOpenNetworked.Value);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _uIManager = UIManager.Instance;

        _isOpenNetworked.OnValueChanged += (oldValue, newValue) => {
            UpdateVisual(newValue);
            UpdateUI(newValue);
        };

        // Initialize with the current state if it spawns mid-game.
        UpdateVisual(_isOpenNetworked.Value);
        UpdateUI(_isOpenNetworked.Value);
    }

    private void Update() {
        // Auto-close chest when the opener moves too far away.
        if (_isOpenNetworked.Value && _chestOpenedByClientId.Value == NetworkManager.Singleton.LocalClientId) {
            var localPlayer = PlayerController.LocalInstance;
            if (localPlayer != null && Vector3.Distance(transform.position, localPlayer.transform.position) > MaxDistanceToPlayer) {
                if (IsServer) TryToggleChestServer(NetworkManager.Singleton.LocalClientId);
                else RequestToggleChestServerRpc(NetworkManager.Singleton.LocalClientId, localPlayer.transform.position);
            }
        }

        // Check for chest content changes
        if (_itemContainer != null) {
            string currentState = SaveObject();
            if (currentState != _lastChestState) {
                _lastChestState = currentState;
                Vector3Int cellPos = Vector3Int.FloorToInt(transform.position - new Vector3(0f, 0.5f, 0f));
                if (PlaceableObjectsManager.Instance != null) {
                    PlaceableObjectsManager.Instance.UpdatePlaceableObjectStateServerRpc(new Vector3IntSerializable(cellPos), currentState);
                }
            }
        }
    }

    // Ensures the item container exists and is set up with the correct number of slots.
    void InitializeItemContainer() {
        if (_itemContainer != null) return;
        _itemContainer = ScriptableObject.CreateInstance<ItemContainerSO>();
        _itemContainer.Initialize(ChestSO.ItemSlots);
        _itemContainer.MarkAsChestContainer();
        if (!_isSubscribed) {
            _itemContainer.OnItemsUpdated += OnChestItemsUpdated;
            _isSubscribed = true;
        }
    }

    // Sets up the chest data and visuals before it's fully loaded.
    public override void InitializePreLoad(int itemId) {
        _itemId = itemId;
        InitializeItemContainer();
        _closedSprite.enabled = true;
        _openedSprite.enabled = false;
    }

    // Allows the player to interact with the chest, toggling its open/closed state.
    public override void Interact(PlayerController player) {
        if (IsServer) TryToggleChestServer(player.OwnerClientId);
        else RequestToggleChestServerRpc(player.OwnerClientId, player.transform.position);
    }

    // Invoked on server when a client requests to toggle the chest state.
    [ServerRpc(RequireOwnership = false)]
    void RequestToggleChestServerRpc(ulong requestingClientId, Vector3 playerPosition) {
        if (Vector3.Distance(transform.position, playerPosition) <= MaxDistanceToPlayer) {
            TryToggleChestServer(requestingClientId);
        }
    }

    // If the chest is closed, open it; if open, close it. Only toggles if it's not already open by another client.
    void TryToggleChestServer(ulong requestingClientId) {
        if (!_isOpenNetworked.Value) {
            _isOpenNetworked.Value = true;
            _chestOpenedByClientId.Value = requestingClientId;
        } else if (_chestOpenedByClientId.Value == requestingClientId) {
            _isOpenNetworked.Value = false;
            _chestOpenedByClientId.Value = 0;
        }
    }

    // Updates the chest's appearance based on the open state.
    void UpdateVisual(bool isOpen) {
        _closedSprite.enabled = !isOpen;
        _openedSprite.enabled = isOpen;
    }

    // Updates the UI for this chest based on the open state.
    void UpdateUI(bool isOpen) {
        bool chestOpenedByMe = _chestOpenedByClientId.Value == NetworkManager.Singleton.LocalClientId;
        if (isOpen && chestOpenedByMe) _uIManager.OpenChestUI(_itemContainer);
        else _uIManager.CloseChestUI();
    }

    // Grants the player any items still inside the chest.
    public override void PickUpItemsInPlacedObject(PlayerController player) {
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

    // Retrieves the ChestSO data from the global item database using this chest's item ID.
    ChestSO ChestSO => GameManager.Instance.ItemManager.ItemDatabase[_itemId] as ChestSO;

    #region -------------------- Save & Load --------------------

    public override string SaveObject() {
        return _itemContainer.SaveItemContainer();
    }

    public override void LoadObject(string data) {
        if (string.IsNullOrEmpty(data) || data == _lastChestState) return;
        _isLoading = true;
        InitializeItemContainer();

        // Use the raw JSON string directly.
        _itemContainer.LoadItemContainer(data);

        _lastChestState = data;
        _isLoading = false;
    }

    #endregion -------------------- Save & Load --------------------

    public override void InitializePostLoad() { }
}
