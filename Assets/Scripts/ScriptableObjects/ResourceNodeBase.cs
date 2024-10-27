using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Types of resource nodes available in the game.
/// </summary>
public enum ResourceNodeType {
    Tree,
    Ore,
    Branch,
    TreeStump,
}

/// <summary>
/// Abstract base class representing a generic resource node in the game.
/// Handles common interactions, networking, and resource management.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
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
    protected static readonly Vector2[] _adjacentOffsets = new Vector2[]
    {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1,  0),             new(1,  0),
        new(-1,  1), new(0,  1), new(1,  1)
    };

    // Private variables
    private Vector3 _originalPosition;
    private Coroutine _shakeCoroutine;

    // Network Variables
    protected NetworkVariable<int> _networkCurrentHp = new NetworkVariable<int>(
        value: 100,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    protected NetworkVariable<bool> _networkHitShookToday = new NetworkVariable<bool>(
        value: false,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    // Cached singleton instances
    protected TimeManager _timeManager;
    protected PlayerToolbeltController _playerToolbeltController;
    protected CropsManager _cropsManager;
    protected PlayerMovementController _playerMovementController;
    protected AudioManager _audioManager;
    protected FMODEvents _fmodEvents;

    // Cached Components
    protected BoxCollider2D _boxCollider2D;

    #region Unity Callbacks

    protected virtual void Awake() {
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _originalPosition = transform.localPosition;
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            InitializeServer();
        }

        InitializeClient();
    }

    private void OnDestroy() {
        if (IsServer) {
            UnsubscribeServerEvents();
        }

        UnsubscribeClientEvents();
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Initializes server-side logic, including event subscriptions and initial state setup.
    /// </summary>
    protected virtual void InitializeServer() {
        // Cache Singleton Instances
        _timeManager = TimeManager.Instance;
        _playerToolbeltController = PlayerToolbeltController.LocalInstance;
        _cropsManager = CropsManager.Instance;
        _playerMovementController = PlayerMovementController.LocalInstance;
        _audioManager = AudioManager.Instance;
        _fmodEvents = FMODEvents.Instance;

        // Initialize HP
        _networkCurrentHp.Value = _startingHP;
        _networkHitShookToday.Value = false;

        // Subscribe to TimeManager Events
        _timeManager.OnNextDayStarted += OnNextDayStarted;
    }

    /// <summary>
    /// Initializes client-side logic, including network variable subscriptions.
    /// </summary>
    protected virtual void InitializeClient() {
        // Subscribe to NetworkVariable Changes
        _networkCurrentHp.OnValueChanged += OnCurrentHpChanged;
        _networkHitShookToday.OnValueChanged += OnHitTodayChanged;
    }

    /// <summary>
    /// Unsubscribes server-side events to prevent memory leaks.
    /// </summary>
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

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handler for the start of a new day event.
    /// Resets HP and performs type-specific actions.
    /// </summary>
    protected virtual void OnNextDayStarted() {
        ResetHealth();
        PerformTypeSpecificNextDayActions();
    }

    /// <summary>
    /// Resets the health of the resource node.
    /// </summary>
    protected void ResetHealth() {
        _networkCurrentHp.Value = _startingHP;
        _networkHitShookToday.Value = false;
    }

    /// <summary>
    /// Handles changes to the current HP network variable.
    /// Updates local state and triggers visual/audio effects.
    /// </summary>
    /// <param name="oldValue">Previous HP value.</param>
    /// <param name="newValue">New HP value.</param>
    protected virtual void OnCurrentHpChanged(int oldValue, int newValue) {
        _networkCurrentHp.Value = newValue;
        PlaySound();

        if (_networkCurrentHp.Value > 0) {
            StartShakeEffect();
        } else {
            HandleNodeDestruction();
        }
    }

    /// <summary>
    /// Handles changes to the hit today network variable.
    /// Updates local state accordingly.
    /// </summary>
    /// <param name="oldValue">Previous hit today value.</param>
    /// <param name="newValue">New hit today value.</param>
    protected void OnHitTodayChanged(bool oldValue, bool newValue) {
        _networkHitShookToday.Value = newValue;
    }

    #endregion

    #region Server RPCs

    /// <summary>
    /// Server RPC to handle hitting the resource node.
    /// Applies damage and manages node state.
    /// </summary>
    /// <param name="damage">Damage to apply.</param>
    [ServerRpc(RequireOwnership = false)]
    public virtual void HitResourceNodeServerRpc(int damage, ServerRpcParams rpcParams = default) {
        ItemSlot selectedTool = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();

        if (selectedTool.RarityId < _minimumToolRarity) {
            Debug.LogWarning("Tool rarity too low.");
            // TODO: Implement bounce back animation & sound
            HandleClientCallback(rpcParams, false);
            return;
        }

        ApplyDamage(damage);
    }

    /// <summary>
    /// Applies damage to the resource node and updates network variables.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    protected void ApplyDamage(int damage) {
        _networkCurrentHp.Value -= damage;
        _networkCurrentHp.Value = _networkCurrentHp.Value;
    }

    #endregion

    #region Client RPCs

    /// <summary>
    /// Client RPC to destroy the game object across all clients.
    /// </summary>
    [ClientRpc]
    protected void DestroyGameObjectClientRpc() {
        Destroy(gameObject);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Destroys the resource node across all clients.
    /// </summary>
    protected void DestroyNodeAcrossNetwork() {
        Vector3Int position = Vector3Int.FloorToInt(transform.position);

        if (_cropsManager.GetCropTileAtPosition(position) != null) {
            _cropsManager.DestroyCropTileServerRpc(new Vector3IntSerializable(position), 0, ToolSO.ToolTypes.Pickaxe); // Destroy plant
            _cropsManager.DestroyCropTileServerRpc(new Vector3IntSerializable(position), 0, ToolSO.ToolTypes.Pickaxe); // Destroy cropTile
        }

        DestroyGameObjectClientRpc();
    }

    /// <summary>
    /// Starts the shake effect coroutine after the resource node is hit.
    /// Ensures only one coroutine runs at a time.
    /// </summary>
    protected void StartShakeEffect() {
        if (_shakeCoroutine != null) {
            StopCoroutine(_shakeCoroutine);
        }

        _shakeCoroutine = StartCoroutine(ShakeAfterHit());
    }

    /// <summary>
    /// Coroutine to handle the shake animation after being hit.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator ShakeAfterHit() {
        Vector3 originalPos = _originalPosition;

        for (int i = 0; i < SHAKE_ITERATIONS; i++) {
            transform.localPosition += new Vector3(SHAKE_AMOUNT_X, SHAKE_AMOUNT_Y, 0f);
            yield return new WaitForSeconds(TIME_BETWEEN_SHAKES);
            transform.localPosition = originalPos;
            yield return new WaitForSeconds(TIME_BETWEEN_SHAKES);
        }
    }

    /// <summary>
    /// Retrieves a random adjacent position around the given tree position.
    /// Utilizes a pre-defined offset array to minimize allocations.
    /// </summary>
    /// <param name="treePosition">The position of the tree.</param>
    /// <returns>A random adjacent Vector3 position.</returns>
    protected Vector3 GetRandomAdjacentPosition(Vector3Int treePosition) {
        int randomIndex = Random.Range(0, _adjacentOffsets.Length);
        Vector2 offset = _adjacentOffsets[randomIndex];
        return new Vector3(treePosition.x + offset.x, treePosition.y + offset.y, treePosition.z);
    }

    /// <summary>
    /// Abstract method for performing type-specific actions at the start of a new day.
    /// Must be implemented by subclasses.
    /// </summary>
    protected abstract void PerformTypeSpecificNextDayActions();

    /// <summary>
    /// Abstract method for playing sounds based on the resource node type.
    /// Must be implemented by subclasses.
    /// </summary>
    protected abstract void PlaySound();

    /// <summary>
    /// Abstract method for handling node destruction.
    /// Must be implemented by subclasses.
    /// </summary>
    protected abstract void HandleNodeDestruction();

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines if the resource node can be hit based on the provided types.
    /// </summary>
    /// <param name="canBeHit">Set of ResourceNodeTypes that can be hit.</param>
    /// <returns>True if the node type is in the set; otherwise, false.</returns>
    public abstract bool CanHitResourceNodeType(HashSet<ResourceNodeType> canBeHit);

    /// <summary>
    /// Sets the seed to drop for the resource node.
    /// </summary>
    /// <param name="seed">The ItemSO representing the seed.</param>
    public abstract void SetSeed(SeedSO seed);

    protected void HandleClientCallback(ServerRpcParams serverRpcParams, bool success) {
        // Get the client ID from the server RPC parameters
        var clientId = serverRpcParams.Receive.SenderClientId;
        // If the client is connected, remove the seed from the sender's inventory
        if (NetworkManager.ConnectedClients.ContainsKey(clientId)) {
            var client = NetworkManager.ConnectedClients[clientId];
            client.PlayerObject.GetComponent<PlayerToolsAndWeaponController>().ClientCallback(success);
        }
    }

    #endregion
}
