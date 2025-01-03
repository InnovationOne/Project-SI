using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(NetworkObject))]
public class PlaceableObjectsManager : NetworkBehaviour, IDataPersistance {
    public static PlaceableObjectsManager Instance { get; private set; }

    [SerializeField] bool _saveObjects;
    [SerializeField] bool _loadObjects;
    
    public NetworkList<PlaceableObjectData> PlaceableObjects { get; private set; }

    Tilemap _targetTilemap;
    ItemDatabaseSO _itemDatabase;
    CropsManager _cropsManager;

    [Header("Galaxy Rose")]
    [SerializeField] RoseSO _galaxyRose;
    [SerializeField] RuntimeAnimatorController _roseDestroyAnimator;

    // Position-to-index lookup
    Dictionary<Vector3Int, int> _positionToIndexMap;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one PlaceableObjectsManager instance found!");
            return;
        }
        Instance = this;

        _targetTilemap = GetComponent<Tilemap>();
        PlaceableObjects = new NetworkList<PlaceableObjectData>();
        PlaceableObjects.OnListChanged += OnPlaceableObjectsChanged;
        _positionToIndexMap = new Dictionary<Vector3Int, int>();
    }

    void Start() {
        _itemDatabase = ItemManager.Instance.ItemDatabase;
        _cropsManager = CropsManager.Instance;
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

    void HandlePlaceableObjectAdd(PlaceableObjectData placeableObject) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject netObj)) {
            VisualizePlaceableObjectOnMap(placeableObject, netObj);
        }

        if (!_positionToIndexMap.ContainsKey(placeableObject.Position)) {
            _positionToIndexMap[placeableObject.Position] = PlaceableObjects.Count - 1;
        }
    }

    void HandlePlaceableObjectRemoveAt(PlaceableObjectData placeableObject) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject netObj)) {
            netObj.Despawn();
        }
        _positionToIndexMap.Remove(placeableObject.Position);
    }

    void HandlePlaceableObjectValueChange(PlaceableObjectData placeableObject) {
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

    void VisualizePlaceableObjectOnMap(PlaceableObjectData obj, NetworkObject netObj) {
        if (netObj.TryGetComponent<PlaceableObject>(out var placeableObjectComp)) {
            placeableObjectComp.InitializePreLoad(obj.ObjectId);
        }

        netObj.GetComponent<IObjectDataPersistence>()?.LoadObject(obj.State);

        if (placeableObjectComp != null) {
            placeableObjectComp.InitializePostLoad();
        }
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
            State = string.Empty,
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
        ItemSpawnManager.Instance.SpawnItemServerRpc(
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

    public void SaveData(GameData data) {
        if (_saveObjects) {
            data.PlacedObjects = JsonConvert.SerializeObject(PlaceableObjects);
        }
    }

    public void LoadData(GameData data) {
        if (!IsServer || string.IsNullOrEmpty(data.PlacedObjects) || !_loadObjects) return;
        var list = JsonConvert.DeserializeObject<List<PlaceableObjectData>>(data.PlacedObjects);
        PlaceableObjects.Clear();
        _positionToIndexMap.Clear();

        for (int i = 0; i < list.Count; i++) {
            var o = list[i];
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(o.PrefabNetworkObjectId, out var netObj)) {
                VisualizePlaceableObjectOnMap(o, netObj);
            }
            CreatePlaceableObjectsPrefab(ref o, o.ObjectId);
            PlaceableObjects.Add(o);
            _positionToIndexMap[o.Position] = PlaceableObjects.Count - 1;
        }
    }

    public PlaceableObjectData? GetCropTileAtPosition(Vector3Int position) => _positionToIndexMap.TryGetValue(position, out int idx) ? PlaceableObjects[idx] : null;

    int FindPlaceableObjectIndexAtPosition(Vector3Int pos) => _positionToIndexMap.TryGetValue(pos, out var idx) ? idx : -1;


    void HandleItemReduction(ServerRpcParams serverRpcParams, int itemId) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject.TryGetComponent<PlayerInventoryController>(out var inventoryController)) {
                inventoryController.InventoryContainer.RemoveItem(new ItemSlot(itemId, 1, 0));
            } else {
                Debug.LogError($"PlayerInventoryController not found on Client {clientId}");
            }
        }
    }

    void HandleClientCallback(ServerRpcParams serverRpcParams, bool success) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            client.PlayerObject.GetComponent<PlayerToolsAndWeaponController>().ClientCallbackClientRpc(success);
        }
    }
}