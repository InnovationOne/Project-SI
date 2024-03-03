using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// This class represents the character useing an item
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    public static PlayerToolsAndWeaponController LocalInstance { get; private set; }

    private ResourceNode _lastResourceNode;
    private PlayerMarkerController _playerMarkerController;
    private PlayerToolbeltController _playerToolbeltController;
    private AttackController _playerAttackController;
    private PlayerMarkerController _playerMakerController;

    private bool _success;
    private bool _callbackSuccessfull;
    private const float _maxTimeout = 2f;
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

    private void Start() {
        _playerMarkerController = PlayerMarkerController.LocalInstance;
        _playerToolbeltController = PlayerToolbeltController.LocalInstance;
        _playerAttackController = GetComponent<AttackController>();
        _playerMakerController = PlayerMarkerController.LocalInstance;
    }

    private void Update() {
        if (!IsOwner || _playerMarkerController == null) {
            return;
        }

        // Show the ResourceNode Highlight
        Vector2 gridPosition = new(_playerMarkerController.MarkedCellPosition.x + 0.5f, _playerMarkerController.MarkedCellPosition.y + 0.5f);
        Collider2D collider2D = Physics2D.OverlapPoint(gridPosition);
        if (collider2D != null) {
            // Show the possible interaction e.g. ui or highlight etc.
            if (_lastResourceNode != null && _lastResourceNode.gameObject.GetInstanceID() != collider2D.gameObject.GetInstanceID()) {
                _lastResourceNode.ShowPossibleInteraction(false);
            }
            if (collider2D.TryGetComponent(out _lastResourceNode)) {
                _lastResourceNode.ShowPossibleInteraction(true);
            }
        } else if (_lastResourceNode != null) {
            // Hide the last possible interaction
            _lastResourceNode.ShowPossibleInteraction(false);
            _lastResourceNode = null;
        }
        // ------------

        // When the inventory UI is opened.
        if (InventoryMasterVisual.Instance.gameObject.activeSelf) {
            return;
        }

        ItemSO itemSO = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().Item;
        if (itemSO == null) {
            return;
        }

        if (Input.GetMouseButtonDown(0)) {
            // Use weapon
            if (itemSO.IsWeapon) {
                WeaponAction(itemSO);
            }

            ToolAction(itemSO);
        }
    }

    private void WeaponAction(ItemSO itemSO) {
        _playerAttackController.Attack(itemSO.Damage, new Vector2(_playerMarkerController.MarkedCellPosition.x + 0.5f, _playerMarkerController.MarkedCellPosition.y + 0.5f));
    }

    #region Tool Actions
    private void ToolAction(ItemSO itemSO) {
        // Starte den rekursiven Vorgang
        StartToolAction(itemSO.OnGridAction.GetEnumerator());
    }

    private void StartToolAction(IEnumerator<ToolActionSO> enumerator) {
        // Check for the next ToolAction
        if (enumerator.MoveNext()) {
            ToolActionSO toolAction = enumerator.Current;
            if (toolAction != null) {
                _callbackSuccessfull = false;
                _success = false;
                _timeout = _maxTimeout;
                _elapsedTime = 0f;
                StartCoroutine(PerformToolAction(toolAction, enumerator));
            }
        }
    }

    private IEnumerator PerformToolAction(ToolActionSO toolAction, IEnumerator<ToolActionSO> enumerator) {
        // Execute the ToolAction
        toolAction.OnApplyToTileMap(_playerMakerController.MarkedCellPosition, _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot());

        // Wait for the ServerRpc response
        while (!_callbackSuccessfull && _elapsedTime < _timeout) {
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
        _callbackSuccessfull = true;
        _success = success;
    }

    public void AreaMarkerCallback() {
        _elapsedTime = 0f;
    }
    #endregion
}
