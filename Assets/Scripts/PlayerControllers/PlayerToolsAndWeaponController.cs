using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static PlayerAnimationController;

// This class represents the character using an item
[RequireComponent(typeof(NetworkObject))]
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    PlayerMarkerController _playerMarkerController;
    PlayerToolbeltController _playerToolbeltController;
    PlayerController _pC;
    PlayerHealthAndEnergyController _pHEC;
    PlayerMovementController _pMC;
    PlayerAnimationController _pAC;
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

    float _comboTimer;
    int _comboIndex;

    float _bowChargedTimer;

    private ContactFilter2D _contactFilter;
    private Collider2D[] _overlapResults = new Collider2D[10];
    [SerializeField] private LayerMask _enemyLayerMask;

    void Start() {
        _pC = GetComponent<PlayerController>();
        _playerMarkerController = GetComponent<PlayerMarkerController>();
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _pHEC = GetComponent<PlayerHealthAndEnergyController>();
        _pMC = GetComponent<PlayerMovementController>();
        _itemManager = GameManager.Instance.ItemManager;
        _inventoryMasterUI = InventoryMasterUI.Instance;
        _pAC = GetComponent<PlayerAnimationController>();

        _inputManager = GameManager.Instance.InputManager;
        _inputManager.OnLeftClickAction += HandleLeftClick;
        _inputManager.OnRightClickStarted += HandleRightClickStarted;
        _inputManager.OnRightClickCanceled += HandleRightClickCanceled;

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
    }

    bool CanAttack() {
        return _attackTimer <= 0f;
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

        switch (weaponSO.WeaponType) {
            case WeaponSO.WeaponTypes.Melee:
                MeleeAction(weaponSO);
                break;
            case WeaponSO.WeaponTypes.Ranged:
                RangedAction(weaponSO);
                break;
            case WeaponSO.WeaponTypes.Magic:
                //MagicAction(weaponSO);
                break;
        }

        
    }

    #region -------------------- Meele Action --------------------
    void MeleeAction(WeaponSO weaponSO) {
        return;

        CheckComboWindow(weaponSO.ComboMaxDelay, weaponSO.ComboMaxCount);

        PlayerState animationType = PlayerState.Slash;
        // Sword 
        if (weaponSO.HasSlashAnimation && _comboIndex == 0) {
            // 1. Slash Animation
            animationType = PlayerState.Slash;
        } else if (weaponSO.HasSlashReverseAnimation && _comboIndex == 1) {
            // 2. Slash Reverse Animation
            animationType = PlayerState.SlashReverse;
        } else if (weaponSO.HasThrustAnimation) {
            // 3. Thrust Animation
            animationType = PlayerState.RaiseStaff;
        } else {
            // Fallback
            Debug.LogError($"No animation found for comboIndex {_comboIndex}");
        }

        // Start attack
        Debug.Log($"[Player Attack] ComboIndex = {_comboIndex}/{weaponSO.ComboMaxCount - 1}");
        _pHEC.AdjustEnergy(-weaponSO.AttackEnergyCost);
        _attackTimer = 1f / weaponSO.AttackSpeed;
        StartCoroutine(PerformMeleeHit(weaponSO, _comboIndex, animationType));
    }

    void CheckComboWindow(float comboMaxDelay, int comboMaxCount) {
        if (Time.time - _comboTimer > comboMaxDelay) { 
            _comboIndex = 0; 
        } else { 
            _comboIndex++;
            if (_comboIndex > comboMaxCount - 1){ 
                _comboIndex = 0; 
            }
        }        
        _comboTimer = Time.time;
    }

    IEnumerator PerformMeleeHit(WeaponSO weapon, int comboIndex, PlayerState animationType) {
        StopMovement();
        var previousPlayerState = _pAC.ActivePlayerState;
        _pAC.ChangeState(animationType);
        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length / 2);

        // 1) Hole dir die korrekten Polygondaten
        int arrayIndex = Mathf.Clamp(comboIndex, 0, 999);

        var listOfPoints = weapon.ComboPointsAttack;

        if (arrayIndex >= listOfPoints.Count) {
            Debug.LogError($"ComboIndex {comboIndex} is out of bounds for weapon {weapon.name}");
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
            WeaponSO.DamageTypes dmgType = weapon.DamageType;

            for (int i = 0; i < hitCount; i++) {
                Collider2D col = _overlapResults[i];

                // Prüfen, ob der Collider ein BoxCollider2D ist
                if (col is BoxCollider2D && col.TryGetComponent<IDamageable>(out var target)) {
                    target.TakeDamage(transform.position, weapon.AttackDamage, dmgType, weapon.KnockbackForce);
                    Debug.Log($"Hit {col.name} for {weapon.AttackDamage} damage");
                }

                // Den Eintrag zurücksetzen, um die Liste aufzuräumen
                _overlapResults[i] = null;
            }
        }

        // 4) Zerstöre das temporäre Objekt
        Destroy(tempHitbox);

        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length / 2);

        _pAC.ChangeState(previousPlayerState, true);

        StartMovement();
    }

    #endregion -------------------- Meele Action --------------------

    #region -------------------- Ranged Action --------------------
    void HandleRightClickStarted() {
        var weaponSO = GetItemSO() as WeaponSO;
        if (weaponSO == null || !weaponSO.HasBowAnimation) return;
        StopMovement();

        // Bogen spannen
        _pAC.ChangeState(PlayerState.RaiseBowAndAim, true);
        _bowChargedTimer = Time.time + _pAC.AnimationTime;
    }

    void HandleRightClickCanceled() {
        // Bogen loslassen
        _pAC.ChangeState(PlayerState.Idle, true);
        StartMovement();
    }

    void RangedAction(WeaponSO weaponSO) {
        // Bow
        if (weaponSO.HasBowAnimation && CanShoot()) {
            Shoot(true);
        }

        // Crossbow
        if (weaponSO.HasThrustAnimation) {
            Shoot(false);
        }
    }

    bool CanShoot() => (_pAC.ActivePlayerState == PlayerState.RaiseBowAndAim || _pAC.ActivePlayerState == PlayerState.AimNewArrow) && Time.time > _bowChargedTimer;

    void Shoot(bool hasReload) {
        // Shoot arrow
        // TODO: Implement arrow shooting

        if (hasReload) {
            StartCoroutine(ReloadBow());
        }
    }

    IEnumerator ReloadBow() {
        _pAC.ChangeState(PlayerState.LooseArrow, true);
        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length);
        _pAC.ChangeState(PlayerState.GrabNewArrow, true);
        yield return new WaitForSeconds(_anim.GetCurrentAnimatorStateInfo(0).length);
        _pAC.ChangeState(PlayerState.AimNewArrow, true);
        _bowChargedTimer = Time.time + _pAC.AnimationTime;
    }
    #endregion -------------------- Ranged Action --------------------
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