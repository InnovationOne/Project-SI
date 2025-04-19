using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(NetworkObject))]
public class PlaceableObjectsManager : NetworkBehaviour, IDataPersistance {
    public static PlaceableObjectsManager Instance { get; private set; }

    [Header("Debug: Saving and Loading")]
    [SerializeField] private bool _saveObjects;
    [SerializeField] private bool _loadObjects;

    [Header("References")]
    [SerializeField] private Tilemap _targetTilemap;
    [SerializeField] private LayerMask[] _forbiddenCollisionLayers;
    public LayerMask CombinedForbiddenLayerMask => CombineMasks(_forbiddenCollisionLayers);

    private readonly Dictionary<ulong, PlaceableObjectData> _objectDataByNetId = new();
    private ItemDatabaseSO _itemDatabase;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one PlaceableObjectsManager instance found!");
            return;
        }
        Instance = this;
    }

    void Start() {
        _itemDatabase = ItemManager.Instance.ItemDatabase;
    }

    /// <summary>
    /// Places an object on the map. This RPC is invoked on the server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PlaceObjectOnMapServerRpc(Vector3IntSerializable posSer, int itemId, int rotationIdx, ServerRpcParams rpcParams = default) {
        Vector3Int pos = posSer.ToVector3Int();

        if (_itemDatabase[itemId] is not ObjectSO objectSO) {
            Debug.LogError($"Item {itemId} is not a valid ObjectSO.");
            HandleClientCallback(rpcParams, false);
            return;
        }

        Vector2Int size = objectSO.OccupiedSizeInCells;
        if (size.x < 1 || size.y < 1) size = Vector2Int.one;

        var grid = _targetTilemap.GetComponentInParent<Grid>();
        var checker = new CollisionChecker(grid, CombineMasks(_forbiddenCollisionLayers));
        if (checker.CheckCollision(checker.CalculateOccupiedCells(pos, size))) {
            HandleClientCallback(rpcParams, false);
            return;
        }

        // Process item reduction and notify client.
        HandleItemReduction(rpcParams, itemId);
        HandleClientCallback(rpcParams, true);

        GameObject instance = Instantiate(objectSO.PrefabToSpawn, transform);
        instance.transform.position = _targetTilemap.GetCellCenterWorld(pos) + new Vector3(0, 0.5f, 0);

        if (!instance.TryGetComponent(out NetworkObject netObj)) {
            netObj = instance.AddComponent<NetworkObject>();
        }

        netObj.Spawn();

        var data = new PlaceableObjectData {
            ObjectId = itemId,
            RotationIdx = rotationIdx,
            Position = pos,
            State = ""
        };

        _objectDataByNetId[netObj.NetworkObjectId] = data;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickUpObjectServerRpc(ulong netId, bool dropItem = true, ServerRpcParams rpcParams = default) {
        if (!_objectDataByNetId.TryGetValue(netId, out var data)) {
            HandleClientCallback(rpcParams, false);
            return;
        }

        HandleClientCallback(rpcParams, true);

        // Spawn Item
         if (dropItem) GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                        new ItemSlot(data.ObjectId, 1, 0),
                        _targetTilemap.CellToWorld(data.Position),
                        PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                        spreadType: ItemSpawnManager.SpreadType.Circle);

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var netObj)) {
            netObj.GetComponent<IInteractable>()?.PickUpItemsInPlacedObject(PlayerController.LocalInstance);
            netObj.Despawn();
        }

        _objectDataByNetId.Remove(netId);
    }

    /// <summary>
    /// Updates the state of a placed object based on client request.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UpdateObjectStateServerRpc(ulong netId, string newState) {
        if (_objectDataByNetId.TryGetValue(netId, out var data)) {
            data.State = newState;
            _objectDataByNetId[netId] = data;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var netObj)) {
                if (netObj.TryGetComponent<PlaceableObject>(out var po)) {
                    po.UpdateState(newState);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestObjectStateServerRpc(ulong requesterNetId, string callbackName, ServerRpcParams rpcParams = default) {
        string state = _objectDataByNetId.TryGetValue(requesterNetId, out var data) ? data.State : "";

        ClientRpcParams clientParams = new() {
            Send = new ClientRpcSendParams {
                TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId }
            }
        };

        SendStateToObjectClientRpc(state, requesterNetId, callbackName, clientParams);
    }

    [ClientRpc]
    private void SendStateToObjectClientRpc(string state, ulong targetNetObjId, string callbackName, ClientRpcParams clientRpcParams = default) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var obj)) {
            if (obj.TryGetComponent<PlaceableObject>(out var po)) {
                po.UpdateState(state);
                po.OnStateReceivedCallback(callbackName);
            }
        }
    }

    private void HandleItemReduction(ServerRpcParams rpcParams, int itemId) {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerInventoryController>(out var inventoryController)) {
                inventoryController.InventoryContainer.RemoveItem(new ItemSlot(itemId, 1, 0));
            }
        }
    }

    private void HandleClientCallback(ServerRpcParams rpcParams, bool success) {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerToolsAndWeaponController>(out var playerToolsAndWeaponController)) {
                playerToolsAndWeaponController.ClientCallbackClientRpc(success);
            }
        }
    }

    private static LayerMask CombineMasks(LayerMask[] masks) {
        int combined = 0;
        foreach (var m in masks) combined |= m.value;
        return combined;
    }

    public bool TryGetNetworkIdAt(Vector3Int cell, out ulong netId) {
        foreach (var kv in _objectDataByNetId) {
            if (kv.Value.Position == cell) {
                netId = kv.Key;
                return true;
            }
        }
        netId = 0;
        return false;
    }

    public PlaceableObjectData GetData(ulong networkId) {
        return _objectDataByNetId[networkId];
    }

    public void Cheat_RemoveAllObjects() {
        foreach (var kv in _objectDataByNetId) {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(kv.Key, out var netObj)) {
                netObj.Despawn();
            }
        }
        _objectDataByNetId.Clear();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReplacePrefabServerRpc(ulong netId, string finishedPrefabName, ServerRpcParams rpcParams = default) {
        if (!_objectDataByNetId.TryGetValue(netId, out var data)) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var oldObj)) {
            oldObj.Despawn();
        }
        _objectDataByNetId.Remove(netId);

        // Neues Prefab instanziieren
        if (_itemDatabase[data.ObjectId] is BuildingSO bso && bso.FinishedBuildingPrefab.name == finishedPrefabName) {
            Vector3 pos = _targetTilemap.GetCellCenterWorld(data.Position) + new Vector3(0, 0.5f, 0);
            GameObject inst = Instantiate(bso.FinishedBuildingPrefab, transform);
            inst.transform.position = pos;
            var netObj = inst.GetComponent<NetworkObject>() ?? inst.AddComponent<NetworkObject>();
            netObj.Spawn();

            // Daten aktualisieren
            var newData = new PlaceableObjectData { ObjectId = data.ObjectId, RotationIdx = data.RotationIdx, Position = data.Position, State = "" };
            _objectDataByNetId[netObj.NetworkObjectId] = newData;
        }
    }

    #region Save & Load

    public void SaveData(GameData data) {
        if (!IsServer || !_saveObjects) return;
        var list = new List<PlaceableObjectData>();

        foreach (var kv in _objectDataByNetId) {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(kv.Key, out var netObj)) {
                if (netObj.TryGetComponent<PlaceableObject>(out var po)) {
                    var dataCopy = kv.Value;
                    dataCopy.State = po.SaveObject();
                    list.Add(dataCopy);
                }
            }
        }

        data.PlacedObjects = JsonConvert.SerializeObject(list);
    }

    public void LoadData(GameData data) {
        if (!IsServer || string.IsNullOrEmpty(data.PlacedObjects) || !_loadObjects) return;

        _objectDataByNetId.Clear();
        var list = JsonConvert.DeserializeObject<List<PlaceableObjectData>>(data.PlacedObjects);

        foreach (var objData in list) {
            if (_itemDatabase[objData.ObjectId] is not ObjectSO so) continue;
            GameObject instance = Instantiate(so.PrefabToSpawn, transform);
            instance.transform.position = _targetTilemap.GetCellCenterWorld(objData.Position) + new Vector3(0, 0.5f, 0);

            if (!instance.TryGetComponent(out NetworkObject netObj)) {
                netObj = instance.AddComponent<NetworkObject>();
            }

            netObj.Spawn();
            _objectDataByNetId[netObj.NetworkObjectId] = objData;

            if (instance.TryGetComponent<PlaceableObject>(out var po)) {
                po.InitializePreLoad(objData.ObjectId);
                po.LoadObject(objData.State);
                po.InitializePostLoad();
            }
        }
    }

    #endregion
}