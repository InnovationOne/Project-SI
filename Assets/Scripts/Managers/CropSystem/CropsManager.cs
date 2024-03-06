using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages the crops in the game, including planting, growing, and harvesting.
/// Implements the IDataPersistance interface for data persistence.
/// </summary>
public class CropsManager : NetworkBehaviour, IDataPersistance {
    public static CropsManager Instance { get; private set; }

    [Header("Debug: Save and Load")]
    [SerializeField] private bool _saveCrops = true;
    [SerializeField] private bool _loadCrops = true;

    [Header("Params")]
    [SerializeField] private float _cropPositionSpread = 0.1f; //Spread of the crop sprite renderer position
    [SerializeField] private int _notWateredDamage = 25; //Damage applied to the crop if not watered
    [SerializeField] private int _maxCropDamage = 100;
    [SerializeField] private int _probabilityToDeleteUnseededTile = 25; //Chance in percent to delete unseeded tile
    [SerializeField] private int[] _probabilityToSpawnRarity = { 30, 15, 5 }; //30% rare, 15% epic, 5% legendary

    [Header("Reference: TileBases")]
    [SerializeField] private RuleTile _dirtDry;
    [SerializeField] private RuleTile _dirtWet;
    [SerializeField] private RuleTile _dirtPlowedDry;
    [SerializeField] private RuleTile _dirtPlowedWet;
    [SerializeField] private TileBase[] _tilesThatCanBePlowed;

    [Header("Reference: After Harvest")]
    [SerializeField] private Sprite _cropHole;

    [Header("Reference: Prefab")]
    [SerializeField] private HarvestCrop _cropsSpritePrefab;

    private Tilemap _targetTilemap;
    [SerializeField] private CropDatabaseSO _cropDatabase;
    public CropTileContainer CropTileContainer { get; private set; }
    private TilemapManager _tilemapReadManager;
    private PlaceableObjectsManager _placeableObjectsManager;

    private const int TRANSFER_BATCH_SIZE = 80;


    /// <summary>
    /// This method is called when the script instance is being loaded.
    /// It initializes the CropsManager singleton instance, sets up the required components,
    /// and configures the serialization settings for Vector3.
    /// </summary>
    private void Awake() {
        // Check if there is more than one instance of CropsManager in the scene
        if (Instance != null) {
            Debug.LogError("There is more than one instance of CropsManager in the scene!");
            return;
        }
        // Set the current instance as the singleton instance
        Instance = this;

        // Get the Tilemap component attached to the same GameObject
        _targetTilemap = GetComponent<Tilemap>();
        // Create a new instance of CropTileContainer
        CropTileContainer = new CropTileContainer();

        // Configure the serialization settings for Vector3
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
            Converters = { new Vector3Converter() },
        };
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Subscribes to various events and initializes necessary managers.
    /// </summary>
    private void Start() {
        // Subscribe to events triggered when the next day starts, the next season starts, and the rain intensity changes
        TimeAndWeatherManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;
        TimeAndWeatherManager.Instance.OnNextSeasonStarted += TimeAndWeatherManager_OnNextSeasonStarted;
        TimeAndWeatherManager.Instance.OnChangeRainIntensity += TimeAndWeatherManager_OnChangeRainIntensity;

        _cropDatabase.AssignCropIds();

        // Get references to the TilemapManager and PlaceableObjectsManager instances
        _tilemapReadManager = TilemapManager.Instance;
        _placeableObjectsManager = PlaceableObjectsManager.Instance;
    }

    /// <summary>
    /// Called when the object is spawned on the network.
    /// </summary>
    public override void OnNetworkSpawn() {
        // Check if the current instance is running on the server
        if (IsServer) {
            // Subscribe to the event triggered when a client is connected to the network
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnected;
        }
    }


    #region Client Late Join
    /// <summary>
    /// Event handler for when a client is connected to the network.
    /// Sends batches of crop JSON data to the late-joining client.
    /// </summary>
    /// <param name="clientId">The ID of the connected client.</param>
    private void NetworkManager_OnClientConnected(ulong clientId) {
        var cropJsonsForTransfer = new List<string>(TRANSFER_BATCH_SIZE);

        // Serialize each crop tile in the crop tile container and add the JSON representation to the list
        foreach (var cropJson in CropTileContainer.SerializeCropTileContainer(CropTileContainer.CropTiles)) {
            cropJsonsForTransfer.Add(cropJson);

            // If the batch size is reached, send the batch of crop JSONs to the late-joining client and clear the list
            if (cropJsonsForTransfer.Count == TRANSFER_BATCH_SIZE) {
                SendCropJsonBatchToLateJoinClient(clientId, cropJsonsForTransfer);
                cropJsonsForTransfer.Clear();
            }
        }

        // Send any remaining crop JSONs to the client
        if (cropJsonsForTransfer.Count > 0) {
            SendCropJsonBatchToLateJoinClient(clientId, cropJsonsForTransfer);
        }
    }

    /// <summary>
    /// Sends a batch of crop JSON data to a late-joining client.
    /// </summary>
    /// <param name="clientId">The ID of the client to send the data to.</param>
    /// <param name="cropJsons">The list of crop JSON strings to send.</param>
    private void SendCropJsonBatchToLateJoinClient(ulong clientId, List<string> cropJsons) {
        // Serialize the list of crop data into a JSON string
        var json = JsonConvert.SerializeObject(cropJsons);
        // Call the ClientRpc method with the client ID and the serialized data
        NetworkManager_OnClientConnectedClientRpc(clientId, json);
    }

    /// <summary>
    /// This method is a ClientRpc that is called when a client connects to the server.
    /// It updates the crops container on the client side with the provided JSON data.
    /// </summary>
    /// <param name="clientId">The ID of the connected client.</param>
    /// <param name="cropsContainerJSON">The JSON data representing the crops container.</param>
    [ClientRpc]
    private void NetworkManager_OnClientConnectedClientRpc(ulong clientId, string cropsContainerJSON) {
        // If the client is the one that just connected and it is not the server
        if (clientId == NetworkManager.Singleton.LocalClientId && !IsServer) {
            // Update the client's crop container with the provided JSON data
            UpdateCropsClientRpc(cropsContainerJSON);
        }
    }
    #endregion


    #region Next Day and Season
    /// <summary>
    /// This method is called when the next day starts in the TimeAndWeatherManager.
    /// It performs various tasks related to crop management, such as deleting unseeded tiles,
    /// checking if crops are watered and applying damage, and transferring crop tile data to clients.
    /// </summary>
    private void TimeAndWeatherManager_OnNextDayStarted() {
        // If the current instance is not the server
        if (!IsServer) {
            // Log a message and exit the method
            Debug.Log("Not server; function needed");
            return;
        }

        // Delete some of the tiles that have not been seeded
        DeleteSomeUnseededTiles();

        // Check if the crops have been watered and apply damage if not
        CheckIfWateredAndApplyDamage();

        // Transfer the crop tile data from the server to the clients
        TransferServerCropTileContainerToClients();
    }

    /// <summary>
    /// Event handler for when the next season starts in the TimeAndWeatherManager.
    /// </summary>
    /// <param name="nextSeasonIndex">The index of the next season.</param>
    private void TimeAndWeatherManager_OnNextSeasonStarted(int nextSeasonIndex) {
        // Get the seasons in which the crops need to survive
        var seasonsToSurvive = GetSeasonsToSurvive();

        // Iterate over each crop tile
        foreach (CropTile cropTile in CropTileContainer.CropTiles) {
            // If the crop tile has a crop and the crop cannot survive in the next season
            if (cropTile.CropId >= 0 && !seasonsToSurvive.Contains((TimeAndWeatherManager.SeasonName)nextSeasonIndex)) {
                // Set the damage of the crop to the maximum
                cropTile.Damage = _maxCropDamage;
            }
        }
    }

    /// <summary>
    /// Retrieves the seasons in which the crops need to survive.
    /// </summary>
    /// <returns>A HashSet containing the names of the seasons.</returns>
    private HashSet<TimeAndWeatherManager.SeasonName> GetSeasonsToSurvive() {
        // Initialize a new hash set to store the seasons
        var seasonsToSurvive = new HashSet<TimeAndWeatherManager.SeasonName>();

        // Iterate over each crop in the crop tiles
        foreach (int crop in CropTileContainer.CropTiles.Select(tile => tile.CropId).Where(crop => crop >= 0)) {
            // Add the seasons in which the crop can grow to the hash set
            seasonsToSurvive.UnionWith(_cropDatabase.GetCropSOFromCropId(crop).SeasonsToGrow);
        }

        // Return the hash set of seasons
        return seasonsToSurvive;
    }
    #endregion


    #region Update CropTiles
    /// <summary>
    /// Transfers the server's crop tile container to the clients.
    /// </summary>
    private void TransferServerCropTileContainerToClients() {
        // If the current instance is not the server
        if (!IsServer) {
            // Clear the crop tile container on the client
            ClearCropTilesContainerClientRpc();
        }

        // Update the crop tiles on the server
        UpdateCropTilesOnServer();
    }

    /// <summary>
    /// Clears the crop tiles container and destroys all child gameObjects.
    /// This method is called on the client via RPC.
    /// </summary>
    [ClientRpc]
    private void ClearCropTilesContainerClientRpc() {
        // Clear the crop tiles container
        CropTileContainer.ClearCropTileContainer();

        // Iterate over each child of this transform
        foreach (Transform child in transform) {
            // Destroy the child gameObject
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Updates the crop tiles on the server.
    /// </summary>
    private void UpdateCropTilesOnServer() {
        // Iterate over each crop tile in the container
        foreach (var cropTile in CropTileContainer.CropTiles) {
            // If the crop tile has a crop
            if (cropTile.CropId >= 0) {
                // If the crop is dead
                if (cropTile.IsDead(cropTile, _maxCropDamage)) {
                    // Update the dead crop tile
                    UpdateDeadCropTile(cropTile);
                    Debug.Log($"CropTile: {cropTile.CropId} | IsDead?: {cropTile.IsDead(cropTile, _maxCropDamage)} | Damage: {cropTile.Damage}");
                } else {
                    // Otherwise, update the alive crop tile
                    UpdateAliveCropTile(cropTile);
                }
                
                // Visualize the changes to the crop
                VisualizeCropChanges(cropTile);
            }
        }

        // Initialize a list to hold the serialized crop data
        var cropJsonsForTransfer = new List<string>(TRANSFER_BATCH_SIZE);

        // Iterate over each serialized crop tile in the container
        foreach (var cropJson in CropTileContainer.SerializeCropTileContainer(CropTileContainer.CropTiles)) {
            // Add the serialized crop tile to the list
            cropJsonsForTransfer.Add(cropJson);

            // If the list has reached the maximum batch size
            if (cropJsonsForTransfer.Count == TRANSFER_BATCH_SIZE) {
                // Send the batch of serialized crop data to the client
                SendCropJsonBatchToClient(cropJsonsForTransfer);
                // Clear the list for the next batch
                cropJsonsForTransfer.Clear();
            }
        }

        // If there are any remaining serialized crop data in the list
        if (cropJsonsForTransfer.Count > 0) {
            // Send the remaining data to the client
            SendCropJsonBatchToClient(cropJsonsForTransfer);
        }
    }

    /// <summary>
    /// Sends a batch of crop JSON strings to the client.
    /// </summary>
    /// <param name="cropJsons">The list of crop JSON strings to send.</param>
    private void SendCropJsonBatchToClient(List<string> cropJsons) {
        // Serialize the list of crop JSON strings into a single JSON string
        var json = JsonConvert.SerializeObject(cropJsons);
        // Send the serialized JSON string to the client
        UpdateCropsClientRpc(json);
    }

    /// <summary>
    /// Updates the dead crop tile by resetting it if the crop stage is 0.
    /// </summary>
    /// <param name="cropTile">The crop tile to update.</param>
    private void UpdateDeadCropTile(CropTile cropTile) {
        // If the crop stage of the crop tile is 0
        if (cropTile.GetCropStage(_cropDatabase.GetCropSOFromCropId(cropTile.CropId)) == 0) {
            // Reset the crop tile
            cropTile.ResetCropTile();
        }
    }

    /// <summary>
    /// Updates the growth timer of an alive crop tile.
    /// </summary>
    /// <param name="cropTile">The crop tile to update.</param>
    private void UpdateAliveCropTile(CropTile cropTile) {
        // If the crop in the crop tile is not done growing
        if (!cropTile.IsCropDoneGrowing(_cropDatabase.GetCropSOFromCropId(cropTile.CropId))) {
            // Increment the current grow timer of the crop tile
            cropTile.CurrentGrowthTimer++;
        }
    }

    /// <summary>
    /// Updates the crops on the client side using the provided crops container JSON.
    /// </summary>
    /// <param name="cropsContainerJSON">The JSON string representing the crops container.</param>
    [ClientRpc]
    private void UpdateCropsClientRpc(string cropsContainerJSON) {
        // If the current instance is the server
        if (IsServer) {
            // The server has already updated the crops, so return
            return;
        }

        // Receive and unbox the crops container JSON on the client
        ReciveAndUnboxCropsContainerJSON(cropsContainerJSON);
    }

    /// <summary>
    /// Receives and unboxes a JSON string representing a crops container, and processes the crop tiles within it.
    /// </summary>
    /// <param name="cropsContainerJSON">The JSON string representing the crops container.</param>
    private void ReciveAndUnboxCropsContainerJSON(string cropsContainerJSON) {
        // Deserialize the crops container JSON into a list of crop tiles
        foreach (var cropTile in CropTileContainer.DeserializeCropTileContainer(cropsContainerJSON)) {
            // Try to add the crop tile to the container
            if (CropTileContainer.TryAddCropTileToContainer(cropTile)) {
                // If the crop tile has a crop
                if (cropTile.CropId >= 0) {
                    // Create a crop prefab for the crop tile
                    CreateCropPrefab(cropTile);
                    // Visualize the changes to the crop
                    VisualizeCropChanges(cropTile);
                }

                // Visualize the changes to the tile
                VisualizeTileChanges(cropTile);
            }
        }
    }
    #endregion


    #region Visualize crop tiles on map
    /// <summary>
    /// Visualizes the changes in a crop tile by setting the appropriate tile on the target tilemap.
    /// </summary>
    /// <param name="cropTile">The crop tile to visualize.</param>
    private void VisualizeTileChanges(CropTile cropTile) {
        // Choose the tile based on whether the crop tile is watered or not
        TileBase tile = cropTile.IsWatered ? _dirtPlowedWet : _dirtPlowedDry;
        // Set the tile at the crop position on the target tilemap
        _targetTilemap.SetTile(cropTile.CropPosition, tile);
    }

    /// <summary>
    /// Visualizes the changes in a crop tile's appearance based on its current state.
    /// </summary>
    /// <param name="cropTile">The crop tile to visualize.</param>
    private void VisualizeCropChanges(CropTile cropTile) {
        if (cropTile == null) {
            Debug.LogError("CropTile ist null.");
            return;
        }
        if (cropTile.CropId < 0) {
            Debug.LogError($"At {cropTile.CropPosition}, CropId is -1");
            return;
        }

        CropSO cropSO = _cropDatabase.GetCropSOFromCropId(cropTile.CropId);
        // If the crop is dead and its stage is not 0
        if (cropTile.IsDead(cropTile, _maxCropDamage) && cropTile.GetCropStage(cropSO) != 0) {
            // Set the sprite of the crop tile to the corresponding dead sprite
            cropTile.Prefab.GetComponent<SpriteRenderer>().sprite = cropSO.DeadSpritesGrowthStages[cropTile.GetCropStage(cropSO) - 1];
        } else {
            // Otherwise, set the sprite of the crop tile to the corresponding growth stage sprite
            cropTile.Prefab.GetComponent<SpriteRenderer>().sprite = cropSO.SpritesGrowthStages[cropTile.GetCropStage(cropSO)];
        }
    }

    /// <summary>
    /// Creates a crop prefab for the given crop tile.
    /// </summary>
    /// <param name="cropTile">The crop tile to create the prefab for.</param>
    private void CreateCropPrefab(CropTile cropTile) {
        // Instantiate a new crop sprite prefab
        HarvestCrop prefab = Instantiate(_cropsSpritePrefab, transform);
        // Set the prefab of the crop tile
        cropTile.Prefab = prefab;

        // Calculate the world position of the crop tile on the grid
        Vector3 worldPosition = TilemapManager.Instance.FixPositionOnGrid(_targetTilemap.CellToWorld(cropTile.CropPosition));
        // Set the position of the prefab in the world
        cropTile.Prefab.transform.position = worldPosition + new Vector3(0, 0.5f) + new Vector3(cropTile.SpriteRendererOffset.x, cropTile.SpriteRendererOffset.y, -0.1f);
        // Set the scale of the prefab
        cropTile.Prefab.transform.localScale = new Vector3(cropTile.SpriteRendererXScale, 1, 1);
        // Set the position of the crop on the prefab
        cropTile.Prefab.SetCropPosition(cropTile.CropPosition);
    }
    #endregion


    #region Plow Crop Tile
    /// <summary>
    /// Plows the specified tiles and consumes the given amount of energy.
    /// </summary>
    /// <param name="wantToPlowTilePositions">The positions of the tiles to plow.</param>
    /// <param name="usedEnergy">The amount of energy consumed.</param>
    public void PlowTiles(List<Vector3Int> wantToPlowTilePositions, int usedEnergy) {
        // Convert the list of positions to a JSON string
        var wantToPlowTilePositionsJSON = JsonConvert.SerializeObject(wantToPlowTilePositions);
        // Call the server RPC method with the JSON string and the used energy
        PlowTilesServerRpc(wantToPlowTilePositionsJSON, usedEnergy);
    }

    /// <summary>
    /// Server RPC method for plowing tiles.
    /// </summary>
    /// <param name="wantToPlowTilePositionsJSON">JSON string representing the positions of tiles to be plowed.</param>
    /// <param name="usedEnergy">The amount of energy used for plowing.</param>
    /// <param name="serverRpcParams">Optional parameters for the server RPC.</param>
    [ServerRpc(RequireOwnership = false)]
    private void PlowTilesServerRpc(string wantToPlowTilePositionsJSON, int usedEnergy, ServerRpcParams serverRpcParams = default) {
        // Convert the JSON string back to a list of positions
        var wantToPlowTilePositions = JsonConvert.DeserializeObject<List<Vector3Int>>(wantToPlowTilePositionsJSON);
        var canPlowTilePositions = new List<Vector3Int>();

        // Check each position if it can be plowed
        foreach (var position in wantToPlowTilePositions) {
            if (CanPlowTile(position)) {
                // If it can be plowed, add it to the list
                canPlowTilePositions.Add(position);
            }
        }

        // Handle the client callback, indicating if any tiles can be plowed
        HandleClientCallback(serverRpcParams, canPlowTilePositions.Count > 0);

        // Calculate the total used energy
        var totalUsedEnergy = usedEnergy * canPlowTilePositions.Count;
        // Handle the energy reduction for the player
        HandleEnergyReduction(serverRpcParams, totalUsedEnergy);

        // Convert the list of positions that can be plowed to a JSON string
        var canPlowTilePositionsJSON = JsonConvert.SerializeObject(canPlowTilePositions);
        // Call the client RPC method with the JSON string
        PlowTileClientRpc(canPlowTilePositionsJSON);
    }

    /// <summary>
    /// Checks if a tile can be plowed at the specified position.
    /// </summary>
    /// <param name="position">The position of the tile to check.</param>
    /// <returns>True if the tile can be plowed, false otherwise.</returns>
    private bool CanPlowTile(Vector3Int position) {
        // Check if the position is not already plowed, if the tile at the position can be plowed, and if there is no object placed at the position
        return !CropTileContainer.IsPositionPlowed(position) &&
               Array.IndexOf(_tilesThatCanBePlowed, _tilemapReadManager.ReturnTileBaseAtGridPosition(position)) != -1 &&
               !_placeableObjectsManager.IsPositionPlaced(position);
    }

    /// <summary>
    /// Handles the reduction of energy for a player.
    /// </summary>
    /// <param name="serverRpcParams">The server RPC parameters.</param>
    /// <param name="totalUsedEnergy">The total amount of energy used.</param>
    private void HandleEnergyReduction(ServerRpcParams serverRpcParams, int totalUsedEnergy) {
        // Get the client ID from the server RPC parameters
        var clientId = serverRpcParams.Receive.SenderClientId;
        // Check if the client is connected
        if (NetworkManager.ConnectedClients.ContainsKey(clientId)) {
            // Get the client
            var client = NetworkManager.ConnectedClients[clientId];
            // Reduce the energy of the player
            client.PlayerObject.GetComponent<PlayerHealthAndEnergyController>().RemoveEnergy(totalUsedEnergy);
        }
    }

    /// <summary>
    /// Sends a client RPC to plow the specified tile positions and creates crop tiles at those positions.
    /// </summary>
    /// <param name="canPlowTilePositionsJSON">The JSON string representing the positions of the tiles that can be plowed.</param>
    [ClientRpc]
    private void PlowTileClientRpc(string canPlowTilePositionsJSON) {
        // Deserialize the JSON string back to a list of positions
        var canPlowTilePositions = JsonConvert.DeserializeObject<List<Vector3Int>>(canPlowTilePositionsJSON);
        // For each position that can be plowed
        foreach (var position in canPlowTilePositions) {
            // Create a crop tile at the position
            var crop = CreateCropTile(position);
            // Visualize the changes on the tile
            VisualizeTileChanges(crop);
        }
    }

    private CropTile CreateCropTile(Vector3Int position) {
        // Create a new crop tile at the given position
        var crop = new CropTile { };

        crop.CropPosition = position;
        // Get the rule tile at the given position
        RuleTile ruleTile = _targetTilemap.GetTile<RuleTile>(position);
        // If the rule tile is a wet dirt tile, set the crop tile as watered
        if (ruleTile == _dirtWet) {
            crop.IsWatered = true;
        }

        // Try to add the crop tile to the container
        CropTileContainer.TryAddCropTileToContainer(crop);
        // Return the created crop tile
        return crop;
    }
    #endregion


    #region Seed Crop Tile
    [ServerRpc(RequireOwnership = false)]
    public void SeedTileServerRpc(Vector3Int wantToSeedTilePosition, int itemId, ServerRpcParams serverRpcParams = default) {
        // Check if the position is not plowed or is already seeded
        if (!CropTileContainer.IsPositionPlowed(wantToSeedTilePosition) ||
            CropTileContainer.IsPositionSeeded(wantToSeedTilePosition) ||
            !ItemManager.Instance.ItemDatabase.Items[itemId].CropToGrow.SeasonsToGrow.Contains((TimeAndWeatherManager.SeasonName)TimeAndWeatherManager.Instance.CurrentSeason)) {
            // If it is, handle the client callback and return
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        // Handle the reduction of the item from the sender's inventory
        HandleItemReduction(serverRpcParams, itemId);

        // Generate a random position for the sprite renderer
        var spriteRendererPosition = GenerateRandomSpriteRendererPosition();

        // Generate a random scale for the sprite renderer
        var spriteRendererScaleX = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;

        // Handle the client callback
        HandleClientCallback(serverRpcParams, true);

        // Call the client RPC method to seed the tile
        SeedTileClientRpc(wantToSeedTilePosition, spriteRendererPosition, spriteRendererScaleX, itemId);
    }

    /// <summary>
    /// Handles the reduction of an item from the sender's inventory.
    /// </summary>
    /// <param name="serverRpcParams">The server RPC parameters.</param>
    /// <param name="itemId">The ID of the item to be reduced.</param>
    private void HandleItemReduction(ServerRpcParams serverRpcParams, int itemId) {
        // Get the client ID from the server RPC parameters
        var clientId = serverRpcParams.Receive.SenderClientId;
        // Check if the client is connected
        if (NetworkManager.ConnectedClients.ContainsKey(clientId)) {
            // If it is, remove the item from the sender's inventory
            var client = NetworkManager.ConnectedClients[clientId];
            client.PlayerObject.GetComponent<PlayerInventoryController>().InventoryContainer.RemoveAnItemFromTheItemContainer(itemId, 1, 0);
        }
    }

    /// <summary>
    /// Generates a random position within a specified range for the SpriteRenderer.
    /// </summary>
    /// <returns>A Vector3 representing the generated position.</returns>
    private Vector3 GenerateRandomSpriteRendererPosition() {
        // Generate and return a new Vector3 with random x and y coordinates within the specified range
        return new Vector3(
            UnityEngine.Random.Range(-_cropPositionSpread, _cropPositionSpread),
            UnityEngine.Random.Range(-_cropPositionSpread, _cropPositionSpread)
        );
    }

    /// <summary>
    /// Sends a client RPC to seed a crop tile at the specified position with the given parameters.
    /// </summary>
    /// <param name="position">The position of the crop tile.</param>
    /// <param name="spriteRendererPosition">The position of the crop sprite renderer.</param>
    /// <param name="spriteRendererScaleX">The X scale of the crop sprite renderer.</param>
    /// <param name="itemId">The ID of the crop item.</param>
    [ClientRpc]
    private void SeedTileClientRpc(Vector3Int canSeedTilePosition, Vector3 spriteRendererPosition, int spriteRendererScaleX, int itemId) {
        // Get the crop tile at the specified position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(canSeedTilePosition);

        // Create the crop prefab
        CreateCropPrefab(cropTile);

        // Initialize the sprite renderer of the crop tile with the given parameters
        InitializeCropTileSpriteRenderer(cropTile, spriteRendererPosition, spriteRendererScaleX, itemId);

        // Visualize the changes made to the crop tile
        VisualizeCropChanges(cropTile);
    }

    /// <summary>
    /// Initializes the sprite renderer properties of a crop tile.
    /// </summary>
    /// <param name="cropTile">The crop tile to initialize.</param>
    /// <param name="spriteRendererPosition">The position of the sprite renderer.</param>
    /// <param name="spriteRendererScaleX">The scale of the sprite renderer on the X-axis.</param>
    /// <param name="itemId">The ID of the item associated with the crop tile.</param>
    private void InitializeCropTileSpriteRenderer(CropTile cropTile, Vector3 spriteRendererPosition, int spriteRendererScaleX, int itemId) {
        // Set the position of the sprite renderer
        cropTile.SpriteRendererOffset = spriteRendererPosition;
        cropTile.Prefab.transform.position += (Vector3)cropTile.SpriteRendererOffset;

        // Set the properties of the crop tile based on the given item ID
        SetCropTileProperties(cropTile, itemId);

        // Set the position of the crop for harvesting
        cropTile.Prefab.SetCropPosition(cropTile.CropPosition);

        // Set the scale of the sprite renderer
        cropTile.SpriteRendererXScale = spriteRendererScaleX;
        cropTile.Prefab.transform.localScale = new Vector3(cropTile.SpriteRendererXScale, 1, 1);
    }

    /// <summary>
    /// Sets the properties of a crop tile based on the specified item ID.
    /// </summary>
    /// <param name="cropTile">The crop tile to set the properties for.</param>
    /// <param name="itemId">The ID of the item associated with the crop tile.</param>
    private void SetCropTileProperties(CropTile cropTile, int itemId) {
        // Get the crop ID from the item database using the given item ID
        int cropId = ItemManager.Instance.ItemDatabase.Items[itemId].CropToGrow.CropID;

        Debug.Log($"SetCropTileProperties | CropId: {cropId}");
        cropTile.CropId = cropId;

        // Get the crop from the crop database using the crop ID
        CropSO crop = _cropDatabase.GetCropSOFromCropId(cropId);
        // Set the sprite of the crop tile based on its growth stage
        cropTile.Prefab.GetComponent<SpriteRenderer>().sprite = crop.SpritesGrowthStages[cropTile.GetCropStage(crop)];
    }
    #endregion


    #region Harvest Crop
    /// <summary>
    /// Server RPC method for harvesting a crop at the specified position.
    /// </summary>
    /// <param name="position">The position of the crop to harvest.</param>
    /// <param name="serverRpcParams">Optional parameters for the server RPC.</param>
    [ServerRpc(RequireOwnership = false)]
    public void HarvestCropServerRpc(Vector3Int position, ServerRpcParams serverRpcParams = default) {
        // Check if the crop at the given position can be harvested
        if (!CanHarvestCrop(position, out CropTile cropTile)) {
            // If not, handle the client callback and return
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        // Calculate the number of items to spawn when the crop is harvested
        int itemCountToSpawn = CalculateItemCount(cropTile);
        // Calculate the rarity of the items to be spawned
        int itemRarity = CalculateItemRarity();

        // Handle the client callback
        HandleClientCallback(serverRpcParams, true);

        // Call the client RPC to harvest the crop
        HarvestCropClientRpc(position, itemCountToSpawn, itemRarity);
    }

    /// <summary>
    /// Checks if a crop can be harvested at the specified grid position.
    /// </summary>
    /// <param name="gridPosition">The grid position to check.</param>
    /// <param name="cropTile">The crop tile at the specified grid position.</param>
    /// <returns>True if the crop can be harvested, false otherwise.</returns>
    private bool CanHarvestCrop(Vector3Int gridPosition, out CropTile cropTile) {
        // Get the crop tile at the given grid position
        cropTile = CropTileContainer.GetCropTileAtPosition(gridPosition);

        // Check if the position is seeded and the crop is ready to harvest
        if (!CropTileContainer.IsPositionSeeded(gridPosition) || !CropIsReadyToHarvest(cropTile)) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a crop is ready to be harvested.
    /// </summary>
    /// <param name="cropTile">The crop tile to check.</param>
    /// <returns>True if the crop is ready to be harvested, false otherwise.</returns>
    private bool CropIsReadyToHarvest(CropTile cropTile) {
        // Get the crop from the crop database using the crop ID
        CropSO crop = _cropDatabase.GetCropSOFromCropId(cropTile.CropId);
        // Check if the crop is done growing and is not dead
        return cropTile.IsCropDoneGrowing(crop) && !cropTile.IsDead(cropTile, _maxCropDamage);
    }

    /// <summary>
    /// Calculates the item count for a given crop tile.
    /// </summary>
    /// <param name="cropTile">The crop tile to calculate the item count for.</param>
    /// <returns>The calculated item count.</returns>
    private int CalculateItemCount(CropTile cropTile) {
        // Get the crop from the crop database using the crop ID
        CropSO crop = _cropDatabase.GetCropSOFromCropId(cropTile.CropId);
        // Calculate the item count by generating a random number between the minimum and maximum item amount to spawn
        return UnityEngine.Random.Range(crop.MinItemAmountToSpawn, crop.MaxItemAmountToSpawn);
    }

    /// <summary>
    /// Calculates the rarity of an item to be spawned based on the probability to spawn rarity.
    /// </summary>
    /// <returns>The rarity of the item to be spawned. Returns -1 if no rarity is determined.</returns>
    private int CalculateItemRarity() {
        // Generate a random number between 0 and 100
        int itemToSpawn = UnityEngine.Random.Range(0, 100);
        Debug.Log(_probabilityToSpawnRarity[2]);
        // Determine the rarity of the item to spawn based on the generated number and the probability to spawn rarity
        if (itemToSpawn > 100 - _probabilityToSpawnRarity[2]) {
            return 0;
        } else if (itemToSpawn > 100 - _probabilityToSpawnRarity[1]) {
            return 1;
        } else if (itemToSpawn > 100 - _probabilityToSpawnRarity[0]) {
            return 2;
        } else {
            return 3;
        }
    }

    /// <summary>
    /// Client RPC method for harvesting a crop at a given position.
    /// </summary>
    /// <param name="position">The position of the crop to harvest.</param>
    /// <param name="itemCountToSpawn">The number of items to spawn after harvesting.</param>
    /// <param name="itemRarity">The rarity of the spawned items.</param>
    [ClientRpc]
    private void HarvestCropClientRpc(Vector3Int position, int itemCountToSpawn, int itemRarity) {
        // Get the crop tile at the given position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);
        // Get the crop from the crop database using the crop ID
        CropSO crop = _cropDatabase.GetCropSOFromCropId(cropTile.CropId);

        // Spawn items at the position of the harvested crop
        ItemSpawnManager.Instance.SpawnItemAtPosition(
            _targetTilemap.CellToWorld(position),
            Vector2.zero,
            crop.ItemToGrowAndSpawn,
            itemCountToSpawn,
            itemRarity,
            SpreadType.Circle);

        // Handle the crop after it has been harvested
        HandleCropAfterHarvest(cropTile, crop);
    }

    /// <summary>
    /// Handles the actions to be taken after harvesting a crop.
    /// </summary>
    /// <param name="cropTile">The crop tile that was harvested.</param>
    /// <param name="crop">The crop that was harvested.</param>
    private void HandleCropAfterHarvest(CropTile cropTile, CropSO crop) {
        // If the crop can regrow, set it to regrow and adjust the grow timer
        if (crop.CanRegrow) {
            cropTile.IsRegrowing = true;
            cropTile.CurrentGrowthTimer -= crop.DaysToRegrow;
            VisualizeCropChanges(cropTile);
        }
        // If the crop does not regrow, destroy the crop tile
        else {
            DestroyCropTilePlantClientRpc(cropTile.CropPosition);
        }
    }
    #endregion


    #region Water Crop Tile

    #region Watering Can
    /// <summary>
    /// Waters the specified tiles and consumes energy.
    /// </summary>
    /// <param name="wantToWaterTilePositions">The positions of the tiles to be watered.</param>
    /// <param name="usedEnergy">The amount of energy consumed.</param>
    public void WaterTiles(List<Vector3Int> wantToWaterTilePositions, int usedEnergy) {
        // Convert the list of tile positions to a JSON string
        var wantToWaterTilePositionsJSON = JsonConvert.SerializeObject(wantToWaterTilePositions);
        // Call the server RPC to water the crop tiles
        WaterCropTileServerRpc(wantToWaterTilePositionsJSON, usedEnergy);
    }

    /// <summary>
    /// Server RPC method for watering crop tiles.
    /// </summary>
    /// <param name="wantToWaterTilePositionsJSON">JSON string representing the positions of the tiles to be watered.</param>
    /// <param name="usedEnergy">The amount of energy used for watering.</param>
    /// <param name="serverRpcParams">Optional parameters for the server RPC.</param>
    [ServerRpc(RequireOwnership = false)]
    private void WaterCropTileServerRpc(string wantToWaterTilePositionsJSON, int usedEnergy, ServerRpcParams serverRpcParams = default) {
        // Convert the JSON string back to a list of tile positions
        var wantToWaterTilePositions = JsonConvert.DeserializeObject<List<Vector3Int>>(wantToWaterTilePositionsJSON);
        // Initialize lists to hold the positions of tiles that can be watered and tiles that need to change rule
        var canWaterTilePositions = new List<Vector3Int>();
        var changeRuleTilePositions = new List<Vector3Int>();

        // Iterate over the positions of the tiles to be watered
        foreach (var position in wantToWaterTilePositions) {
            // Get the tile at the current position
            TileBase tileBase = _targetTilemap.GetTile<TileBase>(position);

            // Check if the tile can be watered
            if (CanWaterTile(position)) {
                // If it can, add the position to the list of tiles that can be watered
                canWaterTilePositions.Add(position);
            } else if (tileBase == _dirtDry) {
                // If the tile is dry dirt, add the position to the list of tiles that need to change rule
                changeRuleTilePositions.Add(position);
            }
        }

        // Calculate the total energy used by multiplying the energy used per tile by the number of tiles
        var totalUsedEnergy = usedEnergy * wantToWaterTilePositions.Count;
        // Handle the reduction of energy
        HandleEnergyReduction(serverRpcParams, totalUsedEnergy);

        // Handle the client callback
        HandleClientCallback(serverRpcParams, true);

        // Convert the lists of tile positions to JSON strings
        var canWaterTilePositionsJSON = JsonConvert.SerializeObject(canWaterTilePositions);
        var changeRuleTilePositionsJSON = JsonConvert.SerializeObject(changeRuleTilePositions);

        // Call the client RPC to water the crop tiles and change the rule tiles
        WaterCropTileClientRpc(canWaterTilePositionsJSON, changeRuleTilePositionsJSON);
    }

    /// <summary>
    /// Checks if a tile at the specified position can be watered.
    /// </summary>
    /// <param name="position">The position of the tile.</param>
    /// <returns>True if the tile can be watered, false otherwise.</returns>
    private bool CanWaterTile(Vector3Int position) {
        // Check if the tile at the given position is plowed
        return CropTileContainer.IsPositionPlowed(position);
    }

    /// <summary>
    /// Client RPC method that waters the crop tiles and changes the rule tiles accordingly.
    /// </summary>
    /// <param name="canWaterTilePositionsJSON">The JSON string representing the positions of the crop tiles that can be watered.</param>
    /// <param name="changeRuleTilePositionsJSON">The JSON string representing the positions of the rule tiles that need to be changed.</param>
    [ClientRpc]
    private void WaterCropTileClientRpc(string canWaterTilePositionsJSON, string changeRuleTilePositionsJSON) {
        // Convert the JSON strings back to lists of tile positions
        var canPlowTilePositions = JsonConvert.DeserializeObject<List<Vector3Int>>(canWaterTilePositionsJSON);
        var changeRuleTilePositions = JsonConvert.DeserializeObject<List<Vector3Int>>(changeRuleTilePositionsJSON);

        // Iterate over the positions of the tiles that can be watered
        foreach (var position in canPlowTilePositions) {
            // Get the crop tile at the current position
            CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);
            // Set the crop tile to be watered
            cropTile.IsWatered = true;
            // Visualize the changes to the tile
            VisualizeTileChanges(cropTile);
        }

        // Iterate over the positions of the tiles that need to change rule
        foreach (var position in changeRuleTilePositions) {
            // Set the tile at the current position to be wet dirt
            _targetTilemap.SetTile(position, _dirtWet);
        }
    }
    #endregion

    /// <summary>
    /// Event handler for the change in rain intensity in the time and weather manager.
    /// </summary>
    /// <param name="intensity">The new rain intensity value.</param>
    private void TimeAndWeatherManager_OnChangeRainIntensity(int intensity) {
        // If the intensity is zero, there is no rain, so dry all crop tiles.
        // Otherwise, there is rain, so water all crop tiles.
        if (intensity == 0) {
            DryAllCropTiles();
        } else {
            WaterAllCropTiles();
        }
    }

    /// <summary>
    /// Waters all crop tiles and updates the visual representation of the changes.
    /// </summary>
    private void WaterAllCropTiles() {
        // Iterate over all crop tiles and set their watered status to true.
        // Then visualize the changes.
        foreach (CropTile cropTile in CropTileContainer.CropTiles) {
            cropTile.IsWatered = true;
            VisualizeTileChanges(cropTile);
        }

        // Iterate over all positions within the target tilemap.
        // If a tile exists at a position and it is a dry dirt tile, change it to a wet dirt tile.
        foreach (Vector3Int gridPosition in _targetTilemap.cellBounds.allPositionsWithin) {
            if (_targetTilemap.HasTile(gridPosition)) {
                TileBase tileBase = _targetTilemap.GetTile(gridPosition);
                if (tileBase == _dirtDry) {
                    _targetTilemap.SetTile(gridPosition, _dirtWet);
                }
            }
        }
    }

    /// <summary>
    /// Dries all crop tiles and changes the visual representation of the tiles.
    /// </summary>
    private void DryAllCropTiles() {
        // Iterate over all crop tiles and set their watered status to false.
        // Then visualize the changes.
        foreach (CropTile cropTile in CropTileContainer.CropTiles) {
            cropTile.IsWatered = false;
            VisualizeTileChanges(cropTile);
        }

        // Iterate over all positions within the target tilemap.
        // If a tile exists at a position and it is a wet dirt tile, change it to a dry dirt tile.
        foreach (Vector3Int gridPosition in _targetTilemap.cellBounds.allPositionsWithin) {
            if (_targetTilemap.HasTile(gridPosition)) {
                TileBase tileBase = _targetTilemap.GetTile(gridPosition);
                if (tileBase == _dirtWet) {
                    _targetTilemap.SetTile(gridPosition, _dirtDry);
                }
            }
        }
    }

    /// <summary>
    /// Checks if each crop tile is watered and applies damage accordingly.
    /// </summary>
    private void CheckIfWateredAndApplyDamage() {
        // Iterate over all crop tiles.
        foreach (CropTile cropTile in CropTileContainer.CropTiles) {
            // If there's no crop on the tile, mark it as not watered and continue to the next tile.
            if (cropTile.CropId == -1) {
                cropTile.IsWatered = false;
                VisualizeTileChanges(cropTile);
                continue;
            }

            // If the crop is not watered, increase its damage.
            // If the crop is watered and its damage is greater than 0, decrease its damage.
            cropTile.Damage = !cropTile.IsWatered ? cropTile.Damage + _notWateredDamage : cropTile.Damage = 0;

            // Generate a random number and check if the crop will die based on its damage.
            if (UnityEngine.Random.Range(0, _maxCropDamage) < cropTile.Damage) {
                cropTile.Damage = _maxCropDamage;
            }

            cropTile.IsWatered = UnityEngine.Random.Range(0, 100) < cropTile.KeepWateredScaler * 100 ? true : false;

            // Visualize the changes to the tile.
            VisualizeTileChanges(cropTile);
        }
    }
    #endregion


    #region Destroy Crop
    /// <summary>
    /// Destroys a crop tile on the server and performs the corresponding action based on the tool type used.
    /// </summary>
    /// <param name="position">The position of the crop tile to destroy.</param>
    /// <param name="usedEnergy">The amount of energy used.</param>
    /// <param name="toolTypes">The type of tool used.</param>
    /// <param name="serverRpcParams">Additional parameters for the server RPC.</param>
    [ServerRpc(RequireOwnership = false)]
    public void DestroyCropTileServerRpc(Vector3Int position, int usedEnergy, ToolTypes toolTypes, ServerRpcParams serverRpcParams = default) {
        // Initialize success flag
        bool success = false;

        // Perform action based on the tool type
        switch (toolTypes) {
            case ToolTypes.Scythe:
                // If the position is not seeded, set success to false
                // Otherwise, harvest the crop and set success to true
                if (!CropTileContainer.IsPositionSeeded(position)) {
                    success = false;
                    break;
                } else {
                    success = true;
                    ScytheCrop(position);
                    break;
                }
            case ToolTypes.Pickaxe:
                // If the position is not plowed, set success to false
                // Otherwise, pick the crop and set success to true
                if (!CropTileContainer.IsPositionPlowed(position)) {
                    success = false;
                    break;
                } else {
                    success = true;
                    PickaxeCrop(position);
                    break;
                }
            default:
                // Log error if no valid tool type is provided
                Debug.LogError("No valid tool type");
                break;
        }

        // Handle client callback with the success flag
        HandleClientCallback(serverRpcParams, success);

        // Handle energy reduction with the used energy
        HandleEnergyReduction(serverRpcParams, usedEnergy);
    }

    /// <summary>
    /// Harvests a crop using a scythe at the specified position.
    /// </summary>
    /// <param name="position">The position of the crop to be harvested.</param>
    private void ScytheCrop(Vector3Int position) {
        // Retrieve the crop tile at the specified position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);

        // Calculate the item count and rarity
        int itemCount = CalculateItemCount(cropTile);
        int itemRarity = CalculateItemRarity();

        // Retrieve the crop from the database
        CropSO crop = _cropDatabase.GetCropSOFromCropId(cropTile.CropId);

        // Check if the crop is done growing and if it can be harvested by a scythe
        bool cropDoneGrowing = cropTile.IsCropDoneGrowing(crop);
        bool cropIsHarvestedByScytheAndDoneGrowing = crop.IsHarvestedByScythe && cropDoneGrowing;

        // If the crop can regrow, harvest it
        // If the crop is done growing, harvest and destroy it
        // Otherwise, destroy the plowed tile
        if (cropIsHarvestedByScytheAndDoneGrowing && crop.CanRegrow) {
            // Harvest regrowing crop
            HarvestCropClientRpc(position, itemCount, itemRarity);
        } else if (cropIsHarvestedByScytheAndDoneGrowing || cropDoneGrowing) {
            // Harvest and destroy crop
            HarvestCropClientRpc(position, itemCount, itemRarity);
            DestroyCropTilePlantClientRpc(position);
        } else {
            // Destroy plowed tile
            DestroyCropTilePlantClientRpc(position);
        }
    }

    /// <summary>
    /// Picks the crop at the specified position. If the position is seeded, the seed is destroyed. Otherwise, the plowed tile is destroyed.
    /// </summary>
    /// <param name="position">The position of the crop to pick.</param>
    private void PickaxeCrop(Vector3Int position) {
        // If the position is seeded, destroy the seed
        // Otherwise, destroy the plowed tile
        if (CropTileContainer.IsPositionSeeded(position)) {
            // Destroy seed
            DestroyCropTilePlantClientRpc(position);
        } else {
            // Destroy plowed tile
            DestroyCropTileClientRpc(position);
        }
    }

    /// <summary>
    /// Destroys the crop tile plant on the client side.
    /// </summary>
    /// <param name="position">The position of the crop tile.</param>
    [ClientRpc]
    public void DestroyCropTilePlantClientRpc(Vector3Int position) {
        // Retrieve the crop tile from the container
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);

        // Destroy the crop tile's prefab
        Destroy(cropTile.Prefab.gameObject);

        // Reset the crop tile
        cropTile.ResetCropTile();
    }

    /// <summary>
    /// Destroys the crop tile on the client side and updates the tilemap with the appropriate tile based on the watered state.
    /// </summary>
    /// <param name="position">The position of the crop tile to destroy.</param>
    [ClientRpc]
    private void DestroyCropTileClientRpc(Vector3Int position) {
        // Retrieve the crop tile from the container at the given position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);

        // Remove the retrieved crop tile from the container
        CropTileContainer.RemoveCropTileFromContainer(cropTile);

        // Determine the appropriate tile based on whether the crop tile is watered or not
        TileBase targetTile = cropTile.IsWatered ? _dirtWet : _dirtDry;

        // Set the determined tile at the specified grid position on the target tilemap
        _targetTilemap.SetTile(position, targetTile);
    }


    /// <summary>
    /// Deletes unseeded tiles from the crop system.
    /// </summary>
    private void DeleteSomeUnseededTiles() {
        // Create a list of tiles to remove, which are unseeded and should be removed based on probability
        var tilesToRemove = CropTileContainer.CropTiles
            .Where(cropTile => cropTile.CropId == -1 && ShouldRemoveTile())
            .ToList();

        // For each tile in the list, remove it from the container and set the tile at its position on the target tilemap to null
        foreach (var cropTile in tilesToRemove) {
            CropTileContainer.RemoveCropTileFromContainer(cropTile);
            _targetTilemap.SetTile(cropTile.CropPosition, null);
        }
    }

    /// <summary>
    /// Determines whether a tile should be removed based on probability.
    /// </summary>
    /// <returns>True if the tile should be removed, false otherwise.</returns>
    private bool ShouldRemoveTile() {
        // Generate a random number between 0 and 100, and return true if it's less than the probability to delete unseeded tile
        int probability = UnityEngine.Random.Range(0, 100);
        return probability < _probabilityToDeleteUnseededTile;
    }
    #endregion


    /// <summary>
    /// Handles the callback from the client after a server RPC call.
    /// Removes the seed from the sender's inventory if the client is connected.
    /// </summary>
    /// <param name="serverRpcParams">The parameters of the server RPC call.</param>
    /// <param name="success">Indicates whether the server RPC call was successful.</param>
    private void HandleClientCallback(ServerRpcParams serverRpcParams, bool success) {
        // Get the client ID from the server RPC parameters
        var clientId = serverRpcParams.Receive.SenderClientId;
        // If the client is connected, remove the seed from the sender's inventory
        if (NetworkManager.ConnectedClients.ContainsKey(clientId)) {
            var client = NetworkManager.ConnectedClients[clientId];
            client.PlayerObject.GetComponent<PlayerToolsAndWeaponController>().ClientCallback(success);
        }
    }


    // *TODO* Maybe delete after place object refactor
    /// <summary>
    /// Checks if a given position is seeded with a crop.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True if the position is seeded with a crop, false otherwise.</returns>
    public bool IsPositionSeeded(Vector3Int position) {
        // Check if the given position is seeded in the crop tile container
        return CropTileContainer.IsPositionSeeded(position);
    }


    #region Save and Load
    /// <summary>
    /// Saves the game data, including the crops on the map.
    /// </summary>
    /// <param name="data">The game data to be saved.</param>
    public void SaveData(GameData data) {
        // If the flag to save crops is set, serialize the crop data and store it in the game data
        if (_saveCrops) {
            data.CropsOnMap = JsonConvert.SerializeObject(CropTileContainer.SerializeCropTileContainer(CropTileContainer.CropTiles));
        }
    }


    /// <summary>
    /// Loads the game data into the crops manager.
    /// </summary>
    /// <param name="data">The game data to load.</param>
    public void LoadData(GameData data) {
        // If the game data contains crop data and the flag to load crops is set, deserialize the crop data and load it into the crop manager
        if (!string.IsNullOrEmpty(data.CropsOnMap) && _loadCrops) {
            ReciveAndUnboxCropsContainerJSON(data.CropsOnMap);
        }
    }
    #endregion

}