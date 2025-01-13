using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// This class represents the character using an item
[RequireComponent(typeof(NetworkObject))]
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    PlayerMarkerController _playerMarkerController;
    PlayerToolbeltController _playerToolbeltController;
    PlayerController _pC;
    PlayerHealthAndEnergyController _pHaEC;
    PlayerMovementController _pMC;
    InputManager _inputManager;
    InventoryMasterUI _inventoryMasterUI;
    ItemManager _itemManager;
    [SerializeField] Animator _anim;

    const float MAX_TIMEOUT = 2f;

    bool _success;
    bool _callbackSuccessful;
    float _timeout;
    float _elapsedTime;

    float _attackTimer;
    float _specialAttackTimer;
    float _heavyAttackChargeTime;

    float _comboTimer;
    int _comboIndex;

    private ContactFilter2D _contactFilter;
    private Collider2D[] _overlapResults = new Collider2D[10];
    [SerializeField] private LayerMask _enemyLayerMask;

    void Start() {
        _pC = GetComponent<PlayerController>();
        _playerMarkerController = GetComponent<PlayerMarkerController>();
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _pHaEC = GetComponent<PlayerHealthAndEnergyController>();
        _pMC = GetComponent<PlayerMovementController>();
        _itemManager = GameManager.Instance.ItemManager;
        _inventoryMasterUI = InventoryMasterUI.Instance;

        _inputManager = GameManager.Instance.InputManager;
        _inputManager.OnLeftClickAction += HandleLeftClick;
        _inputManager.OnRightClickAction += HandleRightClick;
        _inputManager.OnLeftClickStarted += HandleLeftClickStarted;
        _inputManager.OnLeftClickCanceled += HandleLeftClickCanceled;
        _inputManager.OnSpecialComboAction += HandleSpecialComboAction;

        _contactFilter.useTriggers = true;
        _contactFilter.SetLayerMask(_enemyLayerMask);
        _contactFilter.useLayerMask = true;
    }

    new void OnDestroy() {
        _inputManager.OnLeftClickAction -= HandleLeftClick;
        base.OnDestroy();
    }

    private void Update() {
        if (_attackTimer > 0) _attackTimer -= Time.deltaTime;
        if (_specialAttackTimer > 0) _specialAttackTimer -= Time.deltaTime;
        if (_heavyAttackChargeTime > 0) _heavyAttackChargeTime -= Time.deltaTime;
    }

    bool CanAttack() {
        return _attackTimer <= 0f && AttackableState();
    }

    bool AttackableState() {
        return _pMC.ActivePlayerState == PlayerMovementController.PlayerState.Idle ||
               _pMC.ActivePlayerState == PlayerMovementController.PlayerState.Walking ||
               _pMC.ActivePlayerState == PlayerMovementController.PlayerState.Running;
    }

    void HandleLeftClick() {
        var itemSO = GetItemSO();
        if (itemSO == null) return;

        if (itemSO.ItemType == ItemSO.ItemTypes.Weapons) {
            WeaponAction(itemSO as WeaponSO);
        }

        ToolAction(itemSO);
    }

    #region -------------------- Weapon Action --------------------
    void WeaponAction(WeaponSO weaponSO) {
        if (!CanAttack()) return;

        // Combo-Window check
        if (Time.time - _comboTimer > weaponSO.ComboMaxDelay) {
            _comboIndex = 0;
        }
        _comboIndex++;
        if (_comboIndex > weaponSO.ComboMaxCount) {
            _comboIndex = 0;
        }
        _comboTimer = Time.time;

        // Start attack
        Debug.Log($"[Player Attack] ComboIndex = {_comboIndex}/{weaponSO.ComboMaxCount}");

        _pHaEC.AdjustEnergy(-weaponSO.LightAttackEnergyCost);

        _attackTimer = 1f / weaponSO.LightAttackSpeed;
        StartCoroutine(PerformMeleeHit(weaponSO, WeaponSO.AttackMode.Light, _comboIndex));
    }

    void HandleLeftClickStarted() {
        var weaponSO = GetItemSO() as WeaponSO;
        if (!CanAttack() || weaponSO == null) return;

        Debug.Log("Holding LMB: Charging heavy attack!");

        _heavyAttackChargeTime = weaponSO.HeavyAttackChargeTime;
    }

    void HandleLeftClickCanceled() {
        var weaponSO = GetItemSO() as WeaponSO;
        if (!CanAttack() || weaponSO == null || _heavyAttackChargeTime > 0) return;

        Debug.Log("Releasing LMB: Heavy attack triggered!");

        _pHaEC.AdjustEnergy(-weaponSO.HeavyAttackEnergyCost);
        _attackTimer = 1f / weaponSO.HeavyAttackSpeed;
        StartCoroutine(PerformMeleeHit(weaponSO, WeaponSO.AttackMode.Heavy, 0));
    }

    void HandleRightClick() {
        var weaponSO = GetItemSO() as WeaponSO;
        if (!CanAttack() || weaponSO == null) return;
        StopMovement();

        // Start blocking
        Debug.Log("Player is blocking!");
        var previousPlayerState = _pMC.ActivePlayerState;
        _pMC.ChangeState(PlayerMovementController.PlayerState.Blocking);
        _pHaEC.AdjustEnergy(-weaponSO.BlockEnergyCost);

        _pMC.ChangeState(previousPlayerState, true);
        StartMovement();
    }

    void HandleSpecialComboAction() {
        return;

        if (!AttackableState()) return;
        StopMovement();

        Debug.Log("LMB + RMB gleichzeitig gedrückt: SPECIAL ATTACK!");
        //_pC.ChangeState(PlayerController.PlayerState.Attacking);

        // Here you could, for example, call up a vortex attack, area attack, etc.
        // -> TODO: WeaponAction( specialComboWeaponSO ) or something similar.

        //_pC.ChangeState(PlayerController.PlayerState.Idle);
        StartMovement();
    }

    IEnumerator PerformMeleeHit(WeaponSO weapon, WeaponSO.AttackMode mode, int comboIndex) {
        StopMovement();
        var previousPlayerState = _pMC.ActivePlayerState;
        _pMC.ChangeState(PlayerMovementController.PlayerState.Attacking);
        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length / 2);

        // 1) Hole dir die korrekten Polygondaten
        int arrayIndex = Mathf.Clamp(comboIndex, 0, 999);

        var listOfPoints = (mode == WeaponSO.AttackMode.Light)
            ? weapon.ComboPointsLightAttack
            : weapon.ComboPointsHeavyAttack;

        if (listOfPoints == null || listOfPoints.Count == 0) {
            Debug.LogError($"No {mode} polygon data set in weaponSO!");
            yield break;
        }
        if (arrayIndex >= listOfPoints.Count) {
            arrayIndex = listOfPoints.Count - 1; // Fallback
        }

        var polygonLocalPoints = listOfPoints[arrayIndex];
        if (polygonLocalPoints.Points == null || polygonLocalPoints.Points.Length < 3) {
            Debug.LogError($"Polygon data for comboIndex {comboIndex} is invalid!");
            yield break;
        }

        // 2) Erstelle ein temporäres Objekt mit PolygonCollider2D
        var tempHitbox = new GameObject("TempMeleeHitbox");
        tempHitbox.transform.position = transform.position;

        // Offset für die Richtung des Spielers (Testing)
        if (_pMC.LastMotionDirection == new Vector2(0, 1)) {
            tempHitbox.transform.position += new Vector3(0, -0.20835f, 0);
        } else if (_pMC.LastMotionDirection == new Vector2(1, 0) || _pMC.LastMotionDirection == new Vector2(-1, 0)) {
            tempHitbox.transform.position += new Vector3(0.04167f, 0.04167f, 0);
        }

        // Bestimme Rotation basierend auf LastMotionDirection:
        Vector2 lookDir = _pMC.LastMotionDirection;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        tempHitbox.transform.rotation = Quaternion.Euler(0, 0, angle + 90);

        var polyCollider = tempHitbox.AddComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;
        // Pfad setzen. Hier interpretieren wir polygonLocalPoints als “lokale” Koordinaten (um (0,0)).
        // => es rotiert das gesamte tempHitbox-Objekt, wodurch sich die localPoints mitdrehen.
        polyCollider.SetPath(0, polygonLocalPoints.Points);

        // 3) OverlapCollider

        int hitCount = Physics2D.OverlapCollider(polyCollider, _contactFilter, _overlapResults);
        if (hitCount > 0) {
            // Schaden definieren
            int dmg = (mode == WeaponSO.AttackMode.Light) ? weapon.LightAttackDamage : weapon.HeavyAttackDamage;
            WeaponSO.DamageTypes dmgType = weapon.DamageType;

            for (int i = 0; i < hitCount; i++) {
                Collider2D col = _overlapResults[i];

                // Prüfen, ob der Collider ein BoxCollider2D ist
                if (col is BoxCollider2D && col.TryGetComponent<IDamageable>(out var target)) {
                    target.TakeDamage(transform.position, dmg, dmgType, mode);
                    Debug.Log($"Hit {col.name} for {dmg} damage");
                }

                // Den Eintrag zurücksetzen, um die Liste aufzuräumen
                _overlapResults[i] = null;
            }
        }

        // 4) Zerstöre das temporäre Objekt
        Destroy(tempHitbox);

        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length / 2);

        _pMC.ChangeState(previousPlayerState, true);

        StartMovement();
    }
    #endregion -------------------- Weapon Action --------------------

    #region -------------------- Tool Action --------------------
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

    IEnumerator PerformToolAction(ToolActionSO toolAction, IEnumerator<ToolActionSO> enumerator) {
        StopMovement();

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

        StartMovement();
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
    #endregion -------------------- Tool Action --------------------

    void StopMovement() {
        _pMC.SetCanMoveAndTurn(false);
    }

    void StartMovement() {
        _pMC.SetCanMoveAndTurn(true);
    }

    ItemSO GetItemSO() {
        if (_inventoryMasterUI.gameObject.activeSelf) return null;

        var selectedItemSlot = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (selectedItemSlot == null) return null;
        return GameManager.Instance.ItemManager.ItemDatabase[selectedItemSlot.ItemId];
    }

    public bool IsPlayerHoldingWeapon() {
        var weaponSO = GetItemSO() as WeaponSO;
        return weaponSO != null;
    }
}