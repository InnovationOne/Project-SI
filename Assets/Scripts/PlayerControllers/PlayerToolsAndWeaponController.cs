using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// This class represents the character useing an item
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    PlayerMarkerController _playerMarkerController;
    PlayerToolbeltController _playerToolbeltController;
    InputManager _inputManager;
    InventoryMasterUI _inventoryMasterUI;
    ItemManager _itemManager;

    const float MAX_TIMEOUT = 2f;

    bool _success;
    bool _callbackSuccessful;
    float _timeout;
    float _elapsedTime;

    void Start() {
        _playerMarkerController = GetComponent<PlayerMarkerController>();
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _itemManager = ItemManager.Instance;
        _inventoryMasterUI = InventoryMasterUI.Instance;

        _inputManager = InputManager.Instance;
        _inputManager.OnLeftClickAction += HandleLeftClick;
    }

    void OnDestroy() {
        _inputManager.OnLeftClickAction -= HandleLeftClick;
    }

    void HandleLeftClick() {
        if (_inventoryMasterUI.gameObject.activeSelf) return;

        var selectedItemSlot = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (selectedItemSlot == null) return;

        var itemSO = _itemManager.ItemDatabase[selectedItemSlot.ItemId];
        if (itemSO == null) return;

        ToolAction(itemSO);
    }

    void ToolAction(ItemSO itemSO) {
        var actionEnumerator = itemSO.LeftClickAction.GetEnumerator();
        StartToolAction(actionEnumerator);
    }

    void StartToolAction(IEnumerator<ToolActionSO> enumerator) {
        // Attempt to move to the next tool action
        if (!enumerator.MoveNext()) return;

        var toolAction = enumerator.Current;
        if (toolAction == null) return;

        // Reset callback flags and start the coroutine for this action
        _callbackSuccessful = false;
        _success = false;
        _timeout = MAX_TIMEOUT;
        _elapsedTime = 0f;

        StartCoroutine(PerformToolAction(toolAction, enumerator));
    }

    private IEnumerator PerformToolAction(ToolActionSO toolAction, IEnumerator<ToolActionSO> enumerator) {
        // Apply the tool action to the tile map at the player's marked position
        toolAction.OnApplyToTileMap(_playerMarkerController.MarkedCellPosition, _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot());

        // Wait until the server callback is received or we time out
        while (!_callbackSuccessful && _elapsedTime < _timeout) {
            yield return null;
            _elapsedTime += Time.deltaTime;
        }

        // If not successful, log and try the next action
        if (!_success) {
            if (_elapsedTime >= _timeout) {
                Debug.LogError($"{toolAction.name} | ToolAction timeout!");
            }

            // Proceed to the next action regardless of success
            StartToolAction(enumerator);
        }
    }

    [ClientRpc]
    public void ClientCallbackClientRpc(bool success) {
        _callbackSuccessful = true;
        _success = success;
    }

    [ClientRpc]
    public void AreaMarkerCallbackClientRpc() {
        _elapsedTime = 0f;
    }
}