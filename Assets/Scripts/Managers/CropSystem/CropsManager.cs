using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static CropTile;

/// <summary>
/// Manages the crops in the game, including planting, growing, and harvesting.
/// Implements the IDataPersistance interface for data persistence.
/// </summary>
public class CropsManager : NetworkBehaviour, IDataPersistance {
    public static CropsManager Instance { get; private set; }

    [Header("Debug: Save and Load")]
    [SerializeField] private bool _saveCrops = true;
    [SerializeField] private bool _loadCrops = true;

    [Header("Params: Crop")]
    [SerializeField] private float _cropPositionSpread = 0.1f; //Spread of the crop sprite renderer position
    [SerializeField] private int _notWateredDamage = 25; //Damage applied to the crop if not watered
    [SerializeField] private int _maxCropDamage = 100;
    [SerializeField] private int _probabilityToDeleteUnseededTile = 25; //Chance in percent to delete unseeded tile
    [SerializeField] private int[] _probabilityToSpawnRarity = { 70, 20, 8, 2 }; //70% Copper, 20% Iron, 8% Gold, 2% Diamond

    [Header("Params: Multiplayer")]
    private const int TRANSFER_BATCH_SIZE = 80; // Batch size for transferring crop JSON data to clients

    [Header("Reference: TileBases")]
    [SerializeField] private RuleTile _dirtDry;
    [SerializeField] private RuleTile _dirtWet;
    [SerializeField] private RuleTile _dirtPlowedDry;
    [SerializeField] private RuleTile _dirtPlowedWet;
    [SerializeField] private TileBase[] _tilesThatCanBePlowed;

    [Header("Reference: Fertilizer")]
    [SerializeField] private List<FertilizerSO> _growthFertilizer;
    [SerializeField] private List<FertilizerSO> _regrowthFertilizer;
    [SerializeField] private List<FertilizerSO> _qualityFertilizer;
    [SerializeField] private List<FertilizerSO> _quantityFertilizer;
    [SerializeField] private List<FertilizerSO> _waterFertilizer;

    [Header("Reference: After Harvest")]
    [SerializeField] private Sprite _cropHole;

    [Header("Reference: Prefab")]
    [SerializeField] private GameObject _cropsSpritePrefab;
    [SerializeField] private GameObject _cropsTreeSpritePrefab;

    [Header("Reference: Database")]
    public CropDatabaseSO CropDatabase; // public for resource node (if the tree can be cut)
    public CropTileContainer CropTileContainer { get; private set; } // Public for cheat commands

    [Header("Reference: Tilemap")]
    private Tilemap _targetTilemap;

    [Header("Reference: Managers")]
    private TilemapManager _tilemapReadManager;
    private PlaceableObjectsManager _placeableObjectsManager;

    private const float PROBABILITY_OF_THUNDER_STRIKE = 0.01f;
    [SerializeField] private ItemSO _coal;

    // Future testing
    //private NetworkList<CropTileData> cropTiles = new NetworkList<CropTileData>();


    /// <summary>
    /// This method is called when the script instance is being loaded.
    /// It initializes the CropsManager singleton instance, sets up the required components,
    /// and configures the serialization settings for Vector3.
    /// </summary>
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of CropsManager in the scene!");
            return;
        }
        Instance = this;

        InitializeComponents();
    }

    private void InitializeComponents() {
        _targetTilemap = GetComponent<Tilemap>();
        CropTileContainer = new CropTileContainer();

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
            Converters = { new Vector3Converter() },
        };

        CropDatabase.InitializeCrops();
    }


    /// <summary>
    /// Called when the script instance is being loaded.
    /// Subscribes to various events and initializes necessary managers.
    /// </summary>
    private void Start() {
        // Subscribe to events triggered when the next day starts, the next season starts, and the rain intensity changes
        TimeManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;
        TimeManager.Instance.OnNextSeasonStarted += TimeAndWeatherManager_OnNextSeasonStarted;
        WeatherManager.Instance.OnChangeRainIntensity += TimeAndWeatherManager_OnChangeRainIntensity;
        WeatherManager.Instance.OnThunderStrike += OnThunderStrike;

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
        foreach (var cropJson in CropTileContainer.SerializeCropTileContainer()) {
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

        // Check for crops that are fertilized with water and apply the effect
        CheckForWaterFertilizedCrops();

        // Transfer the crop tile data from the server to the clients
        TransferServerCropTileContainerToClients();
    }

    /// <summary>
    /// Event handler for when the next season starts in the TimeAndWeatherManager.
    /// </summary>
    /// <param name="nextSeasonIndex">The index of the next season.</param>
    private void TimeAndWeatherManager_OnNextSeasonStarted(int nextSeasonIndex) {
        // Iterate over each crop tile
        foreach (CropTile cropTile in CropTileContainer.CropTileMap.Values) {
            // If the crop tile has a crop and the crop cannot survive in the next season
            if (cropTile.CropId >= 0 && !CropDatabase[cropTile.CropId].SeasonsToGrow.Contains((TimeManager.SeasonName)nextSeasonIndex)) {
                // Set the damage of the crop to the maximum
                cropTile.Damage = _maxCropDamage;
            }
        }
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
        foreach (var cropTile in CropTileContainer.CropTileMap.Values) {
            // If the crop tile has a crop
            if (cropTile.CropId >= 0) {
                // If the crop is dead
                if (cropTile.IsDead()) {
                    // Update the dead crop tile
                    UpdateDeadCropTile(cropTile);
                    Debug.Log($"CropTile: {cropTile.CropId} | IsDead?: {cropTile.IsDead()} | Damage: {cropTile.Damage}");
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
        foreach (var cropJson in CropTileContainer.SerializeCropTileContainer()) {
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
        if (cropTile.GetCropStage() == CropStage.Seeded) {
            // Reset the crop tile
            cropTile.ResetCropTile();
        }
    }

    /// <summary>
    /// Updates the growth timer of an alive crop tile.
    /// </summary>
    /// <param name="cropTile">The crop tile to update.</param>
    private void UpdateAliveCropTile(CropTile cropTile) {
        // Retrieve crop information from the database
        var cropInfo = CropDatabase[cropTile.CropId];

        // Check if the crop is a tree and currently in the Growing stage and not struck by a lightning
        if (cropInfo.IsTree &&
            cropTile.GetCropStage() == CropStage.Growing &&
            !cropTile.IsStruckByLightning) {
            // Get the current position of the tree
            int currentX = cropTile.CropPosition.x;
            int currentY = cropTile.CropPosition.y;

            bool canGrow = true;

            // Iterate through a 5x5 grid centered on the current tree
            for (int dx = -2; dx <= 2; dx++) {
                for (int dy = -2; dy <= 2; dy++) {
                    // Skip the current tree's position
                    if (dx == 0 && dy == 0) {
                        continue;
                    }

                    int adjacentX = currentX + dx;
                    int adjacentY = currentY + dy;

                    // Retrieve the adjacent crop tile
                    CropTile adjacentTile = CropTileContainer.GetCropTileAtPosition(new Vector3Int(adjacentX, adjacentY));

                    // Continue if the adjacent tile is empty or not a tree
                    if (adjacentTile == null || !CropDatabase[adjacentTile.CropId].IsTree) {
                        continue;
                    }

                    var adjacentCropInfo = CropDatabase[adjacentTile.CropId];

                    // Get the stage of the adjacent tree
                    CropStage adjacentStage = adjacentTile.GetCropStage();

                    // If the adjacent tree is at a higher stage, current tree cannot grow
                    if (adjacentStage > CropStage.Growing) {
                        canGrow = false;
                        break;
                    }
                }

                // Exit early if a higher tree is found
                if (!canGrow) {
                    break;
                }
            }

            // If the crop in the crop tile is not done growing
            if (!cropTile.IsCropDoneGrowing()) {
                // Increment the current grow timer of the crop tile.
                cropTile.CurrentGrowthTimer++;
            }
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
            if (CropTileContainer.AddCropTileToContainer(cropTile)) {
                // If the crop tile has a crop
                if (cropTile.CropId >= 0) {
                    // Create a crop prefab for the crop tile
                    CreateCropPrefab(cropTile, CropDatabase[cropTile.CropId].IsTree, 0); // TODO Die 0 muss die SeedId sein. Vielleicht die SeedId als referenz im CropTile speichern.
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

        CropSO cropSO = CropDatabase[cropTile.CropId];
        // If the crop is dead and its stage is not 0
        if (cropTile.IsDead() && cropTile.GetCropStage() != CropStage.Seeded) {
            // Set the sprite of the crop tile to the corresponding dead sprite
            cropTile.Prefab.GetComponent<SpriteRenderer>().sprite = cropSO.DeadSpritesGrowthStages[(int)cropTile.GetCropStage() - 1];
        } else {
            // Otherwise, set the sprite of the crop tile to the corresponding growth stage sprite
            cropTile.Prefab.GetComponent<SpriteRenderer>().sprite = cropSO.SpritesGrowthStages[(int)cropTile.GetCropStage()];
        }


        if (cropTile.GrowthTimeScaler > 1) {
            cropTile.Prefab.GetComponent<HarvestCrop>().SetFertilizerSprite(
                _growthFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == Math.Round((cropTile.GrowthTimeScaler - 1) * 100)).FertilizerType,
                _growthFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == Math.Round((cropTile.GrowthTimeScaler - 1) * 100)).FertilizerCropTileColor);
        }
        if (cropTile.RegrowthTimeScaler > 1) {
            cropTile.Prefab.GetComponent<HarvestCrop>().SetFertilizerSprite(
                _regrowthFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == Math.Round((cropTile.RegrowthTimeScaler - 1) * 100)).FertilizerType,
                _regrowthFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == Math.Round((cropTile.RegrowthTimeScaler - 1) * 100)).FertilizerCropTileColor);
        }
        if (cropTile.QualityScaler > 1) {
            cropTile.Prefab.GetComponent<HarvestCrop>().SetFertilizerSprite(
                _qualityFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == cropTile.QualityScaler).FertilizerType,
                _qualityFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == cropTile.QualityScaler).FertilizerCropTileColor);
        }
        if (cropTile.QuantityScaler > 1) {
            cropTile.Prefab.GetComponent<HarvestCrop>().SetFertilizerSprite(
                _quantityFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == Math.Round((cropTile.QuantityScaler - 1) * 100)).FertilizerType,
                _quantityFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == Math.Round((cropTile.QuantityScaler - 1) * 100)).FertilizerCropTileColor);
        }
        if (cropTile.WaterScaler > 0) {
            cropTile.Prefab.GetComponent<HarvestCrop>().SetFertilizerSprite(
                _waterFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == cropTile.WaterScaler).FertilizerType,
                _waterFertilizer.Find(fertilizer => fertilizer.FertilizerBonusValue == cropTile.WaterScaler).FertilizerCropTileColor);
        }
    }

    /// <summary>
    /// Creates a crop prefab for the given crop tile.
    /// </summary>
    /// <param name="cropTile">The crop tile to create the prefab for.</param>
    private void CreateCropPrefab(CropTile cropTile, bool isTree, int itemId) {
        // Create and set the prefab of the crop tile
        if (isTree) {
            cropTile.Prefab = Instantiate(_cropsTreeSpritePrefab, transform);
            cropTile.Prefab.GetComponent<ResourceNodeBase>().SetSeed(ItemManager.Instance.ItemDatabase[itemId] as SeedSO);
        } else {
            cropTile.Prefab = Instantiate(_cropsSpritePrefab, transform);
        }

        // Calculate the world position of the crop tile on the grid
        Vector3 worldPosition = TilemapManager.Instance.AlignPositionToGridCenter(_targetTilemap.CellToWorld(cropTile.CropPosition));
        // Set the position of the prefab in the world
        cropTile.Prefab.transform.position = worldPosition + new Vector3(0, 0.5f) + new Vector3(cropTile.SpriteRendererOffset.x, cropTile.SpriteRendererOffset.y, -0.1f);
        // Set the scale of the prefab
        cropTile.Prefab.transform.localScale = new Vector3(cropTile.SpriteRendererXScale, 1, 1);
        // Set the position of the crop on the prefab
        cropTile.Prefab.GetComponent<HarvestCrop>().SetCropPosition(cropTile.CropPosition);
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
               !_placeableObjectsManager.POContainer.PlaceableObjects.ContainsKey(position);
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
            client.PlayerObject.GetComponent<PlayerHealthAndEnergyController>().AdjustEnergy(-totalUsedEnergy);
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
        CropTileContainer.AddCropTileToContainer(crop);
        // Return the created crop tile
        return crop;
    }
    #endregion


    #region Fertilize Crop Tile

    [ServerRpc(RequireOwnership = false)]
    public void FertilizeTileServerRpc(Vector3Int wantToFertilizeTilePosition, int itemId, ServerRpcParams serverRpcParams = default) {
        // Check if the position is not plowed or is already fertilized
        if (!CropTileContainer.IsPositionPlowed(wantToFertilizeTilePosition) ||
            !CropTileContainer.IsPositionSeeded(wantToFertilizeTilePosition) ||
            !CropTileContainer.CanPositionBeFertilized(wantToFertilizeTilePosition, itemId)
            || CropDatabase[CropTileContainer.GetCropTileAtPosition(wantToFertilizeTilePosition).CropId].IsTree) {
            // If it is, handle the client callback and return
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        // Handle the reduction of the item from the sender's inventory
        HandleItemReduction(serverRpcParams, itemId);

        // Handle the client callback
        HandleClientCallback(serverRpcParams, true);

        // Call the client RPC method to fertilize the tile
        FertilizeTileClientRpc(wantToFertilizeTilePosition, itemId);
    }

    [ClientRpc]
    private void FertilizeTileClientRpc(Vector3Int canFertilizeTilePosition, int itemId) {
        // Get the crop tile at the specified position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(canFertilizeTilePosition);
        FertilizerSO fertilizerSO = ItemManager.Instance.ItemDatabase[itemId] as FertilizerSO;
        SetFertilizerValueAndSprite(cropTile, fertilizerSO);

        // Visualize the changes made to the crop tile
        VisualizeCropChanges(cropTile);
    }

    private void SetFertilizerValueAndSprite(CropTile cropTile, FertilizerSO fertilizerSO) {
        switch (fertilizerSO.FertilizerType) {
            case FertilizerSO.FertilizerTypes.GrowthTime:
                cropTile.GrowthTimeScaler = (fertilizerSO.FertilizerBonusValue / 100) + 1;
                break;
            case FertilizerSO.FertilizerTypes.RegrowthTime:
                cropTile.RegrowthTimeScaler = (fertilizerSO.FertilizerBonusValue / 100) + 1;
                break;
            case FertilizerSO.FertilizerTypes.Quality:
                cropTile.QualityScaler = fertilizerSO.FertilizerBonusValue;
                break;
            case FertilizerSO.FertilizerTypes.Quantity:
                cropTile.QuantityScaler = (fertilizerSO.FertilizerBonusValue / 100) + 1;
                break;
            case FertilizerSO.FertilizerTypes.Water:
                cropTile.WaterScaler = fertilizerSO.FertilizerBonusValue;
                break;
        }
    }
    #endregion


    #region Seed Crop Tile
    [ServerRpc(RequireOwnership = false)]
    public void SeedTileServerRpc(Vector3Int wantToSeedTilePosition, int itemId, ServerRpcParams serverRpcParams = default) {
        // Tree
        if ((ItemManager.Instance.ItemDatabase[itemId] as SeedSO).CropToGrow.IsTree
            && !CropTileContainer.IsPositionSeeded(wantToSeedTilePosition)
            && (ItemManager.Instance.ItemDatabase[itemId] as SeedSO).CropToGrow.SeasonsToGrow.Contains((TimeManager.SeasonName)TimeManager.Instance.CurrentDate.Value.Season)
            && Array.IndexOf(_tilesThatCanBePlowed, _tilemapReadManager.ReturnTileBaseAtGridPosition(wantToSeedTilePosition)) != -1
            && !_placeableObjectsManager.POContainer.PlaceableObjects.ContainsKey(wantToSeedTilePosition)) {
            // Plant the tree
        } else {
            // Check if the position is not plowed or is already seeded
            if (!CropTileContainer.IsPositionPlowed(wantToSeedTilePosition) ||
                CropTileContainer.IsPositionSeeded(wantToSeedTilePosition) ||
                !(ItemManager.Instance.ItemDatabase[itemId] as SeedSO).CropToGrow.SeasonsToGrow.Contains((TimeManager.SeasonName)TimeManager.Instance.CurrentDate.Value.Season)) {
                // If it is, handle the client callback and return
                HandleClientCallback(serverRpcParams, false);
                return;
            }
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
            client.PlayerObject.GetComponent<PlayerInventoryController>().InventoryContainer.RemoveItem(new ItemSlot(itemId, 1, 0));
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


        if (CropDatabase[(ItemManager.Instance.ItemDatabase[itemId] as SeedSO).CropToGrow.CropID].IsTree
            && !CropTileContainer.IsPositionPlowed(canSeedTilePosition)) {
            CreateCropTile(canSeedTilePosition);
        }

        // Create the crop prefab
        CreateCropPrefab(cropTile, (ItemManager.Instance.ItemDatabase[itemId] as SeedSO).CropToGrow.IsTree, itemId);

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
        cropTile.Prefab.GetComponent<HarvestCrop>().SetCropPosition(cropTile.CropPosition);

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
        int cropId = (ItemManager.Instance.ItemDatabase[itemId] as SeedSO).CropToGrow.CropID;

        cropTile.CropId = cropId;

        // Get the crop from the crop database using the crop ID
        CropSO crop = CropDatabase[cropTile.CropId];
        // Set the sprite of the crop tile based on its growth stage
        cropTile.Prefab.GetComponent<SpriteRenderer>().sprite = crop.SpritesGrowthStages[(int)cropTile.GetCropStage()];
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
        int itemRarity = CalculateItemRarity(cropTile.QualityScaler);

        // Handle the client callback
        HandleClientCallback(serverRpcParams, true);

        // Call the client RPC to harvest the crop
        HarvestCropClientRpc(position, itemCountToSpawn, itemRarity);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HarvestTreeServerRpc(Vector3Int position, ServerRpcParams serverRpcParams = default) {
        // Get the crop tile at the given grid position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);

        // Calculate the number of items to spawn when the crop is harvested
        int itemCountToSpawn = CalculateItemCount(cropTile);
        // Calculate the rarity of the items to be spawned
        int itemRarity = CalculateItemRarity(cropTile.QualityScaler);

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
        if (!CropTileContainer.IsPositionSeeded(gridPosition)
            || !cropTile.IsCropHarvestable()
            || CropDatabase[cropTile.CropId].IsTree) { // Is a tree so harvest is by shaking or axe
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the item count for a given crop tile.
    /// </summary>
    /// <param name="cropTile">The crop tile to calculate the item count for.</param>
    /// <returns>The calculated item count.</returns>
    public int CalculateItemCount(CropTile cropTile) {
        // Get the crop from the crop database using the crop ID
        CropSO crop = CropDatabase[cropTile.CropId];

        // Ensure that the product of the scaler and the item amount is treated as an integer
        int minItems = Mathf.RoundToInt(crop.MinItemAmountToSpawn * cropTile.QuantityScaler);
        int maxItems = Mathf.RoundToInt(crop.MaxItemAmountToSpawn * cropTile.QuantityScaler);

        // Calculate the item count by generating a random number between the adjusted minimum and maximum item amount to spawn
        return UnityEngine.Random.Range(minItems, maxItems);
    }


    /// <summary>
    /// Calculates the rarity of an item to be spawned based on the probability to spawn rarity.
    /// </summary>
    /// <returns>The rarity of the item to be spawned. Returns -1 if no rarity is determined.</returns>
    public int CalculateItemRarity(float rarityBonus) {
        // Generate a random number between 0 and 100
        int rarityToSpawn = UnityEngine.Random.Range(0, 100);

        // Determine the rarity of the item to spawn based on the probability to spawn rarity
        return rarityToSpawn switch {
            _ when rarityToSpawn <= _probabilityToSpawnRarity[3] + rarityBonus => 0, //e.g. rarity to spawn is 5, probability to spawn rarity is 2, bonus is 30; 5 <= 2 + 30; //5 <= 32; //=> 0
            _ when rarityToSpawn <= _probabilityToSpawnRarity[2] + rarityBonus => 1,
            _ when rarityToSpawn <= _probabilityToSpawnRarity[1] + rarityBonus => 2,
            _ => 3,
        };
    }

    /// <summary>
    /// Client RPC method for harvesting a crop at a given position.
    /// </summary>
    /// <param name="position">The position of the crop to harvest.</param>
    /// <param name="itemAmount">The number of items to spawn after harvesting.</param>
    /// <param name="itemRarity">The rarity of the spawned items.</param>
    [ClientRpc]
    private void HarvestCropClientRpc(Vector3Int position, int itemAmount, int itemRarity) {
        // Get the crop tile at the given position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);
        // Get the crop from the crop database using the crop ID
        CropSO crop = CropDatabase[cropTile.CropId];

        // Spawn items at the position of the harvested crop
        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(crop.ItemToGrowAndSpawn.ItemId, itemAmount, itemRarity),
            initialPosition: _targetTilemap.CellToWorld(position),
            motionDirection: Vector2.zero,
            spreadType: ItemSpawnManager.SpreadType.Circle);

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
        foreach (CropTile cropTile in CropTileContainer.CropTileMap.Values) {
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
        foreach (CropTile cropTile in CropTileContainer.CropTileMap.Values) {
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
        foreach (CropTile cropTile in CropTileContainer.CropTileMap.Values) {
            // If there's no crop on the tile, mark it as not watered and continue to the next tile.
            if (cropTile.CropId == -1
                || CropDatabase[cropTile.CropId].IsTree) {
                cropTile.IsWatered = false;
                VisualizeTileChanges(cropTile);
                continue;
            }

            // If the crop is not watered, increase its damage, else decrease the damage.
            cropTile.Damage += cropTile.IsWatered ? -_notWateredDamage : _notWateredDamage;

            // Generate a random number and check if the crop will die based on its damage.
            if (UnityEngine.Random.Range(0, _maxCropDamage) < cropTile.Damage) {
                cropTile.Damage = _maxCropDamage;
            }

            cropTile.IsWatered = UnityEngine.Random.Range(0, 100) < cropTile.WaterScaler;
            cropTile.WaterScaler = 0f;
            cropTile.Prefab.GetComponent<HarvestCrop>().SetFertilizerSprite(FertilizerSO.FertilizerTypes.Water); // Disable the sprite and set the color to white.

            // Visualize the changes to the tile.
            VisualizeTileChanges(cropTile);
        }
    }

    private void CheckForWaterFertilizedCrops() {
        // Iterate over all crop tiles.
        foreach (CropTile cropTile in CropTileContainer.CropTileMap.Values) {
            if (cropTile.WaterScaler > 0f) {
                cropTile.IsWatered = UnityEngine.Random.Range(0, 100) < cropTile.WaterScaler;
                cropTile.WaterScaler = 0f;
                cropTile.Prefab.GetComponent<HarvestCrop>().SetFertilizerSprite(FertilizerSO.FertilizerTypes.Water); // Disable the sprite and set the color to white.
            }
        }
    }
    #endregion


    #region Destroy Crop

    [ServerRpc(RequireOwnership = false)]
    public void DestroyCropTileServerRpc(Vector3Int position, int usedEnergy, ToolSO.ToolTypes toolTypes, ServerRpcParams serverRpcParams = default) {
        bool success;
        if (CropDatabase[CropTileContainer.GetCropTileAtPosition(position).CropId].IsTree) {
            success = false;
        } else {
            success = HandleToolAction(position, toolTypes);
        }

        HandleClientCallback(serverRpcParams, success);
        HandleEnergyReduction(serverRpcParams, usedEnergy);
    }

    private bool HandleToolAction(Vector3Int position, ToolSO.ToolTypes toolType) {
        return toolType switch {
            ToolSO.ToolTypes.Scythe => TryHandleCropWithScythe(position),
            ToolSO.ToolTypes.Pickaxe => TryHandleCropWithPickaxe(position),
            _ => throw new ArgumentOutOfRangeException(nameof(toolType), $"No valid tool type: {toolType}"),
        };
    }

    private bool TryHandleCropWithScythe(Vector3Int position) {
        if (!CropTileContainer.IsPositionSeeded(position)) return false;
        ScytheCrop(position);
        return true;
    }

    private bool TryHandleCropWithPickaxe(Vector3Int position) {
        if (!CropTileContainer.IsPositionPlowed(position)) return false;
        PickaxeCrop(position);
        return true;
    }

    private void ScytheCrop(Vector3Int position) {
        // Retrieve the crop tile at the specified position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);

        // Calculate the item count and rarity
        int itemCount = CalculateItemCount(cropTile);
        int itemRarity = CalculateItemRarity(cropTile.QualityScaler);

        // Retrieve the crop from the database
        CropSO crop = CropDatabase[cropTile.CropId];

        // Check if the crop is done growing and if it can be harvested by a scythe
        bool cropDoneGrowing = cropTile.IsCropHarvestable();
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

    [ClientRpc]
    public void DestroyCropTilePlantClientRpc(Vector3Int position) {
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);
        Destroy(cropTile.Prefab);
        cropTile.ResetCropTile();
    }

    [ClientRpc]
    public void DestroyCropTileClientRpc(Vector3Int position) {
        // Retrieve the crop tile from the container at the given position
        CropTile cropTile = CropTileContainer.GetCropTileAtPosition(position);

        // Remove the retrieved crop tile from the container
        CropTileContainer.RemoveCropTileFromContainer(cropTile);

        // Determine the appropriate tile based on whether the crop tile is watered or not
        TileBase targetTile = cropTile.IsWatered ? _dirtWet : _dirtDry;

        // Set the determined tile at the specified grid position on the target tilemap
        _targetTilemap.SetTile(position, targetTile);
    }

    private void DeleteSomeUnseededTiles() {
        // Create a list of tiles to remove, which are unseeded and should be removed based on probability
        var tilesToRemove = CropTileContainer.CropTileMap.Values
            .Where(cropTile => cropTile.CropId == -1 && ShouldRemoveTile())
            .ToList();

        // For each tile in the list, remove it from the container and set the tile at its position on the target tilemap to null
        foreach (var cropTile in tilesToRemove) {
            CropTileContainer.RemoveCropTileFromContainer(cropTile);
            _targetTilemap.SetTile(cropTile.CropPosition, null);
        }
    }

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

    private void OnThunderStrike() {
        // Get all cropTiles that are trees
        List<CropTile> treeCropTiles = new List<CropTile>();
        if (UnityEngine.Random.value < PROBABILITY_OF_THUNDER_STRIKE) {
            foreach (CropTile cropTile in CropTileContainer.CropTileMap.Values) {
                if (CropDatabase[cropTile.CropId].IsTree &&
                    cropTile.IsCropDoneGrowing()) {
                    treeCropTiles.Add(cropTile);
                }
            }
        }

        // Select random cropTile
        int selectedTreeCropTileIndex = UnityEngine.Random.Range(0, treeCropTiles.Count);
        CropTile selectedTreeCropTile = treeCropTiles[selectedTreeCropTileIndex];

        // Set struck by lightning
        // TODO: Play an animation to show that this tree is hit by a lightning
        selectedTreeCropTile.IsStruckByLightning = true;

        // Destroy crops on the tree if harvestable, let the tree drop coal instead
        if (selectedTreeCropTile.IsCropHarvestable()) {
            ItemSpawnManager.Instance.SpawnItemServerRpc(
                itemSlot: new ItemSlot(_coal.ItemId, CalculateItemCount(selectedTreeCropTile), 0),
                initialPosition: selectedTreeCropTile.Prefab.transform.position,
                motionDirection: Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle);

            HandleCropAfterHarvest(selectedTreeCropTile, CropDatabase[selectedTreeCropTile.CropId]);
        }
    }

    #region Save and Load
    /// <summary>
    /// Saves the game data, including the crops on the map.
    /// </summary>
    /// <param name="data">The game data to be saved.</param>
    public void SaveData(GameData data) {
        // If the flag to save crops is set, serialize the crop data and store it in the game data
        if (_saveCrops) {
            data.CropsOnMap = JsonConvert.SerializeObject(CropTileContainer.SerializeCropTileContainer());
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

    #region Test
    private void TestAliveCrops() {
        int XCoordinate = 34, YCoordinate = -4;

        for (int y = 0; y < 30; y++) { // Crop amount
            for (int x = 0; x < 4; x++) { // Sprite amount
                List<Vector3Int> position = new List<Vector3Int> {
                    new Vector3Int(x + XCoordinate, y + YCoordinate)
                };
                PlowTilesServerRpc(JsonConvert.SerializeObject(position), 0);
                SeedTileServerRpc(new Vector3Int(x + XCoordinate, y + YCoordinate), 0);
            }
        }



    }
    #endregion
}