using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

/// <summary>
/// This script visualizes the placed objects.
/// </summary>
public class PlaceableObjectsManager : NetworkBehaviour, IDataPersistance {
    public static PlaceableObjectsManager Instance { get; private set; }

    [SerializeField] private bool _saveObjects;
    [SerializeField] private bool _loadObjects;

    public NetworkList<PlaceableObjectData> PlaceableObjects { get; private set; }

    private Tilemap _targetTilemap;
    private ItemDatabaseSO _itemDatabase;
    private CropsManager _cropsManager;

    [Header("Galaxy Rose")]
    [SerializeField] private RoseSO _galaxyRose;
    [SerializeField] private RuntimeAnimatorController _roseDestroyAnimator;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of PlaceableObjectsManager in the scene!");
        }

        Instance = this;
        _targetTilemap = GetComponent<Tilemap>();

        PlaceableObjects = new NetworkList<PlaceableObjectData>(new List<PlaceableObjectData>());
        
        PlaceableObjects.OnListChanged += PlaceableObjects_OnListChanged;
    }

    private void Start() {
        _itemDatabase = ItemManager.Instance.ItemDatabase;
        _cropsManager = CropsManager.Instance;
    }


    #region Network List Change Handler

    private void PlaceableObjects_OnListChanged(NetworkListEvent<PlaceableObjectData> changeEvent) {
        switch (changeEvent.Type) {
            case NetworkListEvent<PlaceableObjectData>.EventType.Add:
                HandlePlaceableObjectAdd(changeEvent.Value);
                break;
            case NetworkListEvent<PlaceableObjectData>.EventType.RemoveAt:
                HandlePlaceableObjectRemove(changeEvent.Value);
                break;
            case NetworkListEvent<PlaceableObjectData>.EventType.Value:
                HandlePlaceableObjectValueChange(changeEvent.Value);
                break;
            case NetworkListEvent<PlaceableObjectData>.EventType.Clear:
                HandlePlaceableObjectsClear(changeEvent.Value);
                break;
        }
    }

    private void HandlePlaceableObjectAdd(PlaceableObjectData placeableObject) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject networkObject)) {
            VisualizePlaceableObjectOnMap(placeableObject, networkObject);
        }
    }

    private void HandlePlaceableObjectRemove(PlaceableObjectData placeableObject) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject networkObject)) {
            networkObject.Despawn();
        }
    }

    private void HandlePlaceableObjectValueChange(PlaceableObjectData placeableObject) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject networkObject)) {
            //isualizePlaceableObjectChanges(placeableObject, networkObject);
        }
    }

    private void HandlePlaceableObjectsClear(PlaceableObjectData placeableObject) {
        if (IsServer) {
            foreach (var cropTile in PlaceableObjects) {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject networkObject)) {
                    networkObject.Despawn();
                }
            }
        }
    }

    #endregion

    #region Visualize Object

    /// <summary>
    /// Visualizes the specified placeable object on the map.
    /// </summary>
    /// <param name="objectToPlace">The placeable object to visualize.</param>
    private void VisualizePlaceableObjectOnMap(PlaceableObjectData placeableObject, NetworkObject networkObject) {
        PlaceableObject placeableObjectComponent = networkObject.GetComponent<PlaceableObject>();
        if (placeableObjectComponent != null) {
            placeableObjectComponent.InitializePreLoad(placeableObject.ObjectId);
        }

        networkObject.GetComponent<IObjectDataPersistence>()?.LoadObject(placeableObject.State);

        if (placeableObjectComponent != null) {
            placeableObjectComponent.InitializePostLoad();
        }
    }
    #endregion

    private void CreatePlaceableObjectsPrefab(ref PlaceableObjectData placeableObject, int itemId) {
        // Ensure this is only executed on the server
        if (!IsServer) {
            Debug.LogError("CreatePlaceableObjectsPrefab should only be called on the server.");
            return;
        }

        // Instantiate the appropriate prefab
        ObjectSO objectSO = _itemDatabase[itemId] as ObjectSO;
        GameObject prefabInstance = Instantiate(objectSO.ObjectToSpawn, transform);

        // Position the prefab correctly
        Vector3 worldPosition = TilemapManager.Instance.AlignPositionToGridCenter(_targetTilemap.CellToWorld(placeableObject.Position));
        prefabInstance.transform.position = worldPosition + new Vector3(0, 0.5f);

        // Ensure the prefab has a NetworkObject component
        if (!prefabInstance.TryGetComponent(out NetworkObject networkObject)) {
            networkObject = prefabInstance.AddComponent<NetworkObject>();
        }

        // Spawn the prefab on the network
        networkObject.Spawn();

        // Assign the NetworkObjectId to CropTile
        placeableObject.PrefabNetworkObjectId = networkObject.NetworkObjectId;
    }


    #region Place Object  

    /// <summary>
    /// Places an object on the map on the server side.
    /// </summary>
    /// <param name="itemId">The ID of the item to place.</param>
    /// <param name="position">The position to place the object at.</param>
    /// <param name="serverRpcParams">Optional parameters for the server RPC.</param>
    [ServerRpc(RequireOwnership = false)]
    public void PlaceObjectOnMapServerRpc(Vector3IntSerializable positionSerializable, int itemId, ServerRpcParams serverRpcParams = default) {
        Vector3Int position = positionSerializable.ToVector3Int();
        ObjectSO objectSO = _itemDatabase[itemId] as ObjectSO;

        if (objectSO == null) {
            Debug.LogError($"Item with ID {itemId} is not an ObjectSO.");
            return;
        }

        if (IsPositionPlaced(position)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        if (_cropsManager.IsPositionSeeded(position)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleItemReduction(serverRpcParams, itemId);
        HandleClientCallback(serverRpcParams, true);

        PlaceableObjectData placeableObject = new PlaceableObjectData {
            ObjectId = itemId,
            Position = position,
            State = string.Empty,
            PrefabNetworkObjectId = 0,
        };

        CreatePlaceableObjectsPrefab(ref placeableObject, itemId);
        
        PlaceableObjects.Add(placeableObject);
    }

    #endregion

    #region Remove Object

    [ServerRpc(RequireOwnership = false)]
    public void PickUpObjectServerRpc(Vector3IntSerializable positionSerializable, ServerRpcParams serverRpcParams = default) {
        Vector3Int position = positionSerializable.ToVector3Int();

        if (!IsPositionPlaced(position)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        int index = FindPlaceableObjectIndexAtPosition(position);
        if (index < 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        PlaceableObjectData placeableObject = PlaceableObjects[index];

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(placeableObject.ObjectId, 1, 0),
            initialPosition: _targetTilemap.CellToWorld(placeableObject.Position),
            motionDirection: PlayerMovementController.LocalInstance.LastMotionDirection,
            spreadType: ItemSpawnManager.SpreadType.Circle);

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject networkObject)) {
            IInteractable interactable = networkObject.GetComponent<IInteractable>();
            interactable?.PickUpItemsInPlacedObject(Player.LocalInstance);
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject prefabNetworkObject)) {
            prefabNetworkObject.Despawn();
        }

        PlaceableObjects.RemoveAt(index);
    }

    #endregion


    #region Save & Load

    public void SaveData(GameData data) {
        if (_saveObjects) {
            data.PlacedObjects = JsonConvert.SerializeObject(PlaceableObjects);
        }
    }

    public void LoadData(GameData data) {
        if (!string.IsNullOrEmpty(data.CropsOnMap) && _loadObjects) {
            List<PlaceableObjectData> placeableObjectDataList = JsonConvert.DeserializeObject<List<PlaceableObjectData>>(data.PlacedObjects);
            PlaceableObjects.Clear();

            for (int i = 0; i < placeableObjectDataList.Count; i++) {
                PlaceableObjectData placeableObjectData = placeableObjectDataList[i];

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObjectData.PrefabNetworkObjectId, out NetworkObject networkObject)) {
                    VisualizePlaceableObjectOnMap(placeableObjectData, networkObject);
                }

                PlaceableObjects.Add(placeableObjectData);
                CreatePlaceableObjectsPrefab(ref placeableObjectData, placeableObjectData.ObjectId);
            }
        }
    }

    #endregion

    #region Utility Methods

    public PlaceableObjectData? GetCropTileAtPosition(Vector3Int position) {
        for (int i = 0; i < PlaceableObjects.Count; i++) {
            if (PlaceableObjects[i].Position.Equals(position)) {
                return PlaceableObjects[i];
            }
        }
        return null;
    }

    public bool IsPositionPlaced(Vector3Int position) {
        for (int i = 0; i < PlaceableObjects.Count; i++) {
            if (PlaceableObjects[i].Position.Equals(position) && PlaceableObjects[i].ObjectId >= 0) {
                return true;
            }
        }
        return false;
    }

    private int FindPlaceableObjectIndexAtPosition(Vector3Int position) {
        for (int i = 0; i < PlaceableObjects.Count; i++) {
            if (PlaceableObjects[i].Position.Equals(position)) {
                return i;
            }
        }
        return -1;
    }

    private void HandleItemReduction(ServerRpcParams serverRpcParams, int itemId) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject.TryGetComponent<PlayerInventoryController>(out var inventoryController)) {
                inventoryController.InventoryContainer.RemoveItem(new ItemSlot(itemId, 1, 0));
            } else {
                Debug.LogError($"PlayerInventoryController not found on Client {clientId}");
            }
        }
    }

    private void HandleClientCallback(ServerRpcParams serverRpcParams, bool success) {
        //if (TestCropsManager.Instance.Test) return;

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