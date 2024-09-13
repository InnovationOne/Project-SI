using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// This class represents the character useing an item
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    public static PlayerToolsAndWeaponController LocalInstance { get; private set; }

    private ResourceNode _lastResourceNode;
    private PlayerMarkerController _playerMarkerController;
    private PlayerToolbeltController _playerToolbeltController;
    private AttackController _playerAttackController;

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
    }

    private void Update() {
        if (!IsOwner || _playerMarkerController == null) {
            return;
        }

        _attackCooldown -= Time.deltaTime;

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

        ItemSO itemSO = ItemManager.Instance.ItemDatabase[_playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId];
        if (itemSO == null) {
            return;
        }

        if (Input.GetMouseButtonDown(0)) {
            // Use weapon
            if (itemSO is WeaponSO weapon) {
                WeaponAction(weapon);
            }

            ToolAction(itemSO);
        }
    }

    #region Weapon Actions
    private float _attackCooldown = 0f;
    private void WeaponAction(WeaponSO weaponSO) {
        if (_attackCooldown > 0f) {
            return;
        }

        _attackCooldown = 1f / weaponSO.AttackSpeed;
        switch (weaponSO.AttackType) {
            case WeaponSO.AttackTypes.Melee:
                PerformMeleeAttack(weaponSO);
                
                break;
            case WeaponSO.AttackTypes.Ranged:
            case WeaponSO.AttackTypes.Magic:
                PerformRangedAttack(weaponSO);
                break;
        }
    }

    private void PerformMeleeAttack(WeaponSO weaponSO) {
        float attackRange = weaponSO.Range;
        float angleIncrement = 10f; // Adjust as needed for precision

        List<Collider2D> targets = new List<Collider2D>();
        Vector2 playerPosition = transform.position;
        Vector2 forwardDirection = transform.up;

        for (float angle = -90; angle <= 90; angle += angleIncrement) {
            float radian = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian)) * forwardDirection;
            Vector2 attackPosition = playerPosition + direction * attackRange;

            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPosition, attackRange / 10f);
            foreach (var hitTarget in hitTargets) {
                if (hitTarget.TryGetComponent<IDamageable>(out var damageable) && hitTarget.GetComponent<Player>() == null) {
                    if (!targets.Contains(hitTarget)) {
                        targets.Add(hitTarget);
                    }
                }
            }
        }

        foreach (var target in targets) {
            if (target.TryGetComponent<IDamageable>(out var damageable)) {
                damageable.TakeDamage(transform.position, DamageToDeal(weaponSO), weaponSO.DamageType);
            }
        }
    }

    private void PerformRangedAttack(WeaponSO weaponSO) {
        Vector2 playerPosition = transform.position;
        Vector2 attackDirection = transform.up;

        RaycastHit2D hit = Physics2D.Raycast(playerPosition, attackDirection, weaponSO.Range);
        if (hit.collider != null && hit.collider.TryGetComponent<IDamageable>(out var damageable)) {
            if (hit.collider.GetComponent<Player>() == null) {
                damageable.TakeDamage(transform.position, DamageToDeal(weaponSO), weaponSO.DamageType);
            }
        }
    }

    private int DamageToDeal(WeaponSO weaponSO) => Random.Range(0, 100) < weaponSO.CritChance ? weaponSO.CritDamage : weaponSO.Damage;
    #endregion


    #region Tool Actions
    private void ToolAction(ItemSO itemSO) {
        // Starte den rekursiven Vorgang
        Debug.Log("Tool Action");
        StartToolAction(itemSO.LeftClickAction.GetEnumerator());
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
        toolAction.OnApplyToTileMap(_playerMarkerController.MarkedCellPosition, _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot());

        // Wait for the ServerRpc response
        while (!_callbackSuccessfull && _elapsedTime < _timeout) {
            yield return null;
            _elapsedTime += Time.deltaTime;
        }

        if (!_success) {
            if (_elapsedTime >= _timeout) {
                //Debug.LogError($"{toolAction.name} | ToolAction timeout!");
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
