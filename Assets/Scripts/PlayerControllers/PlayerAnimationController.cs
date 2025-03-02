using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour, IPlayerDataPersistance {
    public enum PlayerState {
        RaiseBowAndAim, LooseArrow, GrabNewArrow, AimNewArrow,
        FishingHold, FishingThrow, FishingReelLoop, FishingLand,
        Hurt,
        Idle,
        Slash, SlashReverse,
        Spellcast,
        RaiseStaff, ThrustLoop,
        Walkcycle,

        // No animations yet
        Sleeping,
        Stunned,
        Dashing
    }

    // Animation state string constants
    const string RAISE_BOW_AND_AIM = "RaiseBowAndAim";
    const string LOOSE_ARROW = "LooseArrow";
    const string GRAB_NEW_ARROW = "GrabNewArrow";
    const string AIM_NEW_ARROW = "AimNewArrow";
    const string FISHING_HOLD = "FishingHold";
    const string FISHING_THROW = "FishingThrow";
    const string FISHING_REEL_LOOP = "FishingReelLoop";
    const string FISHING_LAND = "FishingLand";
    const string HURT = "Hurt";
    const string IDLE = "Idle";
    const string SLASH = "Slash";
    const string SlashReverse = "SlashReverse";
    const string SPELLCAST = "Spellcast";
    const string RAISE_STAFF = "RaiseStaff";
    const string THRUST_LOOP = "ThrustLoop";
    const string WALKCYCLE = "Walkcycle";

    // Mapping of PlayerStates to Animator state names
    readonly Dictionary<PlayerState, string> _playerStates = new()
    {
        { PlayerState.RaiseBowAndAim, RAISE_BOW_AND_AIM     },
        { PlayerState.LooseArrow,     LOOSE_ARROW           },
        { PlayerState.GrabNewArrow,   GRAB_NEW_ARROW        },
        { PlayerState.AimNewArrow,    AIM_NEW_ARROW         },
        { PlayerState.FishingHold,    FISHING_HOLD          },
        { PlayerState.FishingThrow,  FISHING_THROW          },
        { PlayerState.FishingReelLoop, FISHING_REEL_LOOP    },
        { PlayerState.FishingLand,   FISHING_LAND           },
        { PlayerState.Hurt,           HURT                  },
        { PlayerState.Idle,           IDLE                  },
        { PlayerState.Slash,          SLASH                 },
        { PlayerState.SlashReverse,   SlashReverse          },
        { PlayerState.Spellcast,      SPELLCAST             },
        { PlayerState.RaiseStaff,     RAISE_STAFF           },
        { PlayerState.ThrustLoop,     THRUST_LOOP           },
        { PlayerState.Walkcycle,      WALKCYCLE             },
        { PlayerState.Sleeping,       IDLE                  }, // No animations yet
        { PlayerState.Stunned,        IDLE                  }, // No animations yet
        { PlayerState.Dashing,        IDLE                  }, // No animations yet
    };

    [HideInInspector] public PlayerState ActivePlayerState { get; private set; }
    public float AnimationTime => _weaponAnim != null
        ? _weaponAnim.GetCurrentAnimatorStateInfo(0).length / _weaponAnim.GetCurrentAnimatorStateInfo(0).speed
        : 0f;

    [Header("Animator References")]
    [SerializeField] Animator _weaponAnim;
    [SerializeField] Animator _handsAnim;
    [SerializeField] Animator _helmetHairAnim;
    [SerializeField] Animator _beltAnim;
    [SerializeField] Animator _torsoAnim;
    [SerializeField] Animator _legsAnim;
    [SerializeField] Animator _feetAnim;
    [SerializeField] Animator _headAnim;
    [SerializeField] Animator _bodyAnim;
    [SerializeField] Animator _behindAnim;
    [SerializeField] Animator _shadowAnim;

    [Header("DEBUG: Animator Controllers")]
    [SerializeField] RuntimeAnimatorController _shadowAnimator;
    [SerializeField] RuntimeAnimatorController _bodyAnimator;
    [SerializeField] RuntimeAnimatorController _headAnimator;
    [SerializeField] RuntimeAnimatorController _defaultAnimator;

    // Parameter strings for directional movement
    public const string X_AXIS = "xAxis";
    public const string Y_AXIS = "yAxis";
    public const string LAST_X_AXIS = "lastXAxis";
    public const string LAST_Y_AXIS = "lastYAxis";

    private PlayerToolbeltController _pTC;
    private PlayerMovementController _pMC;
    private InputManager _iM;
    private ClothingUI _clothingUI;
    private AudioManager _audioManager;
    private FMODEvents _fmodEvents;

    private void Awake() {
        _iM = GameManager.Instance.InputManager;
        _pTC = GetComponent<PlayerToolbeltController>();
        _pMC = GetComponent<PlayerMovementController>();
    }

    private void Start() {
        _clothingUI = UIManager.Instance.ClothingUI;
        _audioManager = GameManager.Instance.AudioManager;
        _fmodEvents = GameManager.Instance.FMODEvents;

        // Subscribe to clothing changes for visual updates.
        _clothingUI.PlayerClothingUIItemButtons[0].OnNewItem += SetFeet;
        _clothingUI.PlayerClothingUIItemButtons[1].OnNewItem += SetBelt;
        _clothingUI.PlayerClothingUIItemButtons[2].OnNewItem += SetHelmet;
        _clothingUI.PlayerClothingUIItemButtons[3].OnNewItem += SetLegs;
        _clothingUI.PlayerClothingUIItemButtons[4].OnNewItem += SetHands;
        _clothingUI.PlayerClothingUIItemButtons[5].OnNewItem += SetTorso;

        _pTC.OnToolbeltSlotChanged += OnToolbeltSlotChanged;

        // DEBUG: Apply correct override controllers.
        StartCoroutine(SetAnimationOverride(_bodyAnim, _bodyAnimator));
        StartCoroutine(SetAnimationOverride(_headAnim, _headAnimator));
        StartCoroutine(SetAnimationOverride(_shadowAnim, _shadowAnimator));

        ChangeState(PlayerState.Idle, true);
        SetAnimatorDirection(_iM.GetMovementVectorNormalized());
        SetAnimatorLastDirection(_pMC.LastMotionDirection);
    }

    private void OnDestroy() {
        // Unsubscribe to avoid memory leaks.
        _clothingUI.PlayerClothingUIItemButtons[0].OnNewItem -= SetFeet;
        _clothingUI.PlayerClothingUIItemButtons[1].OnNewItem -= SetBelt;
        _clothingUI.PlayerClothingUIItemButtons[2].OnNewItem -= SetHelmet;
        _clothingUI.PlayerClothingUIItemButtons[3].OnNewItem -= SetLegs;
        _clothingUI.PlayerClothingUIItemButtons[4].OnNewItem -= SetHands;
        _clothingUI.PlayerClothingUIItemButtons[5].OnNewItem -= SetTorso;

        _pTC.OnToolbeltSlotChanged -= OnToolbeltSlotChanged;
    }

    IEnumerator SetAnimationOverride(Animator anim, RuntimeAnimatorController runtimeAnimatorController) {
        var ownerNetworkAnimator = anim.GetComponent<OwnerNetworkAnimator>();
        ownerNetworkAnimator.enabled = false;

        anim.runtimeAnimatorController = runtimeAnimatorController;
        anim.Rebind();
        yield return new WaitForEndOfFrame();

        ownerNetworkAnimator.enabled = true;
        SetAnimatorLastDirection(_pMC.LastMotionDirection);
    }

    void OnToolbeltSlotChanged() {
        int itemId = _pTC.GetCurrentlySelectedToolbeltItemSlot().ItemId;
        var itemSO = GameManager.Instance.ItemManager.ItemDatabase[itemId];

        if (itemSO is WeaponSO weaponSO) {
            if (weaponSO.WeaponType == WeaponSO.WeaponTypes.Melee) {
                _audioManager.PlayOneShot(_fmodEvents.Pull_Weapon, transform.position);
            }
            if (weaponSO.AnimatorFG != null) {
                StartCoroutine(SetAnimationOverride(_weaponAnim, weaponSO.AnimatorFG));
            }
            if (weaponSO.AnimatorBG != null) {
                StartCoroutine(SetAnimationOverride(_behindAnim, weaponSO.AnimatorBG));
            }
        } else if (itemSO is ToolSO toolSO) {
            if (toolSO.AnimatorFG != null) {
                StartCoroutine(SetAnimationOverride(_weaponAnim, toolSO.AnimatorFG));
            }
            if (toolSO.AnimatorBG != null) {
                StartCoroutine(SetAnimationOverride(_behindAnim, toolSO.AnimatorBG));
            }
        } else {
            StartCoroutine(SetAnimationOverride(_weaponAnim, _defaultAnimator));
            StartCoroutine(SetAnimationOverride(_behindAnim, _defaultAnimator));
        }
        ChangeState(ActivePlayerState, true);

        SetAnimatorDirection(_iM.GetMovementVectorNormalized());
        SetAnimatorLastDirection(_pMC.LastMotionDirection);
    }

    public void ChangeState(PlayerState newState, bool forceUpdate = false) {
        if (ActivePlayerState == newState && !forceUpdate) return;
        string stateName = _playerStates[newState];

        TryPlayState(_weaponAnim, stateName);
        TryPlayState(_handsAnim, stateName);
        TryPlayState(_helmetHairAnim, stateName);
        TryPlayState(_beltAnim, stateName);
        TryPlayState(_torsoAnim, stateName);
        TryPlayState(_legsAnim, stateName);
        TryPlayState(_feetAnim, stateName);
        TryPlayState(_headAnim, stateName);
        TryPlayState(_bodyAnim, stateName);
        TryPlayState(_behindAnim, stateName);

        ActivePlayerState = newState;
    }

    bool TryPlayState(Animator animator, string stateName) {
        if (animator == null) return false;
        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, stateHash)) {
            animator.Play(stateHash);
            return true;
        }
        Debug.LogError($"Animator '{animator.name}' does not have a state named '{stateName}' in its Controller.");
        return false;
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
        _handsAnim.SetFloat(axis, value);
        _helmetHairAnim.SetFloat(axis, value);
        _beltAnim.SetFloat(axis, value);
        _torsoAnim.SetFloat(axis, value);
        _legsAnim.SetFloat(axis, value);
        _feetAnim.SetFloat(axis, value);
        _headAnim.SetFloat(axis, value);
        _bodyAnim.SetFloat(axis, value);
        _behindAnim.SetFloat(axis, value);
    }

    #region -------------------- Clothing equipment animation overrides --------------------

    public void SetHands(int itemId) {
        if (itemId == 0) StartCoroutine(SetAnimationOverride(_handsAnim, _defaultAnimator));
        else StartCoroutine(SetAnimationOverride(_handsAnim, (GameManager.Instance.ItemManager.ItemDatabase[itemId] as ClothingSO).Animator));
    }

    public void SetHelmet(int itemId) {
        if (itemId == 0) StartCoroutine(SetAnimationOverride(_helmetHairAnim, _defaultAnimator));
        else StartCoroutine(SetAnimationOverride(_helmetHairAnim, (GameManager.Instance.ItemManager.ItemDatabase[itemId] as ClothingSO).Animator));
    }

    public void SetHair() {
        // _hair = hair;
    }

    public void SetBelt(int itemId) {
        if (itemId == 0) StartCoroutine(SetAnimationOverride(_beltAnim, _defaultAnimator));
        else StartCoroutine(SetAnimationOverride(_beltAnim, (GameManager.Instance.ItemManager.ItemDatabase[itemId] as ClothingSO).Animator));
    }

    public void SetTorso(int itemId) {
        if (itemId == 0) StartCoroutine(SetAnimationOverride(_torsoAnim, _defaultAnimator));
        else StartCoroutine(SetAnimationOverride(_torsoAnim, (GameManager.Instance.ItemManager.ItemDatabase[itemId] as ClothingSO).Animator));
    }

    public void SetLegs(int itemId) {
        if (itemId == 0) StartCoroutine(SetAnimationOverride(_legsAnim, _defaultAnimator));
        else StartCoroutine(SetAnimationOverride(_legsAnim, (GameManager.Instance.ItemManager.ItemDatabase[itemId] as ClothingSO).Animator));
    }

    public void SetFeet(int itemId) {
        if (itemId == 0) StartCoroutine(SetAnimationOverride(_feetAnim, _defaultAnimator));
        else StartCoroutine(SetAnimationOverride(_feetAnim, (GameManager.Instance.ItemManager.ItemDatabase[itemId] as ClothingSO).Animator));
    }

    public void SetHead() {
        // _head = head;
    }

    public void SetBody() {
        // _body = body;
    }

    #endregion -------------------- Clothing equipment animation overrides --------------------

    #region -------------------- Save & Load --------------------

    public void SavePlayer(PlayerData playerData) {
        playerData.LastDirection = new Vector2(_weaponAnim.GetFloat(LAST_X_AXIS), _weaponAnim.GetFloat(LAST_Y_AXIS));
    }

    public void LoadPlayer(PlayerData playerData) {
        SetAnimatorLastDirection(playerData.LastDirection);

    }
    #endregion -------------------- Save & Load --------------------
}
