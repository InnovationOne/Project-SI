using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// This class represents the character useing an item
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    public static PlayerToolsAndWeaponController LocalInstance { get; private set; }

    // Cached references
    private ResourceNode _lastResourceNode;
    private PlayerMarkerController _playerMarkerController;
    private PlayerToolbeltController _playerToolbeltController;
    private AttackController _playerAttackController;

    // Cached references
    private bool _success;
    private bool _callbackSuccessful;
    private const float MAX_TIMEOUT = 2f;
    private float _timeout;
    private float _elapsedTime;

    // Weapon Action management
    private float _attackCooldown = 0f;
    private const float MEELE_ANGEL_INCREMENT = 10f; // Adjust as needed for precision 

    // Layer Masks for optimized physics queries
    [SerializeField] private LayerMask resourceNodeLayerMask;

    [SerializeField] private LayerMask damageableLayerMask;

    // Cached list to reduce allocations
    private List<Collider2D> _targets = new List<Collider2D>();


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

        HandleAttackCooldown();
        HandleResourceNodeHighlight();
        HandleInput();
    }

    #region Update Handlers

    private void HandleAttackCooldown() {
        if (_attackCooldown > 0f) {
            _attackCooldown -= Time.deltaTime;
        }
    }

    private void HandleResourceNodeHighlight() {
        // Only update highlights when the marked cell position changes
        Vector2 gridPosition = _playerMarkerController.MarkedCellPosition + Vector3.one * 0.5f;

        Collider2D collider2D = Physics2D.OverlapPoint(gridPosition, resourceNodeLayerMask);
        if (collider2D != null) {
            if (collider2D.TryGetComponent(out ResourceNode resourceNode)) {
                _lastResourceNode = resourceNode;
            }
        } else if (_lastResourceNode != null) {
            _lastResourceNode = null;
        }
    }

    private void HandleInput() {
        // When the inventory UI is opened, skip input handling
        if (InventoryMasterVisual.Instance != null && InventoryMasterVisual.Instance.gameObject.activeSelf) {
            return;
        }

        // Cache the currently selected item to reduce repeated lookups
        var selectedItemSlot = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (selectedItemSlot == null) {
            return;
        }

        ItemSO itemSO = ItemManager.Instance.ItemDatabase[selectedItemSlot.ItemId];
        if (itemSO == null) {
            return;
        }

        // Use Unity's new Input System or event-based input for better performance (optional)
        if (Input.GetMouseButtonDown(0)) {
            if (itemSO is WeaponSO weapon) {
                WeaponAction(weapon);
            }

            ToolAction(itemSO);
        }
    }

    #endregion

    #region Weapon Actions

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
        _targets.Clear();
        Vector2 playerPosition = transform.position;
        Vector2 forwardDirection = transform.up;

        for (float angle = -90f; angle <= 90f; angle += MEELE_ANGEL_INCREMENT) {
            float radian = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian)).normalized;
            Vector2 attackPosition = playerPosition + direction * attackRange;

            // Verwende Layer Mask zur Optimierung
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPosition, attackRange / 10f, damageableLayerMask);
            foreach (var hitTarget in hitTargets) {
                if (hitTarget.TryGetComponent<IDamageable>(out var damageable) && hitTarget.GetComponent<Player>() == null) {
                    if (!_targets.Contains(hitTarget)) {
                        _targets.Add(hitTarget);
                    }
                }
            }
        }

        foreach (var target in _targets) {
            if (target.TryGetComponent<IDamageable>(out var damageable)) {
                damageable.TakeDamage(transform.position, DamageToDeal(weaponSO), weaponSO.DamageType);
            }
        }
    }

    private void PerformRangedAttack(WeaponSO weaponSO) {
        Vector2 playerPosition = transform.position;
        Vector2 attackDirection = transform.up;

        RaycastHit2D hit = Physics2D.Raycast(playerPosition, attackDirection, weaponSO.Range, damageableLayerMask);
        if (hit.collider != null && hit.collider.TryGetComponent(out IDamageable damageable)) {
            damageable.TakeDamage(transform.position, DamageToDeal(weaponSO), weaponSO.DamageType);
        }
    }

    private int DamageToDeal(WeaponSO weaponSO) => 
        Random.Range(0, 100) < weaponSO.CritChance ? weaponSO.CritDamage : weaponSO.Damage;

    #endregion

    #region Tool Actions

    private void ToolAction(ItemSO itemSO) {
        // Use an asynchronous method instead of recursive coroutines for better performance and stack safety
        StartCoroutine(HandleToolActions(itemSO.LeftClickAction.GetEnumerator()));
    }

    private IEnumerator HandleToolActions(IEnumerator<ToolActionSO> enumerator) {
        while (enumerator.MoveNext()) {
            ToolActionSO toolAction = enumerator.Current;

            if (toolAction != null) {
                _callbackSuccessful = false;
                _success = false;
                _timeout = MAX_TIMEOUT;
                _elapsedTime = 0f;

                // Execute the tool action
                toolAction.OnApplyToTileMap(_playerMarkerController.MarkedCellPosition, _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot());

                // Wait for the ServerRpc response with a timeout
                while (!_callbackSuccessful && _elapsedTime < _timeout) {
                    yield return null;
                    _elapsedTime += Time.deltaTime;
                }

                if (_success) {
                    continue; // Proceed to the next tool action
                } else {
                    if (_elapsedTime >= _timeout) {
                        Debug.LogError($"{toolAction.name} | ToolAction timeout!");
                    }

                    // Retry the current tool action
                    enumerator.Reset();
                }
            }
        }
    }

    public void ClientCallback(bool success) {
        _callbackSuccessful = true;
        _success = success;
    }

    public void AreaMarkerCallback() {
        _elapsedTime = 0f;
    }
    #endregion
}
