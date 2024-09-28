using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum ResourceNodeType {
    Tree,
    Ore,
    Branch,
    TreeStump,
}

[RequireComponent(typeof(BoxCollider2D))]
public class ResourceNode : NetworkBehaviour {

    #region Serialized Fields

    [Header("Node Settings")]
    [SerializeField] private ResourceNodeType _nodeType;
    [SerializeField] private ItemSpawnManager.SpreadType _spreadType;
    [SerializeField] private int _startingHP;
    [SerializeField] private int _minimumToolRarity;
    [SerializeField] private ItemSO _beeNest;

    [Header("Item Slot Settings")]
    [SerializeField] private ItemSO _itemSO;
    [SerializeField] private int _minDropCount;
    [SerializeField] private int _maxDropCount;
    [SerializeField] private int _rarityID;

    #endregion

    #region Private Fields

    // Constant variables
    private const float SHAKE_AMOUNT_X = 0.05f;
    private const float SHAKE_AMOUNT_Y = 0.01f;
    private const float TIME_BETWEEN_SHAKES = 0.03f;
    private const float BEE_NEST_PROBABILITY = 0.05f;
    private const int SHAKE_ITERATIONS = 3;

    // Cached singleton instances
    private TimeAndWeatherManager _timeAndWeatherManager;
    private PlayerToolbeltController _playerToolbeltController;
    private CropsManager _cropsManager;
    private PlayerMovementController _playerMovementController;
    private AudioManager _audioManager;
    private FMODEvents _fmodEvents;

    // Private variables
    private bool _hitToday;
    private Vector3 _originalPosition; 
    private int _currentHp;
    private BoxCollider2D _boxCollider2D;

    // Network Variables
    private NetworkVariable<int> _networkCurrentHp = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _networkHitToday = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    #endregion

    #region Unity Callbacks

    private void Awake() {
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _originalPosition = transform.localPosition;
    }

    public override void OnNetworkSpawn() {
        // Cache singleton instances
        _timeAndWeatherManager = TimeAndWeatherManager.Instance;
        _playerToolbeltController = PlayerToolbeltController.LocalInstance;
        _cropsManager = CropsManager.Instance;
        _playerMovementController = PlayerMovementController.LocalInstance;
        _audioManager = AudioManager.Instance;
        _fmodEvents = FMODEvents.Instance;

        _currentHp = _startingHP;

        if (IsServer) {
            _networkCurrentHp.Value = _currentHp;
            _networkHitToday.Value = _hitToday;

            if (_timeAndWeatherManager != null) {
                _timeAndWeatherManager.OnNextDayStarted += OnNextDayStarted;
            } else {
                Debug.LogError("TimeAndWeatherManager instance is not found.");
            }
        }

        // Subscribe to network variable changes on clients
        _networkCurrentHp.OnValueChanged += OnCurrentHpChanged;
        _networkHitToday.OnValueChanged += OnHitTodayChanged;
    }

    public override void OnNetworkDespawn() {
        if (IsServer && _timeAndWeatherManager != null) {
            _timeAndWeatherManager.OnNextDayStarted -= OnNextDayStarted;
        }

        _networkCurrentHp.OnValueChanged -= OnCurrentHpChanged;
        _networkHitToday.OnValueChanged -= OnHitTodayChanged;
    }

    #endregion

    #region Event Handlers

    private void OnNextDayStarted() {
        _currentHp = _startingHP;
        _networkCurrentHp.Value = _currentHp;
        _networkHitToday.Value = false;
    }

    private void OnCurrentHpChanged(int oldValue, int newValue) {
        _currentHp = newValue;
        PlaySound();

        if (_currentHp > 0) {
            StartCoroutine(ShakeAfterHit());
        } else {
            // Node destroyed, visual updates handled via DestroyGameObjectClientRpc
        }
    }

    private void OnHitTodayChanged(bool oldValue, bool newValue) {
        _hitToday = newValue;
    }

    #endregion

    #region Server RPCs

    /// <summary>
    /// Server RPC to handle hitting the resource node.
    /// </summary>
    /// <param name="damage">Damage to apply.</param>
    [ServerRpc(RequireOwnership = false)]
    public void HitResourceNodeServerRpc(int damage, ServerRpcParams rpcParams = default) {
        var selectedTool = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();

        if (selectedTool.RarityId < _minimumToolRarity) {
            Debug.Log("Tool rarity too low.");
            // Play bounce back animation & sound
            return;
        }

        CropTile cropTile = null;

        if (_nodeType == ResourceNodeType.Tree) {
            Vector3Int pos = Vector3Int.FloorToInt(transform.position);
            cropTile = _cropsManager.CropTileContainer.GetCropTileAtPosition(pos);

            if (cropTile == null || !cropTile.IsCropDoneGrowing(_cropsManager.CropDatabase[cropTile.CropId])) {
                return;
            }
        }

        // Apply damage
        _currentHp -= damage;
        _networkCurrentHp.Value = _currentHp;

        // It is a tree
        if (cropTile != null) {
            HandleCropTileOnHit(cropTile);
        }

        if (_currentHp <= 0) {
            // Node is destroyed
            HandleNodeDestruction();
        }
    }

    /// <summary>
    /// Handles interactions with the crop tile when the node is hit.
    /// </summary>
    /// <param name="cropTile">The crop tile being interacted with.</param>
    private void HandleCropTileOnHit(CropTile cropTile) {
        if (cropTile.IsCropDoneGrowing(_cropsManager.CropDatabase[cropTile.CropId])) {
            _cropsManager.HarvestCropServerRpc(cropTile.CropPosition);
            return;
        }

        if (!_hitToday && Random.value > BEE_NEST_PROBABILITY) {
            // Spawn bee nest item
            var beeNestSlot = new ItemSlot(_beeNest.ItemId, 1, 0);
            ItemSpawnManager.Instance.SpawnItemServerRpc(
                itemSlot: beeNestSlot,
                initialPosition: transform.position,
                motionDirection: Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle
            );

            // TODO: Implement bee attack on player
            _hitToday = true;
            _networkHitToday.Value = _hitToday;
        }
    }

    /// <summary>
    /// Handles the destruction of the resource node.
    /// </summary>
    private void HandleNodeDestruction() {
        // Spawn items
        int dropCount = Random.Range(_minDropCount, _maxDropCount + 1);
        Vector3 spawnPosition = new(
            transform.position.x + _boxCollider2D.offset.x,
            transform.position.y + _boxCollider2D.offset.y,
            transform.position.z
        );

        var itemSlot = new ItemSlot(_itemSO.ItemId, dropCount, _rarityID);
        var motionDirection = _playerMovementController.LastMotionDirection;

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: itemSlot,
            initialPosition: spawnPosition,
            motionDirection: motionDirection,
            spreadType: _spreadType
        );

        // Destroy the node across all clients
        DestroyGameObjectServerRpc();
    }

    /// <summary>
    /// Server RPC to destroy the resource node game object.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void DestroyGameObjectServerRpc(ServerRpcParams rpcParams = default) {
        Vector3Int position = Vector3Int.FloorToInt(transform.position);

        if (_cropsManager.CropTileContainer.GetCropTileAtPosition(position) != null) {
            _cropsManager.DestroyCropTilePlantClientRpc(position);
            _cropsManager.DestroyCropTileClientRpc(position);
        }

        DestroyGameObjectClientRpc();
    }

    #endregion

    #region Client RPCs

    /// <summary>
    /// Client RPC to destroy the game object across all clients.
    /// </summary>
    [ClientRpc]
    private void DestroyGameObjectClientRpc() {
        Destroy(gameObject);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Plays the appropriate sound based on the resource node type.
    /// </summary>
    private void PlaySound() {
        switch (_nodeType) {
            case ResourceNodeType.Tree:
                _audioManager.PlayOneShot(_fmodEvents.HitTreeSFX, transform.position);
                break;
            case ResourceNodeType.Ore:
                //_audioManager.PlayOneShot(_fmodEvents.HitOreSFX, transform.position);
                break;
            default:
                Debug.LogWarning($"Unhandled ResourceNodeType: {_nodeType}");
                break;
        }
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

    #endregion

    public bool CanHitResourceNodeType(HashSet<ResourceNodeType> canBeHit) => canBeHit.Contains(_nodeType);
}
