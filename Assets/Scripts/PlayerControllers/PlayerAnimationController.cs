using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour, IPlayerDataPersistance {
    public enum PlayerState {
        RaiseBowAndAim,
        LooseArrow,
        GrabNewArrow,
        AimNewArrow,

        Hurt,

        Idle,

        Slash,

        SlashReverse,

        Spellcast,

        RaiseStaff,
        ThrustLoop,

        Walkcycle,

        // No animations for these states yet
        Sleeping,
        Stunned,
        Dashing,
    }

    // Bow
    const string RAISE_BOW_AND_AIM = "RaiseBowAndAim";
    const string LOOSE_ARROW = "LooseArrow";
    const string GRAB_NEW_ARROW = "GrabNewArrow";
    const string AIM_NEW_ARROW = "AimNewArrow";

    // Death
    const string HURT = "Hurt";

    // Idle
    const string IDLE = "Idle";

    // Slash
    const string SLASH = "Slash";

    // Spellcast
    const string SPELLCAST = "Spellcast";

    // Thrust
    const string RAISE_STAFF = "RaiseStaff";
    const string THRUST_LOOP = "ThrustLoop";

    // Walkcycle
    const string WALKCYCLE = "Walkcycle";

    readonly Dictionary<PlayerState, string> _playerStates = new() {
        { PlayerState.RaiseBowAndAim, RAISE_BOW_AND_AIM },
        { PlayerState.LooseArrow, LOOSE_ARROW },
        { PlayerState.GrabNewArrow, GRAB_NEW_ARROW },
        { PlayerState.AimNewArrow, AIM_NEW_ARROW },
        { PlayerState.Hurt, HURT },
        { PlayerState.Idle, IDLE },
        { PlayerState.Slash, SLASH },
        { PlayerState.Spellcast, SPELLCAST },
        { PlayerState.RaiseStaff, RAISE_STAFF },
        { PlayerState.ThrustLoop, THRUST_LOOP },
        { PlayerState.Walkcycle, WALKCYCLE },
        { PlayerState.Sleeping, IDLE },
        { PlayerState.Stunned, IDLE },
        { PlayerState.Dashing, IDLE },
    };

    public PlayerState ActivePlayerState { get; private set; }
    public float AnimationTime => _weaponAnim.GetCurrentAnimatorStateInfo(0).length;

    [SerializeField] Animator _weaponAnim;
    [SerializeField] Animator _handAnim;
    [SerializeField] Animator _helmetHairAnim;
    [SerializeField] Animator _beltAnim;
    [SerializeField] Animator _torsoAnim;
    [SerializeField] Animator _legsAnim;
    [SerializeField] Animator _feetAnim;
    [SerializeField] Animator _headAnim;
    [SerializeField] Animator _bodyAnim;
    [SerializeField] Animator _behindAnim;

    ItemSO _weaponItemSO;
    ItemSO _handItemSO;
    ItemSO _helmetItemSO;
    // XX _hair;
    ItemSO _beltItemSO;
    ItemSO _torsoItemSO;
    ItemSO _legsItemSO;
    ItemSO _feetItemSO;
    // XX _head;
    // XX _body;

    // Movement
    public const string X_AXIS = "xAxis";
    public const string Y_AXIS = "yAxis";
    public const string LAST_X_AXIS = "lastXAxis";
    public const string LAST_Y_AXIS = "lastYAxis";

    PlayerToolsAndWeaponController _pTWC;
    PlayerToolbeltController _pTC;

    private void Awake() {
        ChangeState(PlayerState.Idle, true);
        _pTWC = GetComponent<PlayerToolsAndWeaponController>();
        _pTC = GetComponent<PlayerToolbeltController>();
    }

    private void Start() {
        _pTC.OnToolbeltSlotChanged += ToolbeltSlotChanged;
    }

    private void OnDestroy() {
        _pTC.OnToolbeltSlotChanged -= ToolbeltSlotChanged;
    }

    IEnumerator SetWeapon(RuntimeAnimatorController runtimeAnimatorController) {
        _weaponAnim.GetComponent<OwnerNetworkAnimator>().enabled = false;
        _weaponAnim.runtimeAnimatorController = runtimeAnimatorController;
        _weaponAnim.Rebind();
        yield return new WaitForEndOfFrame();
        _weaponAnim.GetComponent<OwnerNetworkAnimator>().enabled = true;
    }

    public void SetHand(ItemSO handItemSO) {
        _handItemSO = handItemSO;
    }

    public void SetHelmet(ItemSO helmetItemSO) {
        _helmetItemSO = helmetItemSO;
    }

    public void SetHair() {
        // _hair = hair;
    }

    public void SetBelt(ItemSO beltItemSO) {
        _beltItemSO = beltItemSO;
    }

    public void SetTorso(ItemSO torsoItemSO) {
        _torsoItemSO = torsoItemSO;
    }

    public void SetLegs(ItemSO legsItemSO) {
        _legsItemSO = legsItemSO;
    }

    public void SetFeet(ItemSO feetItemSO) {
        _feetItemSO = feetItemSO;
    }

    public void SetHead() {
        // _head = head;
    }

    public void SetBody() {
        // _body = body;
    }

    IEnumerator SetBehind(RuntimeAnimatorController runtimeAnimatorController) {
        _behindAnim.GetComponent<OwnerNetworkAnimator>().enabled = false;
        _behindAnim.runtimeAnimatorController = runtimeAnimatorController;
        _weaponAnim.Rebind();
        yield return new WaitForEndOfFrame();
        _behindAnim.GetComponent<OwnerNetworkAnimator>().enabled = true;
    }

    void ToolbeltSlotChanged() {
        int itemId = _pTC.GetCurrentlySelectedToolbeltItemSlot().ItemId;
        ItemSO itemSO = GameManager.Instance.ItemManager.ItemDatabase[itemId];
        if (itemSO is WeaponSO weaponSO) {
            Debug.Log("Setting weapon");
            if (weaponSO.AnimatorFG != null) StartCoroutine(SetWeapon(weaponSO.AnimatorFG));
            if (weaponSO.AnimatorBG != null) StartCoroutine(SetBehind(weaponSO.AnimatorBG));
        } else if (itemSO is ToolSO toolSO) {
            if (toolSO.AnimatorFG != null) StartCoroutine(SetWeapon(toolSO.AnimatorFG));
            if (toolSO.AnimatorBG != null) StartCoroutine(SetBehind(toolSO.AnimatorBG));
        }

        ChangeState(ActivePlayerState, true);
    }

    public void ChangeState(PlayerState newState, bool forceUpdate = false) {
        if (ActivePlayerState == newState && !forceUpdate) return;

        string stateName = _playerStates[newState];
        PlayAnimationIfExists(_weaponAnim, stateName);
        PlayAnimationIfExists(_handAnim, stateName);
        PlayAnimationIfExists(_helmetHairAnim, stateName);
        PlayAnimationIfExists(_beltAnim, stateName);
        PlayAnimationIfExists(_torsoAnim, stateName);
        PlayAnimationIfExists(_legsAnim, stateName);
        PlayAnimationIfExists(_feetAnim, stateName);
        PlayAnimationIfExists(_headAnim, stateName);
        PlayAnimationIfExists(_bodyAnim, stateName);
        PlayAnimationIfExists(_behindAnim, stateName);

        ActivePlayerState = newState;
    }

    void PlayAnimationIfExists(Animator animator, string stateName) {
        int stateHash = Animator.StringToHash(stateName);
        if (animator != null && animator.HasState(0, stateHash)) {
            animator.GetComponent<OwnerNetworkAnimator>().enabled = true;
            animator.Play(stateName);
        } else {
            animator.GetComponent<OwnerNetworkAnimator>().enabled = false;
        }
    }

    public void SetAnimatorDirection(Vector2 direction) {
        SetAnimatorValues(X_AXIS, direction.x);
        SetAnimatorValues(Y_AXIS, direction.y);
    }

    public void SetAnimatorLastDirection(Vector2 direction) {
        SetAnimatorValues(LAST_X_AXIS, direction.x);
        SetAnimatorValues(LAST_Y_AXIS, direction.y);
    }

    void SetAnimatorValues(string axis, float value) {
        _weaponAnim.SetFloat(axis, value);
        _handAnim.SetFloat(axis, value);
        _helmetHairAnim.SetFloat(axis, value);
        _beltAnim.SetFloat(axis, value);
        _torsoAnim.SetFloat(axis, value);
        _legsAnim.SetFloat(axis, value);
        _feetAnim.SetFloat(axis, value);
        _headAnim.SetFloat(axis, value);
        _bodyAnim.SetFloat(axis, value);
        _behindAnim.SetFloat(axis, value);
    }

    #region -------------------- Save & Load --------------------
    public void SavePlayer(PlayerData playerData) {
        playerData.LastDirection = new Vector2(_weaponAnim.GetFloat(LAST_X_AXIS), _weaponAnim.GetFloat(LAST_Y_AXIS));
    }

    public void LoadPlayer(PlayerData playerData) {
        SetAnimatorLastDirection(playerData.LastDirection);
    }
    #endregion -------------------- Save & Load --------------------
}
