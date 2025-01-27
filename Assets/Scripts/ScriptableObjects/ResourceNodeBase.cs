using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Types of resource nodes available.
/// </summary>
public enum ResourceNodeType {
    Tree,
    Ore,
    Branch,
    TreeStump,
}

/// <summary>
/// Base class for resource nodes (e.g., trees, ores).
/// Manages common logic like HP, hitting, dropping items, and network sync.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(NetworkObject))]
public abstract class ResourceNodeBase : NetworkBehaviour {
    // Serialized Fields
    [Header("Node Settings")]
    [SerializeField] protected int _startingHP = 100;
    [SerializeField] protected int _minimumToolRarity = 0;

    [Header("Item Slot Settings")]
    [SerializeField] protected ItemSO _itemSO;
    [SerializeField] protected int _minDropCount = 1;
    [SerializeField] protected int _maxDropCount = 3;
    [SerializeField] protected int _rarityID = 0;

    // Constants
    protected const float SHAKE_AMOUNT_X = 0.05f;
    protected const float SHAKE_AMOUNT_Y = 0.01f;
    protected const float TIME_BETWEEN_SHAKES = 0.03f;
    protected const int SHAKE_ITERATIONS = 3;

    protected static readonly Vector2[] _adjacentOffsets = {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1,  0),             new(1,  0),
        new(-1,  1), new(0,  1), new(1,  1)
    };

    Vector3 _originalPosition;
    Coroutine _shakeCoroutine;

    // Network Variables
    protected NetworkVariable<int> _networkCurrentHp = new(
        value: 100,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    protected NetworkVariable<bool> _networkHitShookToday = new(
        value: false,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    // Cached references 
    protected TimeManager _timeManager;
    protected PlayerToolbeltController _playerToolbeltController;
    protected CropsManager _cropsManager;
    protected PlayerMovementController _playerMovementController;
    protected AudioManager _audioManager;
    protected FMODEvents _fmodEvents;
    protected BoxCollider2D _boxCollider2D;


    protected virtual void Awake() {
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _originalPosition = transform.position;
    }

    private void Start() {
        _originalPosition = transform.position;
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            InitializeServer();
        }
        InitializeClient();
    }

    private new void OnDestroy() {
        if (IsServer) {
            UnsubscribeServerEvents();
        }
        UnsubscribeClientEvents();
        base.OnDestroy();
    }

    protected virtual void InitializeServer() {
        _timeManager = GameManager.Instance.TimeManager;
        _playerToolbeltController = PlayerController.LocalInstance.PlayerToolbeltController;
        _cropsManager = GameManager.Instance.CropsManager;
        _playerMovementController = PlayerController.LocalInstance.PlayerMovementController;
        _audioManager = GameManager.Instance.AudioManager;
        _fmodEvents = GameManager.Instance.FMODEvents;

        ResetHealth();

        _timeManager.OnNextDayStarted += OnNextDayStarted;
    }

    protected virtual void InitializeClient() {
        _networkCurrentHp.OnValueChanged += OnCurrentHpChanged;
        _networkHitShookToday.OnValueChanged += OnHitTodayChanged;
    }

    protected virtual void UnsubscribeServerEvents() {
        if (_timeManager != null) {
            _timeManager.OnNextDayStarted -= OnNextDayStarted;
        }
    }

    /// <summary>
    /// Unsubscribes client-side network variable changes.
    /// </summary>
    protected virtual void UnsubscribeClientEvents() {
        _networkCurrentHp.OnValueChanged -= OnCurrentHpChanged;
        _networkHitShookToday.OnValueChanged -= OnHitTodayChanged;
    }

    protected virtual void OnNextDayStarted() {
        ResetHealth();
        PerformTypeSpecificNextDayActions();
    }

    protected void ResetHealth() {
        _networkCurrentHp.Value = _startingHP;
        _networkHitShookToday.Value = false;
    }

    protected virtual void OnCurrentHpChanged(int oldValue, int newValue) {
        _networkCurrentHp.Value = newValue;

        bool isDestroyed = _networkCurrentHp.Value <= 0;
        PlaySound(isDestroyed);
        if (!isDestroyed) {
            StartShakeEffect();
        } else {
            HandleNodeDestruction();
        }
    }

    protected void OnHitTodayChanged(bool oldValue, bool newValue) => _networkHitShookToday.Value = newValue;

    [ServerRpc(RequireOwnership = false)]
    public virtual void HitResourceNodeServerRpc(int damage, ServerRpcParams rpcParams = default) {
        var selectedTool = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (selectedTool.RarityId < _minimumToolRarity) {
            Debug.LogWarning("Tool rarity too low.");
            // TODO: Implement bounce back animation & sound
            HandleClientCallback(rpcParams, false);
            return;
        }

        ApplyDamage(damage);
        HandleClientCallback(rpcParams, true);
    }

    protected void ApplyDamage(int damage) => _networkCurrentHp.Value -= damage;

    [ClientRpc]
    protected void DestroyGameObjectClientRpc() => Destroy(gameObject);

    protected void DestroyNodeAcrossNetwork() {
        var position = Vector3Int.FloorToInt(transform.position);
        var cropTileOpt = _cropsManager.GetCropTileAtPosition(position);
        if (cropTileOpt != null) {
            // Destroy the plant and the cropTile itself
            //_cropsManager.DestroyCropTileServerRpc(new Vector3IntSerializable(position), 0, ToolSO.ToolTypes.Pickaxe);
        }

        DestroyGameObjectClientRpc();
    }

    protected void StartShakeEffect() {
        if (_shakeCoroutine != null) {
            StopCoroutine(_shakeCoroutine);
        }

        _shakeCoroutine = StartCoroutine(ShakeAfterHit());
    }

    IEnumerator ShakeAfterHit() { 
        var originalPos = _originalPosition;
        for (int i = 0; i < SHAKE_ITERATIONS; i++) {
            transform.position = originalPos + new Vector3(SHAKE_AMOUNT_X, SHAKE_AMOUNT_Y, 0f);
            yield return new WaitForSeconds(TIME_BETWEEN_SHAKES);
            transform.position = originalPos;
            yield return new WaitForSeconds(TIME_BETWEEN_SHAKES);
        }
    }

    protected Vector3 GetRandomAdjacentPosition(Vector3Int treePosition) {
        int randomIndex = Random.Range(0, _adjacentOffsets.Length);
        var offset = _adjacentOffsets[randomIndex];
        return new Vector3(treePosition.x + offset.x, treePosition.y + offset.y, treePosition.z);
    }

    protected abstract void PerformTypeSpecificNextDayActions();
    protected abstract void PlaySound(bool isDestroyed);
    protected abstract void HandleNodeDestruction();

    public abstract bool CanHitResourceNodeType(HashSet<ResourceNodeType> canBeHit);
    public abstract void SetSeed(SeedSO seed);

    protected void HandleClientCallback(ServerRpcParams serverRpcParams, bool success) {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId)) {
            var client = NetworkManager.ConnectedClients[clientId];
            if (client.PlayerObject.TryGetComponent<PlayerToolsAndWeaponController>(out var ptw)) {
                ptw.ClientCallbackClientRpc(success);
            }
        }
    }
}
