using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the player's tools and weapons, handling usage actions and network synchronization.
/// </summary>
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    public static PlayerToolsAndWeaponController LocalInstance { get; private set; }

    // Cached references
    private PlayerMarkerController _playerMarkerController;
    private PlayerToolbeltController _playerToolbeltController;

    // Timeout settings
    private const float MAX_TIMEOUT = 2f;

    // Layer Masks for optimized physics queries
    [SerializeField] private LayerMask resourceNodeLayerMask;

    // Flags for callback handling
    private bool _success;
    private bool _callbackSuccessful;
    private float _timeout;
    private float _elapsedTime;

    // Delegate to allow dynamic callback handling
    private Action<bool> ClientCallbackDelegate;

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerToolsAndWeaponController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    /// <summary>
    /// Initializes component references and subscribes to input actions.
    /// </summary>
    private void Start() {
        _playerMarkerController = PlayerMarkerController.LocalInstance;
        _playerToolbeltController = PlayerToolbeltController.LocalInstance;

        InputManager.Instance.OnLeftClickAction += HandleLeftClick;
    }

    /// <summary>
    /// Ensures proper cleanup of the singleton instance.
    /// </summary>
    private void OnDestroy() {
        InputManager.Instance.OnLeftClickAction -= HandleLeftClick;
    }

    #region Update Handlers

    /// <summary>
    /// Handles the left click action input.
    /// </summary>
    private void HandleLeftClick() {
        // When the inventory UI is opened, skip input handling
        if (InventoryMasterVisual.Instance != null && InventoryMasterVisual.Instance.gameObject.activeSelf) {
            return;
        }

        var selectedItemSlot = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (selectedItemSlot == null) {
            return;
        }

        ItemSO itemSO = ItemManager.Instance.ItemDatabase[selectedItemSlot.ItemId];
        if (itemSO != null) {
            ExecuteToolActionsAsync(itemSO);
        }
    }

    #endregion

    #region Tool Actions

    /// <summary>
    /// Executes tool actions asynchronously.
    /// </summary>
    /// <param name="itemSO">The item scriptable object containing tool actions.</param>
    private async void ExecuteToolActionsAsync(ItemSO itemSO) {
        foreach (var toolAction in itemSO.LeftClickAction) {
            if (toolAction == null) {
                continue;
            }

            _callbackSuccessful = false;
            _success = false;
            _timeout = MAX_TIMEOUT;
            _elapsedTime = 0f;

            // Execute the tool action
            toolAction.OnApplyToTileMap(_playerMarkerController.MarkedCellPosition, _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot());

            // Wait for the ServerRpc response with a timeout
            bool actionSuccess = await WaitForCallbackAsync();

            if (actionSuccess) {
                continue; // Proceed to the next tool action
            } else {
                Debug.Log($"{toolAction.name} | ToolAction failed or timed out!");
                // Optionally, implement retry logic or user feedback here
            }
        }
    }

    /// <summary>
    /// Waits asynchronously for a callback to be received or a timeout to occur.
    /// </summary>
    /// <returns>True if the action was successful; otherwise, false.</returns>
    private async Task<bool> WaitForCallbackAsync() {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        void CallbackHandler(bool success) {
            _callbackSuccessful = true;
            _success = success;
            taskCompletionSource.SetResult(success);
        }

        // Subscribe to the callback
        Action<bool> originalCallback = ClientCallback;
        ClientCallbackDelegate = CallbackHandler;
        // Assuming there's a way to set the callback, otherwise adjust accordingly

        // Wait for the callback or timeout
        var delayTask = Task.Delay(TimeSpan.FromSeconds(MAX_TIMEOUT));
        var completedTask = await Task.WhenAny(taskCompletionSource.Task, delayTask);

        if (completedTask == taskCompletionSource.Task) {
            return taskCompletionSource.Task.Result;
        } else {
            Debug.Log("ToolAction timed out waiting for callback.");
            return false;
        }
    }

    /// <summary>
    /// Called by the server to notify the client about the action result.
    /// </summary>
    /// <param name="success">Indicates whether the action was successful.</param>
    public void ClientCallback(bool success) {
        ClientCallbackDelegate?.Invoke(success);
    }

    /// <summary>
    /// Callback for area marker events.
    /// </summary>
    public void AreaMarkerCallback() {
        _elapsedTime = 0f;
    }
    #endregion
}
