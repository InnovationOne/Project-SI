using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Represents a chest that players can interact with to store or retrieve items.
public class Chest : PlaceableObject {
    [Header("References")]
    [SerializeField] SpriteRenderer _closedSprite;
    [SerializeField] SpriteRenderer _openedSprite;

    // Maximum distance at which players can interact with this chest.
    public override float MaxDistanceToPlayer => 1.5f;

    // Tracks if the chest is currently open. Updated by the server, read by clients.
    NetworkVariable<bool> _isOpenNetworked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<ulong> _chestOpenedByClientId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    ItemContainerSO _itemContainer;
    ChestUI _chestUI;
    int _itemId;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _chestUI = ChestUI.Instance;
        // Whenever _isOpenNetworked changes, update the visuals/UI accordingly.
        _isOpenNetworked.OnValueChanged += (oldValue, newValue) => {
            UpdateVisual(newValue);
            UpdateUI(newValue);
        };

        // Initialize with the current state if it spawns mid-game.
        UpdateVisual(_isOpenNetworked.Value);
        UpdateUI(_isOpenNetworked.Value);
    }

    // Ensures the item container exists and is set up with the correct number of slots.
    void InitializeItemContainer() {
        if (_itemContainer != null) return; 
        _itemContainer = ScriptableObject.CreateInstance<ItemContainerSO>(); 
        _itemContainer.Initialize(ChestSO.ItemSlots);
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
        Debug.Log("Interacting with chest");
        // If we are the server (or host), we can directly toggle. Otherwise, request it.
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
        bool isCurrentlyOpen = _isOpenNetworked.Value;

        // If the chest is closed, open it and mark the opener
        if (!isCurrentlyOpen) {
            _isOpenNetworked.Value = true;
            _chestOpenedByClientId.Value = requestingClientId;
        } else {
            // If it's already open, ensure only the client who opened it can close it
            if (_chestOpenedByClientId.Value == requestingClientId) {
                _isOpenNetworked.Value = false;
                _chestOpenedByClientId.Value = 0;
            }
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
        if (isOpen && chestOpenedByMe) _chestUI.ShowChestUI(_itemContainer);
        else _chestUI.HideChestUI();
    }

    // Grants the player any items still inside the chest.
    public override void PickUpItemsInPlacedObject(PlayerController player) {
        foreach (var slot in _itemContainer.ItemSlots) {
            int remainingAmount = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(slot, false);
            if (remainingAmount > 0) {
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    slot,
                    transform.position,
                    PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    // Retrieves the ChestSO data from the global item database using this chest's item ID.
    ChestSO ChestSO => GameManager.Instance.ItemManager.ItemDatabase[_itemId] as ChestSO;

    #region -------------------- Save & Load --------------------

    public override FixedString4096Bytes SaveObject() {
        return new FixedString4096Bytes(_itemContainer.SaveItemContainer());
    }

    public override void LoadObject(FixedString4096Bytes data) {
        string jsonData = data.ToString();

        if (!string.IsNullOrEmpty(jsonData)) {
            InitializeItemContainer();
            _itemContainer.LoadItemContainer(jsonData);
        }
    }

    #endregion -------------------- Save & Load --------------------

    public override void InitializePostLoad() { }
}
