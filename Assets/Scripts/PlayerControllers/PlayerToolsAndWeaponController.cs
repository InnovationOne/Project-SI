using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// This class represents the character useing an item
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    public static PlayerToolsAndWeaponController LocalInstance { get; private set; }

    // Cached references
    private PlayerMarkerController _playerMarkerController;
    private PlayerToolbeltController _playerToolbeltController;
    private PlayerMarkerController _playerMakerController;

    // Timeout settings
    private const float MAX_TIMEOUT = 2f;

    // Flags for callback handling
    private bool _success;
    private bool _callbackSuccessful;
    private float _timeout;
    private float _elapsedTime;


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
        _playerMakerController = PlayerMarkerController.LocalInstance;

        InputManager.Instance.OnLeftClickAction += HandleLeftClick;
    }

    /// <summary>
    /// Ensures proper cleanup of the singleton instance.
    /// </summary>
    private void OnDestroy() {
        InputManager.Instance.OnLeftClickAction -= HandleLeftClick;
    }

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
            ToolAction(itemSO);
        }
    }

    #region Tool Actions

    private void ToolAction(ItemSO itemSO) {
        // Starte den rekursiven Vorgang
        StartToolAction(itemSO.LeftClickAction.GetEnumerator());
    }

    private void StartToolAction(IEnumerator<ToolActionSO> enumerator) {
        // Check for the next ToolAction
        if (enumerator.MoveNext()) {
            ToolActionSO toolAction = enumerator.Current;
            if (toolAction != null) {
                _callbackSuccessful = false;
                _success = false;
                _timeout = MAX_TIMEOUT;
                _elapsedTime = 0f;
                StartCoroutine(PerformToolAction(toolAction, enumerator));
            }
        }
    }

    private IEnumerator PerformToolAction(ToolActionSO toolAction, IEnumerator<ToolActionSO> enumerator) {
        // Execute the ToolAction
        toolAction.OnApplyToTileMap(_playerMakerController.MarkedCellPosition, _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot());

        // Wait for the ServerRpc response
        while (!_callbackSuccessful && _elapsedTime < _timeout) {
            yield return null;
            _elapsedTime += Time.deltaTime;
        }

        if (!_success) {
            if (_elapsedTime >= _timeout) {
                Debug.LogError($"{toolAction.name} | ToolAction timeout!");
            }
            // Recursilve call when tool action was unsuccessful
            StartToolAction(enumerator);
        }
    }

    public void ClientCallback(bool success) {
        // Callback from the ServerRpc
        _callbackSuccessful = true;
        _success = success;
    }

    public void AreaMarkerCallback() {
        _elapsedTime = 0f;
    }
    #endregion
}