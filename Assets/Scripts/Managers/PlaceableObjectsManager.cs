using Newtonsoft.Json;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// This script visualizes the placed objects.
/// </summary>
public class PlaceableObjectsManager : NetworkBehaviour, IDataPersistance {
    /// <summary>
    /// Singleton instance of the PlaceableObjectsManager.
    /// </summary>
    public static PlaceableObjectsManager Instance { get; private set; }

    [Header("Reference: Database")]
    private PlaceableObjectsContainer _poContainer;
    /// <summary>
    /// Container for all placed objects.
    /// </summary>
    public PlaceableObjectsContainer POContainer => _poContainer;

    [Header("Reference: Tilemap")]
    private Tilemap _targetTilemap;

    [Header("Reference: Prefab")]
    [SerializeField] private GameObject _placeableObjectPrefab;

    /// <summary>
    /// This method is called when the script instance is being loaded.
    /// It checks if an instance of the script already exists and destroys the game object if it does.
    /// Otherwise, it sets the instance to this script and initializes the components.
    /// </summary>
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of PlaceableObjectsManager in the scene!");
        }

        Instance = this;
        InitializeComponents();
    }

    /// <summary>
    /// Initializes the components required for the PlaceableObjectsManager.
    /// </summary>
    private void InitializeComponents() {
        _targetTilemap = GetComponent<Tilemap>();
        _poContainer = new PlaceableObjectsContainer();
    }

    /// <summary>
    /// Called when the object is spawned on the network.
    /// </summary>
    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnected;
        }
    }

    #region Client Late Join & Network Sync    
    /// <summary>
    /// Event handler for when a client is connected to the network.
    /// </summary>
    /// <param name="clientId">The ID of the connected client.</param>
    private void NetworkManager_OnClientConnected(ulong clientId) => NetworkManager_OnClientConnectedClientRpc(clientId, JsonConvert.SerializeObject(_poContainer.SerializePlaceableObjectsContainer()));

    
    /// <summary>
    /// This method is a ClientRpc that is called when a client connects to the server.
    /// It receives the client ID and a JSON string representing a POContainer object.
    /// If the client ID matches the local client ID and the current instance is not a server,
    /// it unboxes the POContainer object from the JSON string.
    /// </summary>
    /// <param name="clientId">The ID of the client that connected.</param>
    /// <param name="poContainerJson">The JSON string representing a POContainer object.</param>
    [ClientRpc]
    private void NetworkManager_OnClientConnectedClientRpc(ulong clientId, string poContainerJson) {
        if (clientId == NetworkManager.Singleton.LocalClientId && !IsServer) {
            UnboxPOContainerJson(poContainerJson);
        }
    }

    /// <summary>
    /// Unboxes the placeable object container JSON and adds the placeable objects to the manager.
    /// </summary>
    /// <param name="poContainerJson">The JSON string representing the placeable object container.</param>
    private void UnboxPOContainerJson(string poContainerJson) {
        foreach (var po in _poContainer.DeserializePlaceableObjecteContainer(poContainerJson)) {
            _poContainer.Add(po.Position, po);
            VisualizeObjectOnMap(po);
        }
    }

    /// <summary>
    /// Handles the reduction of an item in the player's inventory.
    /// </summary>
    /// <param name="serverRpcParams">The parameters of the server RPC call.</param>
    /// <param name="itemId">The ID of the item to be reduced.</param>
    private void HandleItemReduction(ServerRpcParams serverRpcParams, int itemId) {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId)) {
            var client = NetworkManager.ConnectedClients[clientId];
            client.PlayerObject.GetComponent<PlayerInventoryController>().InventoryContainer.RemoveItem(new ItemSlot(itemId, 1, 0));
        }
    }
    #endregion


    #region Visualize Object
    /// <summary>
    /// Visualizes the specified placeable object on the map.
    /// </summary>
    /// <param name="objectToPlace">The placeable object to visualize.</param>
    private void VisualizeObjectOnMap(PlaceableObject objectToPlace) {
        GameObject gameObject = Instantiate(
            _placeableObjectPrefab, 
            TilemapManager.Instance.AlignPositionToGridCenter(_targetTilemap.CellToWorld(objectToPlace.Position)), 
            Quaternion.identity);

        switch ((ItemManager.Instance.ItemDatabase[objectToPlace.ObjectId] as ObjectSO).ObjectType) {
            case ObjectSO.ObjectTypes.ItemProducer:
                gameObject.AddComponent<ItemProducer>().Initialize(objectToPlace.ObjectId);
                break;
            case ObjectSO.ObjectTypes.ItemConverter:
                gameObject.AddComponent<ItemConverter>().Initialize(objectToPlace.ObjectId);
                break;
            case ObjectSO.ObjectTypes.Chest:
                gameObject.AddComponent<Chest>().Initialize(objectToPlace.ObjectId);
                break;
            case ObjectSO.ObjectTypes.Bed:
                gameObject.AddComponent<Bed>().Initialize(objectToPlace.ObjectId);
                break;
            case ObjectSO.ObjectTypes.Fence:
                gameObject.AddComponent<Fence>().Initialize(objectToPlace.ObjectId);
                break;
            case ObjectSO.ObjectTypes.Gate:
                gameObject.AddComponent<Gate>().Initialize(objectToPlace.ObjectId);
                break;
            case ObjectSO.ObjectTypes.Sprinkler:
                gameObject.AddComponent<Sprinkler>().Initialize(objectToPlace.ObjectId);
                break;
            default:
                Debug.LogError("Placeable object for this objecttype isn't implimented!");
                break;
        }

        LoadObjectState(gameObject, objectToPlace.State);
        
        objectToPlace.Prefab = gameObject;
    }

    /// <summary>
    /// Loads the state of a game object using the provided object state.
    /// </summary>
    /// <param name="gameObject">The game object to load the state for.</param>
    /// <param name="objectState">The object state to load.</param>
    private void LoadObjectState(GameObject gameObject, string objectState) {
        IObjectDataPersistence persistence = gameObject.GetComponent<IObjectDataPersistence>();
        persistence?.LoadObject(objectState);
    }
    #endregion


    #region Place Object    
    /// <summary>
    /// Places an object on the map on the server side.
    /// </summary>
    /// <param name="itemId">The ID of the item to place.</param>
    /// <param name="position">The position to place the object at.</param>
    /// <param name="serverRpcParams">Optional parameters for the server RPC.</param>
    [ServerRpc(RequireOwnership = false)]
    public void PlaceObjectOnMapServerRpc(int itemId, Vector3Int position, ServerRpcParams serverRpcParams = default) {
        if (!_poContainer.PlaceableObjects.ContainsKey(position) && !CropsManager.Instance.CropTileContainer.IsPositionSeeded(position)) {
            HandleItemReduction(serverRpcParams, itemId);
            PlaceObjectOnMapClientRpc(itemId, position);
        }
    }

    /// <summary>
    /// Places an object on the map for all clients using a Remote Procedure Call (RPC).
    /// </summary>
    /// <param name="itemId">The ID of the item to be placed.</param>
    /// <param name="positionOnGrid">The position of the object on the grid.</param>
    [ClientRpc]
    private void PlaceObjectOnMapClientRpc(int itemId, Vector3Int positionOnGrid) {
        PlaceableObject placeableObject = new PlaceableObject {
            ObjectId = itemId,
            Position = positionOnGrid
        };

        VisualizeObjectOnMap(placeableObject);
        _poContainer.Add(positionOnGrid, placeableObject);
    }
    #endregion


    #region Remove Object
    /// <summary>
    /// Server RPC method for picking up an object at the specified grid position.
    /// </summary>
    /// <param name="gridPosition">The grid position of the object to pick up.</param>
    /// <param name="serverRpcParams">Optional parameters for the server RPC.</param>
    [ServerRpc(RequireOwnership = false)]
    public void PickUpObjectServerRpc(Vector3Int gridPosition, ServerRpcParams serverRpcParams = default) {
        if (!CanPickUpObject(gridPosition)) {
            return;
        }

        PickUpObjectClientRpc(gridPosition);
    }

    /// <summary>
    /// Checks if an object can be picked up at the specified grid position.
    /// </summary>
    /// <param name="gridPosition">The grid position to check.</param>
    /// <returns>True if the object can be picked up, false otherwise.</returns>
    private bool CanPickUpObject(Vector3Int gridPosition) {
        if (!_poContainer.PlaceableObjects.ContainsKey(gridPosition)) {
            return false;
        }

        var placedObject = _poContainer[gridPosition];
        var objectSO = ItemManager.Instance.ItemDatabase[placedObject.ObjectId] as ObjectSO;

        return objectSO.ItemToPickUpObject.ItemId == PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId;
    }

    /// <summary>
    /// Sends a client RPC to pick up an object at the specified grid position.
    /// </summary>
    /// <param name="gridPosition">The grid position of the object to pick up.</param>
    [ClientRpc]
    private void PickUpObjectClientRpc(Vector3Int gridPosition) {
        var worldPosition = _targetTilemap.CellToWorld(gridPosition);
        var placedObject = _poContainer[gridPosition];

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(placedObject.ObjectId, 1, 0),
            initialPosition: worldPosition,
            motionDirection: PlayerMovementController.LocalInstance.LastMotionDirection,
            spreadType: ItemSpawnManager.SpreadType.Circle);


        HandlePickUpInteraction(placedObject.Prefab.gameObject);
        CleanUpPlacedObject(gridPosition, placedObject);
    }

    /// <summary>
    /// Handles the pick-up interaction for a given GameObject.
    /// </summary>
    /// <param name="gameObject">The GameObject to handle the pick-up interaction for.</param>
    private void HandlePickUpInteraction(GameObject gameObject) {
        var interactable = gameObject.GetComponent<Interactable>();
        var fenceBehaviour = gameObject.GetComponent<Fence>();

        if (interactable != null) {
            interactable.PickUpItemsInPlacedObject(Player.LocalInstance);
        } else if (fenceBehaviour != null) {
            fenceBehaviour.PickUp();
        }
    }

    /// <summary>
    /// Cleans up a placed object by destroying its prefab game object and removing it from the container.
    /// </summary>
    /// <param name="gridPosition">The grid position of the placed object.</param>
    /// <param name="placedObject">The placed object to clean up.</param>
    private void CleanUpPlacedObject(Vector3Int gridPosition, PlaceableObject placedObject) {
        Destroy(placedObject.Prefab);
        _poContainer.Remove(gridPosition);
    }
    #endregion


    #region Save & Load
    public void SaveData(GameData data) {
        data.PlacedObjects = _poContainer.SerializePlaceableObjectsContainer();
    }

    public void LoadData(GameData data) {
        if (!string.IsNullOrEmpty(data.PlacedObjects)) {
            UnboxPOContainerJson(data.PlacedObjects);
        }
    }
    #endregion
}