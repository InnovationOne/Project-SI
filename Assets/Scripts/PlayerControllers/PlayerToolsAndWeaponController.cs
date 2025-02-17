using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static PlayerAnimationController;

// This class represents the character using an item
[RequireComponent(typeof(NetworkObject))]
public class PlayerToolsAndWeaponController : NetworkBehaviour {
    [Header("Animator")]
    [SerializeField] Animator _weaponAnim;
    [SerializeField] Animator _behindAnim;

    [Header("Enemy")]
    [SerializeField] private LayerMask _enemyLayerMask;

    PlayerMarkerController _playerMarkerController;
    PlayerToolbeltController _playerToolbeltController;
    PlayerHealthAndEnergyController _pHEC;
    PlayerMovementController _pMC;
    PlayerAnimationController _pAC;
    InputManager _inputManager;
    InventoryMasterUI _inventoryMasterUI;
    AudioManager _audioManager;
    FMODEvents _fmodEvents;

    // Network
    const float MAX_TIMEOUT = 2f;
    bool _success;
    bool _callbackSuccessful;
    float _timeout;
    float _elapsedTime;

    // Combat
    float _attackTimer;
    float _bowChargedTimer;

    // Combo
    float _comboTimer;
    int _comboIndex;

    ContactFilter2D _contactFilter;
    Collider2D[] _overlapResults = new Collider2D[10];

    void Start() {
        _playerMarkerController = GetComponent<PlayerMarkerController>();
        _playerToolbeltController = GetComponent<PlayerToolbeltController>();
        _pHEC = GetComponent<PlayerHealthAndEnergyController>();
        _pMC = GetComponent<PlayerMovementController>();
        _inventoryMasterUI = InventoryMasterUI.Instance;
        _pAC = GetComponent<PlayerAnimationController>();

        _audioManager = GameManager.Instance.AudioManager;
        _fmodEvents = GameManager.Instance.FMODEvents;

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
        return _attackTimer <= 0f && _pAC.ActivePlayerState == PlayerState.Idle || _pAC.ActivePlayerState == PlayerState.Walkcycle;
    }

    void HandleLeftClick() {
        var itemSO = GetItemSO();
        if (itemSO == null) return;

        if (itemSO.ItemType == ItemSO.ItemTypes.Weapons) {
            WeaponAction(itemSO as WeaponSO);
            return;
        }

        ToolAction(itemSO);
    }

    #region -------------------- Weapon Action --------------------
    void WeaponAction(WeaponSO weaponSO) {
        if (!CanAttack()) return;
        _attackTimer = 1f / weaponSO.AttackSpeed;

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
        if (Time.time - _comboTimer > weaponSO.ComboMaxDelay) _comboIndex = 0;

        PlayerState animationType = PlayerState.Slash;
        if (weaponSO.HasSlashAnimation && _comboIndex == 0) {
            // 1. Slash Animation
            animationType = PlayerState.Slash;
        } else if (weaponSO.HasSlashReverseAnimation && _comboIndex == 1) {
            // 2. Slash Reverse Animation
            animationType = PlayerState.SlashReverse;
        } else if (weaponSO.HasThrustAnimation && _comboIndex == 1 || _comboIndex == 2) {
            // 3. Thrust Animation
            animationType = PlayerState.RaiseStaff;
        } else {
            // Fallback
            Debug.LogError($"No animation found for comboIndex {_comboIndex}");
        }

        // Combo Index
        _comboIndex++;
        if (_comboIndex > weaponSO.ComboMaxCount - 1) {
            _comboIndex = 0;
        }

        // Start attack
        _pHEC.AdjustEnergy(-weaponSO.AttackEnergyCost);
        StartCoroutine(PerformMeleeHit(weaponSO, animationType));
    }

    IEnumerator PerformMeleeHit(WeaponSO weapon, PlayerState animationType) {
        // 1) If the animation is RaiseStaff, play it fully before doing the Thrust
        StopMovement();
        var previousState = _pAC.ActivePlayerState;
        _audioManager.PlayOneShot(_fmodEvents.Whip_Weapon, transform.position);

        if (animationType == PlayerState.RaiseStaff) {
            _pAC.ChangeState(PlayerState.RaiseStaff, true);
            yield return null;
            var animInfo = _weaponAnim.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(animInfo.length / animInfo.speed);
            animationType = PlayerState.ThrustLoop;
        }

        // 2) Switch to the chosen attack state
        _pAC.ChangeState(animationType, true);
        yield return null;

        // 3) Determine which keyFrame to use (slash, slash reverse or thrust)
        float fractionOfClip = 0f;
        if (animationType == PlayerState.Slash) {
            float slashAnimationFrames = 6f;
            fractionOfClip = weapon.SlashHitFrameIndex / slashAnimationFrames;
        } else if (animationType == PlayerState.SlashReverse) {
            float slashAnimationFrames = 6f;
            fractionOfClip = weapon.SlashReverseHitFrameIndex / slashAnimationFrames;
        } else if (animationType == PlayerState.ThrustLoop) {
            float thrustAnimationFrames = 4f;
            fractionOfClip = weapon.ThrustHitFrameIndex / thrustAnimationFrames;
        }

        // 4) Get the AnimatorStateInfo for the weapon anim 
        var info = _weaponAnim.GetCurrentAnimatorStateInfo(0);
        float actualClipDuration = info.length / info.speed;

        // Time until the keyframe
        float waitUntilHit = fractionOfClip * actualClipDuration;
        yield return new WaitForSeconds(waitUntilHit);

        // 5) Perform the actual hit logic
        // a) Check collider from the front/weapon sprite
        var frontSr = _weaponAnim.GetComponent<SpriteRenderer>();
        SpawnAndCheckColliderFromSprite(frontSr, weapon.AttackDamage, weapon.DamageType, weapon.KnockbackForce);

        // b) Check collider from the behind sprite
        var behindSr = _behindAnim.GetComponent<SpriteRenderer>();
        SpawnAndCheckColliderFromSprite(behindSr, weapon.AttackDamage, weapon.DamageType, weapon.KnockbackForce);

        // 7) Wait the remainder of the clip 
        float remainingTime = actualClipDuration - waitUntilHit;
        if (remainingTime > 0f) {
            yield return new WaitForSeconds(remainingTime);
        }

        // 8) Restore previous state
        _pAC.ChangeState(previousState, true);
        StartMovement();

        // 9) Combo Timer
        _comboTimer = Time.time;
    }

    private void SpawnAndCheckColliderFromSprite(SpriteRenderer sr, int damage, WeaponSO.DamageTypes dmgType, float knockback) {
        if (!sr || !sr.sprite) return;

        var currentSprite = sr.sprite;
        int shapeCount = currentSprite.GetPhysicsShapeCount();
        if (shapeCount == 0) return;

        var shapePoints = new List<Vector2>();
        currentSprite.GetPhysicsShape(0, shapePoints);
        if (shapePoints.Count < 3) return;

        // Create a temporary object with a PolygonCollider2D
        var tempHitbox = new GameObject("TempMeleeHitbox");
        tempHitbox.transform.position = transform.position;

        var polyCollider = tempHitbox.AddComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;
        polyCollider.SetPath(0, shapePoints.ToArray());

        // Overlap collider
        int hitCount = Physics2D.OverlapCollider(polyCollider, _contactFilter, _overlapResults);
        if (hitCount > 0) {
            for (int i = 0; i < hitCount; i++) {
                Collider2D col = _overlapResults[i];
                if (col && col.TryGetComponent<IDamageable>(out var target)) {
                    target.TakeDamage(transform.position, damage, dmgType, knockback);
                    Debug.Log($"Hit {col.name} for {damage} damage");
                }
                _overlapResults[i] = null;
            }
        }

        Destroy(tempHitbox);
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
        //_audioManager.PlayOneShot(_fmodEvents.Shoot_Arrow, transform.position);
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
        yield return new WaitForSeconds(_weaponAnim.GetCurrentAnimatorStateInfo(0).length);
        _pAC.ChangeState(PlayerState.GrabNewArrow, true);
        yield return new WaitForSeconds(_weaponAnim.GetCurrentAnimatorStateInfo(0).length);
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