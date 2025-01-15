using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour {
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

    readonly Dictionary<PlayerState, string> _playerStates = new Dictionary<PlayerState, string> {
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

    public PlayerState ActivePlayerState = PlayerState.Idle;
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

    WeaponSO _weaponSO;
    ItemSO _handItemSO;
    ItemSO _helmetItemSO;
    // XX _hair;
    ItemSO _beltItemSO;
    ItemSO _torsoItemSO;
    ItemSO _legsItemSO;
    ItemSO _feetItemSO;
    // XX _head;
    // XX _body;

    PlayerToolsAndWeaponController _pTWC;
    PlayerToolbeltController _pTC;

    private void Awake() {
        _pTWC = GetComponent<PlayerToolsAndWeaponController>();
        _pTC = GetComponent<PlayerToolbeltController>();
    }

    private void Start() {
        _pTC.OnToolbeltSlotChanged += ToolbeltSlotChanged;
    }

    private void OnDestroy() {
        _pTC.OnToolbeltSlotChanged -= ToolbeltSlotChanged;
    }

    public void SetWeapon(WeaponSO weaponSO) {
        _weaponAnim.runtimeAnimatorController = weaponSO.Animator;
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

    void ToolbeltSlotChanged() {
        ChangeState(ActivePlayerState, true);
    }

    public void ChangeState(PlayerState newState, bool forceUpdate = false) {
        if (ActivePlayerState == newState && !forceUpdate) return;

        _weaponAnim.Play(_playerStates[newState]);
        _handAnim.Play(_playerStates[newState]);
        _helmetHairAnim.Play(_playerStates[newState]);
        _beltAnim.Play(_playerStates[newState]);
        _torsoAnim.Play(_playerStates[newState]);
        _legsAnim.Play(_playerStates[newState]);
        _feetAnim.Play(_playerStates[newState]);
        _headAnim.Play(_playerStates[newState]);
        _bodyAnim.Play(_playerStates[newState]);
        _behindAnim.Play(_playerStates[newState]);

        ActivePlayerState = newState;
    }
}
