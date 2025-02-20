using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(NetworkObject))]
public class PlaceableObjectsManager : NetworkBehaviour, IDataPersistance {
    public static PlaceableObjectsManager Instance { get; private set; }

    [SerializeField] bool _saveObjects;
    [SerializeField] bool _loadObjects;

    public NetworkList<PlaceableObjectData> PlaceableObjects { get; private set; }
    Dictionary<int, string> _stateDictionary = new Dictionary<int, string>();
    int _nextStateId = 1;

    [SerializeField] Tilemap _targetTilemap;
    ItemDatabaseSO _itemDatabase;
    CropsManager _cropsManager;

    // Position-to-index lookup
    Dictionary<Vector3Int, int> _positionToIndexMap;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one PlaceableObjectsManager instance found!");
            return;
        }
        Instance = this;

        PlaceableObjects = new NetworkList<PlaceableObjectData>();
        PlaceableObjects.OnListChanged += OnPlaceableObjectsChanged;
        _positionToIndexMap = new Dictionary<Vector3Int, int>();
    }

    void Start() {
        _itemDatabase = GameManager.Instance.ItemManager.ItemDatabase;
        _cropsManager = GameManager.Instance.CropsManager;
    }

    void OnPlaceableObjectsChanged(NetworkListEvent<PlaceableObjectData> changeEvent) {
        switch (changeEvent.Type) {
            case NetworkListEvent<PlaceableObjectData>.EventType.Add:
                HandlePlaceableObjectAdd(changeEvent.Value);
                break;
            case NetworkListEvent<PlaceableObjectData>.EventType.RemoveAt:
                HandlePlaceableObjectRemoveAt(changeEvent.Value);
                break;
            case NetworkListEvent<PlaceableObjectData>.EventType.Value:
                HandlePlaceableObjectValueChange(changeEvent.Value);
                break;
            case NetworkListEvent<PlaceableObjectData>.EventType.Clear:
                HandlePlaceableObjectsClear();
                break;
        }
    }

    void HandlePlaceableObjectAdd(PlaceableObjectData placeableObjectData) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObjectData.PrefabNetworkObjectId, out NetworkObject netObj)) {
            VisualizePlaceableObjectOnMap(placeableObjectData, netObj);
        }

        if (!_positionToIndexMap.ContainsKey(placeableObjectData.Position)) {
            _positionToIndexMap[placeableObjectData.Position] = PlaceableObjects.Count - 1;
        }
    }

    void HandlePlaceableObjectRemoveAt(PlaceableObjectData placeableObjectData) {
        RemoveObjectState(placeableObjectData.StateId);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObjectData.PrefabNetworkObjectId, out NetworkObject netObj)) {
            netObj.Despawn();
        }
        _positionToIndexMap.Remove(placeableObjectData.Position);
    }

    void HandlePlaceableObjectValueChange(PlaceableObjectData placeableObjectData) {
        // TODO: Update visuals if object state changes.
    }

    void HandlePlaceableObjectsClear() {
        if (!IsServer) return;
        foreach (var obj in PlaceableObjects) {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(obj.PrefabNetworkObjectId, out var netObj)) {
                netObj.Despawn();
            }
        }
        _positionToIndexMap.Clear();
    }

    void VisualizePlaceableObjectOnMap(PlaceableObjectData placeableObjectData, NetworkObject netObj) {
        if (netObj.TryGetComponent<PlaceableObject>(out var placeableObjectComp)) placeableObjectComp.InitializePreLoad(placeableObjectData.ObjectId);
        string state = GetObjectState(placeableObjectData.StateId);
        netObj.GetComponent<IObjectDataPersistence>()?.LoadObject(state);
        if (placeableObjectComp != null) placeableObjectComp.InitializePostLoad();
    }

    void CreatePlaceableObjectsPrefab(ref PlaceableObjectData placeableObject, int itemId) {
        if (_itemDatabase[itemId] is not ObjectSO objectSO) {
            Debug.LogError($"Item ID {itemId} is not a valid ObjectSO.");
            return;
        }

        var prefabInstance = Instantiate(objectSO.ObjectToSpawn, transform);
        Vector3 worldPos = _targetTilemap.GetCellCenterWorld(placeableObject.Position);
        prefabInstance.transform.position = worldPos + new Vector3(0, 0.5f, 0);

        // Apply rotation index by selecting the correct sprite or orientation
        if (prefabInstance.TryGetComponent<SpriteRenderer>(out var sr)) {
            int idx = placeableObject.RotationIdx;
            if (idx >= 0 && idx < objectSO.ObjectRotationSprites.Length) {
                sr.sprite = objectSO.ObjectRotationSprites[idx];
            }
        }

        if (!prefabInstance.TryGetComponent(out NetworkObject netObj)) {
            netObj = prefabInstance.AddComponent<NetworkObject>();
        }

        netObj.Spawn();
        placeableObject.PrefabNetworkObjectId = netObj.NetworkObjectId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlaceObjectOnMapServerRpc(Vector3IntSerializable posSer, int itemId, int rotationIdx, ServerRpcParams serverRpcParams = default) {
        var pos = posSer.ToVector3Int();
        if (_itemDatabase[itemId] is not ObjectSO) {
            Debug.LogError($"Item {itemId} is not ObjectSO.");
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        if (_cropsManager.IsPositionSeeded(pos) || FindPlaceableObjectIndexAtPosition(pos) >= 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleItemReduction(serverRpcParams, itemId);
        HandleClientCallback(serverRpcParams, true);

        PlaceableObjectData newObj = new() {
            ObjectId = itemId,
            RotationIdx = rotationIdx,
            Position = pos,
            StateId = RegisterObjectState(""),
            PrefabNetworkObjectId = 0
        };

        CreatePlaceableObjectsPrefab(ref newObj, itemId);
        PlaceableObjects.Add(newObj);
        _positionToIndexMap[pos] = PlaceableObjects.Count - 1;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickUpObjectServerRpc(Vector3IntSerializable posSer, ServerRpcParams serverRpcParams = default) {
        var pos = posSer.ToVector3Int();
        int idx = FindPlaceableObjectIndexAtPosition(pos);
        if (idx < 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleClientCallback(serverRpcParams, true);

        var obj = PlaceableObjects[idx];
        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            new ItemSlot(obj.ObjectId, 1, 0),
            _targetTilemap.CellToWorld(obj.Position),
            PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
            spreadType: ItemSpawnManager.SpreadType.Circle);

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(obj.PrefabNetworkObjectId, out var netObj)) {
            netObj.GetComponent<IInteractable>()?.PickUpItemsInPlacedObject(PlayerController.LocalInstance);
            netObj.Despawn();
        }

        PlaceableObjects.RemoveAt(idx);
        _positionToIndexMap.Remove(pos);
    }

    #region -------------------- Save & Load --------------------

    public void SaveData(GameData data) {
        if (_saveObjects) {
            var normalList = new List<PlaceableObjectData>();
            for (int i = 0; i < PlaceableObjects.Count; i++) {
                normalList.Add(PlaceableObjects[i]);
            }
            data.PlacedObjects = JsonConvert.SerializeObject(normalList);
            data.PlaceableObjectStates = JsonConvert.SerializeObject(_stateDictionary);
        }
    }

    public void LoadData(GameData data) {
        if (!IsServer || string.IsNullOrEmpty(data.PlacedObjects) || !_loadObjects) return;
        if (!string.IsNullOrEmpty(data.PlaceableObjectStates)) {
            _stateDictionary = JsonConvert.DeserializeObject<Dictionary<int, string>>(data.PlaceableObjectStates);
            _nextStateId = _stateDictionary.Count > 0 ? _stateDictionary.Keys.Max() + 1 : 1;
        } else {
            _stateDictionary = new Dictionary<int, string>();
            _nextStateId = 1;
        }

        var list = JsonConvert.DeserializeObject<List<PlaceableObjectData>>(data.PlacedObjects);
        if (list == null) {
            Debug.LogWarning("No valid PlaceableObjectData found in the JSON!");
            return;
        }

        PlaceableObjects.Clear();
        _positionToIndexMap.Clear();

        for (int i = 0; i < list.Count; i++) {
            var o = list[i];
            CreatePlaceableObjectsPrefab(ref o, o.ObjectId);
            PlaceableObjects.Add(o);
            _positionToIndexMap[o.Position] = PlaceableObjects.Count - 1;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(o.PrefabNetworkObjectId, out var netObj)) {
                VisualizePlaceableObjectOnMap(o, netObj);
            }
        }
    }

    #endregion -------------------- Save & Load --------------------

    public PlaceableObjectData? GetCropTileAtPosition(Vector3Int position) => _positionToIndexMap.TryGetValue(position, out int idx) ? PlaceableObjects[idx] : null;

    int FindPlaceableObjectIndexAtPosition(Vector3Int pos) => _positionToIndexMap.TryGetValue(pos, out var idx) ? idx : -1;

    void HandleItemReduction(ServerRpcParams serverRpcParams, int itemId) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject.TryGetComponent<PlayerInventoryController>(out var inventoryController)) {
                inventoryController.InventoryContainer.RemoveItem(new ItemSlot(itemId, 1, 0));
            } else Debug.LogError($"PlayerInventoryController not found on Client {clientId}");
        }
    }

    void HandleClientCallback(ServerRpcParams serverRpcParams, bool success) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            client.PlayerObject.GetComponent<PlayerToolsAndWeaponController>().ClientCallbackClientRpc(success);
        }
    }

    // Liefert den Zustand anhand des Schlüssels.
    public string GetObjectState(int stateId) {
        return _stateDictionary.TryGetValue(stateId, out var state) ? state : "";
    }

    // Registriert einen neuen Zustand und gibt einen eindeutigen Schlüssel zurück.
    public int RegisterObjectState(string state) {
        int id = _nextStateId++;
        _stateDictionary[id] = state;
        return id;
    }

    public void RemoveObjectState(int stateId) {
        _stateDictionary.Remove(stateId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlaceableObjectStateServerRpc(Vector3IntSerializable cellPosSer, string newState) {
        var cellPos = cellPosSer.ToVector3Int();
        for (int i = 0; i < PlaceableObjects.Count; i++) {
            if (PlaceableObjects[i].Position == cellPos) {
                _stateDictionary[PlaceableObjects[i].StateId] = newState;
                break;
            }
        }
    }
}