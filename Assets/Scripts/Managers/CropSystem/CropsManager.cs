using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static CropTile;
using static FertilizerSO;

[Serializable]
public struct Vector3IntSerializable : INetworkSerializable {
    public int x, y, z;

    public Vector3IntSerializable(Vector3Int vector) {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public readonly Vector3Int ToVector3Int() => new(x, y, z);

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref x);
        serializer.SerializeValue(ref y);
        serializer.SerializeValue(ref z);
    }
}

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

    [Header("Reference: TileBases")]
    [SerializeField] private RuleTile _dirtDry;
    [SerializeField] private RuleTile _dirtWet;
    [SerializeField] private RuleTile _dirtPlowedDry;
    [SerializeField] private RuleTile _dirtPlowedWet;
    [SerializeField] private TileBase[] _tilesThatCanBePlowed;
    public TileBase[] TilesThatCanBePlowed => _tilesThatCanBePlowed;

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

    [Header("Reference: Items")]
    [SerializeField] private ItemSO _coal;
    private const float PROBABILITY_OF_THUNDER_STRIKE = 0.01f;

    // References
    private Tilemap _targetTilemap;
    private TilemapManager _tilemapReadManager;
    private PlaceableObjectsManager _placeableObjectsManager;

    public NetworkList<CropTile> CropTiles { get; private set; }


    #region Unity Lifecycle Methods

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of CropsManager in the scene!");
            return;
        }
        Instance = this;

        // Initialize Tilemap
        _targetTilemap = GetComponent<Tilemap>();

        // Initialize JSON settings
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
            Converters = { new Vector3Converter() },
        };

        // Initialize Crop Database
        CropDatabase.InitializeCrops();

        // Initialize NetworkList
        CropTiles = new NetworkList<CropTile>(new List<CropTile>());
        CropTiles.OnListChanged += OnCropTilesListChanged;
    }

    private void Start() {
        if (IsServer) {
            // Subscribe to events only on the server
            TimeManager.Instance.OnNextDayStarted += OnNextDayStarted;
            TimeManager.Instance.OnNextSeasonStarted += OnNextSeasonStarted;
            WeatherManager.Instance.OnChangeRainIntensity += OnChangeRainIntensity;
            WeatherManager.Instance.OnThunderStrike += OnThunderStrike;
        }

        // Get references to the TilemapManager and PlaceableObjectsManager instances
        _tilemapReadManager = TilemapManager.Instance;
        _placeableObjectsManager = PlaceableObjectsManager.Instance;
    }

    private void OnDestroy() {
        if (IsServer) {
            // Unsubscribe from events only on the server
            TimeManager.Instance.OnNextDayStarted -= OnNextDayStarted;
            TimeManager.Instance.OnNextSeasonStarted -= OnNextSeasonStarted;
            WeatherManager.Instance.OnChangeRainIntensity -= OnChangeRainIntensity;
            WeatherManager.Instance.OnThunderStrike -= OnThunderStrike;
            CropTiles.OnListChanged -= OnCropTilesListChanged;
        }
    }

    #endregion

    #region Network List Change Handler

    private void OnCropTilesListChanged(NetworkListEvent<CropTile> changeEvent) {
        switch (changeEvent.Type) {
            case NetworkListEvent<CropTile>.EventType.Add:
                HandleCropTileAdd(changeEvent.Value);
                break;
            case NetworkListEvent<CropTile>.EventType.RemoveAt:
                HandleCropTileRemove(changeEvent.Value);
                break;
            case NetworkListEvent<CropTile>.EventType.Value:
                HandleCropTileValueChange(changeEvent.Value);
                break;
            case NetworkListEvent<CropTile>.EventType.Clear:
                HandleCropTilesClear();
                break;
        }
    }

    private void HandleCropTileAdd(CropTile cropTile) {
        VisualizeTileChanges(cropTile);
    }

    private void HandleCropTileRemove(CropTile cropTile) {
        _targetTilemap.SetTile(cropTile.CropPosition, cropTile.IsWatered ? _dirtWet : _dirtDry);
    }

    private void HandleCropTileValueChange(CropTile cropTile) {
        VisualizeTileChanges(cropTile);

        // Reference the existing NetworkObject using PrefabNetworkObjectId
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject networkObject)) {
            VisualizeCropChanges(cropTile, networkObject);
        }
    }

    private void HandleCropTilesClear() {
        if (IsServer) {
            foreach (var cropTile in CropTiles) {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject networkObject)) {
                    networkObject.Despawn();
                }

                // Update the tilemap
                _targetTilemap.SetTile(cropTile.CropPosition, cropTile.IsWatered ? _dirtWet : _dirtDry);
            }
        }
    }

    #endregion

    #region Next Day and Season
    public void TestOnNextDayStarted() {
        OnNextDayStarted();
    }


    private void OnNextDayStarted() {
        for (int i = 0; i < CropTiles.Count; i++) {
            CropTile cropTile = CropTiles[i];

            if (cropTile.CropId >= 0 &&
                !cropTile.IsCropHarvestable()) {
                cropTile.CurrentGrowthTimer++;
                CropTiles[i] = cropTile;
            }
        }

        DeleteSomeUnseededTiles();
        CheckIfWateredAndApplyDamage();
        CheckForWaterFertilizedCrops();
    }

    private void OnNextSeasonStarted(int nextSeasonIndex) {
        // Iterate over each crop tile using indices to modify the NetworkList
        for (int i = 0; i < CropTiles.Count; i++) {
            CropTile cropTile = CropTiles[i];

            // If the crop tile has a crop and the crop cannot survive in the next season
            if (cropTile.CropId >= 0 && !CropDatabase[cropTile.CropId].SeasonsToGrow.Contains((TimeManager.SeasonName)nextSeasonIndex)) {
                // Set the damage of the crop to the maximum
                cropTile.Damage = _maxCropDamage;
                CropTiles[i] = cropTile;
            }
        }
    }

    #endregion Next Day and Season


    #region Visualize Crop Tiles on Map

    private void VisualizeTileChanges(CropTile cropTile) {
        Vector3Int position = cropTile.CropPosition;
        if (!_targetTilemap.HasTile(position)) {
            return;
        }

        bool isWet = _targetTilemap.GetTile<RuleTile>(position) == _dirtWet ||
                     cropTile.IsWatered;

        if (!GetCropTileAtPosition(position).HasValue) {
            _targetTilemap.SetTile(position, isWet ? _dirtWet : _dirtDry);
        } else {
            _targetTilemap.SetTile(position, isWet ? _dirtPlowedWet : _dirtPlowedDry);
        }
    }

    private void VisualizeCropChanges(CropTile cropTile, NetworkObject networkObject) {
        if (cropTile.CropId < 0) {
            Debug.Log($"At {cropTile.CropPosition}, CropId is -1");
            return;
        }

        CropSO cropSO = CropDatabase[cropTile.CropId];
        SpriteRenderer spriteRenderer = networkObject.GetComponent<SpriteRenderer>();
        HarvestCrop harvestCrop = networkObject.GetComponent<HarvestCrop>();

        if (cropSO == null) {
            Debug.LogError($"CropSO with ID {cropTile.CropId} not found in CropDatabase.");
            return;
        }

        // Update the sprite based on crop stage and health
        UpdateCropSprite(cropTile, cropSO, spriteRenderer);

        // Update fertilizer sprites if applicable
        UpdateFertilizerSprites(cropTile, harvestCrop);
    }

    private void UpdateCropSprite(CropTile cropTile, CropSO cropSO, SpriteRenderer spriteRenderer) {
        // If the crop is dead and its stage is not 0
        if (cropTile.IsDead() && cropTile.GetCropStage() != CropStage.Seeded) {
            spriteRenderer.sprite = cropSO.DeadSpritesGrowthStages[Mathf.Max(0, (int)cropTile.GetCropStage() - 1)];
        } else {
            spriteRenderer.sprite = cropSO.SpritesGrowthStages[Mathf.Clamp((int)cropTile.GetCropStage() - 1, 0, cropSO.SpritesGrowthStages.Count)];
        }
    }

    private void UpdateFertilizerSprites(CropTile cropTile, HarvestCrop harvestCrop) {
        // Dictionary to map fertilizer types to their respective lists
        var fertilizerMappings = new Dictionary<FertilizerTypes, List<FertilizerSO>> {
            { FertilizerTypes.GrowthTime, _growthFertilizer },
            { FertilizerTypes.RegrowthTime, _regrowthFertilizer },
            { FertilizerTypes.Quality, _qualityFertilizer },
            { FertilizerTypes.Quantity, _quantityFertilizer },
            { FertilizerTypes.Water, _waterFertilizer }
        };

        foreach (var mapping in fertilizerMappings) {
            var type = mapping.Key;
            var list = mapping.Value;

            float scaler = type switch {
                FertilizerTypes.GrowthTime => cropTile.GrowthTimeScaler,
                FertilizerTypes.RegrowthTime => cropTile.RegrowthTimeScaler,
                FertilizerTypes.Quality => cropTile.QualityScaler,
                FertilizerTypes.Quantity => cropTile.QuantityScaler,
                FertilizerTypes.Water => cropTile.WaterScaler,
                _ => 1f
            };

            if (scaler > 1f || (type == FertilizerTypes.Water && scaler > 0f)) {
                FertilizerSO fertilizer = list.Find(f => f.FertilizerBonusValue == Mathf.RoundToInt((type == FertilizerTypes.Water ? scaler : scaler - 1f) * 100));
                if (fertilizer != null) {
                    harvestCrop.SetFertilizerSprite(fertilizer.FertilizerType, fertilizer.FertilizerCropTileColor);
                }
            }
        }
    }

    private void CreateCropPrefab(ref CropTile cropTile, bool isTree, int itemId) {
        // Ensure this is only executed on the server
        if (!IsServer) {
            Debug.LogError("CreateCropPrefab should only be called on the server.");
            return;
        }

        // Instantiate the appropriate prefab
        GameObject prefabInstance = Instantiate(isTree ? _cropsTreeSpritePrefab : _cropsSpritePrefab, transform);
        HarvestCrop harvestCrop = prefabInstance.GetComponent<HarvestCrop>();

        if (isTree) {
            ResourceNodeBase resourceNode = prefabInstance.GetComponent<ResourceNodeBase>();
            resourceNode.SetSeed(ItemManager.Instance.ItemDatabase[itemId] as SeedSO);
        }

        // Position the prefab correctly
        Vector3 worldPosition = TilemapManager.Instance.AlignPositionToGridCenter(_targetTilemap.CellToWorld(cropTile.CropPosition));
        prefabInstance.transform.position = worldPosition + new Vector3(0, 0.5f) + new Vector3(cropTile.SpriteRendererOffset.x, cropTile.SpriteRendererOffset.y, -0.1f);
        prefabInstance.transform.localScale = new Vector3(cropTile.SpriteRendererXScale, 1, 1);

        // Set the crop position for harvesting
        harvestCrop.SetCropPosition(cropTile.CropPosition);

        // Ensure the prefab has a NetworkObject component
        if (!prefabInstance.TryGetComponent(out NetworkObject networkObject)) {
            networkObject = prefabInstance.AddComponent<NetworkObject>();
        }

        // Spawn the prefab on the network
        networkObject.Spawn();

        // Assign the NetworkObjectId to CropTile
        cropTile.PrefabNetworkObjectId = networkObject.NetworkObjectId;
    }

    #endregion Visualize crop tiles on map


    #region Plow Crop Tile

    [ServerRpc(RequireOwnership = false)]
    public void PlowTilesServerRpc(Vector3IntSerializable[] positionsSerializable, int usedEnergy, ServerRpcParams serverRpcParams = default) {
        if (positionsSerializable == null || positionsSerializable.Length == 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        using var canPlowTilePositions = new NativeList<Vector3Int>(Allocator.Temp);
        foreach (var positionSerializable in positionsSerializable) {
            Vector3Int position = positionSerializable.ToVector3Int();
            if (CanPlowTile(position)) {
                canPlowTilePositions.Add(position);
            }
        }

        bool hasPlowableTiles = canPlowTilePositions.Length > 0;
        HandleClientCallback(serverRpcParams, hasPlowableTiles);
        if (hasPlowableTiles) {
            HandleEnergyReduction(serverRpcParams, usedEnergy * canPlowTilePositions.Length);
        }

        foreach (var position in canPlowTilePositions) {
            // Create a new CropTile
            CropTile cropTile = new() {
                CropId = -1,
                CropPosition = position,
                IsWatered = _targetTilemap.GetTile<RuleTile>(position) == _dirtWet,
                CurrentGrowthTimer = 0,
                IsRegrowing = false,
                Damage = 0,
                SpriteRendererOffset = GenerateRandomSpriteRendererPosition(),
                SpriteRendererXScale = UnityEngine.Random.value < 0.5f ? 1 : -1,
                InGreenhouse = false,
                IsStruckByLightning = false,
                SeedItemId = -1,
                GrowthTimeScaler = 1f,
                RegrowthTimeScaler = 1f,
                QualityScaler = 1f,
                QuantityScaler = 1f,
                WaterScaler = 0f
            };

            CropTiles.Add(cropTile);
        }
    }

    private bool CanPlowTile(Vector3Int position) {
        // Check if the position is not already plowed, if the tile at the position can be plowed, and if there is no object placed at the position
        PlaceableObjectData? placeableObjectData = PlaceableObjectsManager.Instance.GetCropTileAtPosition(position);
        return !IsPositionPlowed(position) &&
               _tilesThatCanBePlowed.Contains(_tilemapReadManager.GetTileAtGridPosition(position)) &&
               !placeableObjectData.HasValue;
    }

    private void HandleEnergyReduction(ServerRpcParams serverRpcParams, int totalUsedEnergy) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject.TryGetComponent<PlayerHealthAndEnergyController>(out var energyController)) {
                energyController.AdjustEnergy(-totalUsedEnergy);
            } else {
                Debug.LogError($"PlayerHealthAndEnergyController not found on Client {clientId}");
            }
        }
    }

    #endregion Plow Crop Tile


    #region Fertilize Crop Tile

    [ServerRpc(RequireOwnership = false)]
    public void FertilizeTileServerRpc(Vector3IntSerializable positionSerializable, int itemId, ServerRpcParams serverRpcParams = default) {
        Vector3Int position = positionSerializable.ToVector3Int();
        CropTile? cropTileData = GetCropTileAtPosition(position);

        if (!cropTileData.HasValue) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        CropTile cropTile = cropTileData.Value;
        CropSO cropSO = CropDatabase[cropTile.CropId];

        if (!IsPositionPlowed(position) ||
            !IsPositionSeeded(position) ||
            !CanPositionBeFertilized(position, itemId) ||
            cropSO.IsTree) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleItemReduction(serverRpcParams, itemId);
        HandleClientCallback(serverRpcParams, true);

        int index = FindCropTileIndexAtPosition(position);
        if (index >= 0) {
            CropTile updatedCropTile = CropTiles[index];
            FertilizerSO fertilizerSO = ItemManager.Instance.ItemDatabase[itemId] as FertilizerSO;

            if (fertilizerSO != null) {
                SetFertilizerValue(ref updatedCropTile, fertilizerSO);
                CropTiles[index] = updatedCropTile;
            } else {
                Debug.LogError($"FertilizerSO not found for itemId {itemId}");
            }
        }
    }

    private void SetFertilizerValue(ref CropTile cropTile, FertilizerSO fertilizerSO) {
        switch (fertilizerSO.FertilizerType) {
            case FertilizerTypes.GrowthTime:
                cropTile.GrowthTimeScaler = (fertilizerSO.FertilizerBonusValue / 100) + 1;
                break;
            case FertilizerTypes.RegrowthTime:
                cropTile.RegrowthTimeScaler = (fertilizerSO.FertilizerBonusValue / 100) + 1;
                break;
            case FertilizerTypes.Quality:
                cropTile.QualityScaler = fertilizerSO.FertilizerBonusValue;
                break;
            case FertilizerTypes.Quantity:
                cropTile.QuantityScaler = (fertilizerSO.FertilizerBonusValue / 100) + 1;
                break;
            case FertilizerTypes.Water:
                cropTile.WaterScaler = fertilizerSO.FertilizerBonusValue;
                break;
            default:
                throw new NotSupportedException($"Unsupported fertilizer type: {fertilizerSO.FertilizerType}");
        }
    }

    #endregion


    #region Seed Crop Tile

    [ServerRpc(RequireOwnership = false)]
    public void SeedTileServerRpc(Vector3IntSerializable positionSerializable, int itemId, ServerRpcParams serverRpcParams = default) {
        Vector3Int position = positionSerializable.ToVector3Int();
        SeedSO seedSO = ItemManager.Instance.ItemDatabase[itemId] as SeedSO;

        if (seedSO == null) {
            HandleClientCallback(serverRpcParams, false);
            Debug.LogError($"Invalid SeedSO for itemId {itemId}");
            return;
        }

        CropSO cropToGrow = seedSO.CropToGrow;

        if (cropToGrow == null) {
            HandleClientCallback(serverRpcParams, false);
            Debug.LogError($"CropSO to grow is null for SeedSO {seedSO.name}");
            return;
        }

        PlaceableObjectData? placeableObjectData = PlaceableObjectsManager.Instance.GetCropTileAtPosition(position);
        if (cropToGrow.IsTree) {
            if (!IsPositionSeeded(position) &&
                cropToGrow.SeasonsToGrow.Contains((TimeManager.SeasonName)TimeManager.Instance.CurrentDate.Value.Season) &&
                _tilesThatCanBePlowed.Contains(_tilemapReadManager.GetTileAtGridPosition(position)) &&
                !placeableObjectData.HasValue) {
                // Implement tree planting logic here if necessary
            } else {
                HandleClientCallback(serverRpcParams, false);
                return;
            }
        } else if (!IsPositionPlowed(position) ||
                   IsPositionSeeded(position) ||
                   !cropToGrow.SeasonsToGrow.Contains((TimeManager.SeasonName)TimeManager.Instance.CurrentDate.Value.Season)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        CropTile? changeCropTileData = GetCropTileAtPosition(position);
        if (!changeCropTileData.HasValue) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleItemReduction(serverRpcParams, itemId);
        HandleClientCallback(serverRpcParams, true);

        CropTile changeCropTile = changeCropTileData.Value;
        changeCropTile.CropId = cropToGrow.CropID;
        changeCropTile.SeedItemId = itemId;

        // Spawn the prefab and update the NetworkObjectId
        CreateCropPrefab(ref changeCropTile, cropToGrow.IsTree, itemId);

        // Update the CropTile in the NetworkList with the new NetworkObjectId
        int index = FindCropTileIndexAtPosition(position);
        if (index >= 0) {
            CropTiles[index] = changeCropTile;
        }
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

    private Vector3 GenerateRandomSpriteRendererPosition() {
        // Generate and return a new Vector3 with random x and y coordinates within the specified range
        return new Vector3(
            UnityEngine.Random.Range(-_cropPositionSpread, _cropPositionSpread),
            UnityEngine.Random.Range(-_cropPositionSpread, _cropPositionSpread),
            0f
        );
    }

    #endregion Seed Crop Tile


    #region Harvest Crop

    [ServerRpc(RequireOwnership = false)]
    public void HarvestCropServerRpc(Vector3IntSerializable positionSerializable, ServerRpcParams serverRpcParams = default) {
        Vector3Int position = positionSerializable.ToVector3Int();

        CropTile? changeCropTileData = GetCropTileAtPosition(position);
        if (!changeCropTileData.HasValue || !CanHarvestCrop(position, changeCropTileData.Value)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }
        CropTile cropTile = changeCropTileData.Value;

        // Calculate item count and rarity
        int itemCountToSpawn = CalculateItemCount(cropTile);
        int itemRarity = CalculateItemRarity(cropTile.QualityScaler);

        HandleClientCallback(serverRpcParams, true);

        // Spawn the harvested items
        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(CropDatabase[cropTile.CropId].ItemToGrowAndSpawn.ItemId, itemCountToSpawn, itemRarity),
            initialPosition: _targetTilemap.CellToWorld(position),
            motionDirection: Vector2.zero,
            spreadType: ItemSpawnManager.SpreadType.Circle);

        // Handle crop after harvest
        HandleCropAfterHarvest(position);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HarvestTreeServerRpc(Vector3IntSerializable positionSerializable, ServerRpcParams serverRpcParams = default) {
        Vector3Int position = positionSerializable.ToVector3Int();
        int index = FindCropTileIndexAtPosition(position);

        if (index >= 0) {
            CropTile cropTile = CropTiles[index];
            int itemCountToSpawn = CalculateItemCount(cropTile);
            int itemRarity = CalculateItemRarity(cropTile.QualityScaler);

            ItemSpawnManager.Instance.SpawnItemServerRpc(
                itemSlot: new ItemSlot(cropTile.CropId, itemCountToSpawn, itemRarity),
                initialPosition: _targetTilemap.CellToWorld(position),
                motionDirection: Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle);

            HandleCropAfterHarvest(position);
        } else {
            HandleClientCallback(serverRpcParams, false);
        }
    }

    private bool CanHarvestCrop(Vector3Int gridPosition, CropTile cropTile) {
        if (!IsPositionSeeded(gridPosition) ||
            !cropTile.IsCropHarvestable() ||
            CropDatabase[cropTile.CropId].IsTree) {
            return false;
        }

        return true;
    }

    public int CalculateItemCount(CropTile cropTile) {
        // Get the crop from the crop database using the crop ID
        CropSO crop = CropDatabase[cropTile.CropId];
        if (crop == null) {
            Debug.LogError($"CropSO with ID {cropTile.CropId} not found in CropDatabase.");
            return 0;
        }

        // Adjust min and max items based on the QuantityScaler
        int minItems = Mathf.RoundToInt(crop.MinItemAmountToSpawn * cropTile.QuantityScaler);
        int maxItems = Mathf.RoundToInt(crop.MaxItemAmountToSpawn * cropTile.QuantityScaler);

        // Calculate the item count by generating a random number between the adjusted minimum and maximum item amount to spawn
        return UnityEngine.Random.Range(minItems, maxItems + 1); // Inclusive upper bound
    }

    public int CalculateItemRarity(float rarityBonus) {
        // Generate a random number between 0 and 100
        int rarityToSpawn = UnityEngine.Random.Range(0, 100);
        int cumulativeProbability = 0;

        for (int i = _probabilityToSpawnRarity.Length - 1; i >= 0; i--) {
            cumulativeProbability += _probabilityToSpawnRarity[i];
            if (rarityToSpawn < cumulativeProbability + Mathf.RoundToInt(rarityBonus)) {
                return i;
            }
        }

        return _probabilityToSpawnRarity.Length - 1; // Default rarity
    }

    private void HandleCropAfterHarvest(Vector3Int position) {
        if (!IsServer) {
            return;
        }

        int index = FindCropTileIndexAtPosition(position);
        if (index >= 0) {
            CropTile cropTile = CropTiles[index];
            CropSO crop = CropDatabase[cropTile.CropId];

            if (crop == null) {
                Debug.LogError($"CropSO with ID {cropTile.CropId} not found in CropDatabase.");
                return;
            }

            if (crop.CanRegrow) {
                cropTile.IsRegrowing = true;
                cropTile.CurrentGrowthTimer -= crop.DaysToRegrow;
                CropTiles[index] = cropTile;
            } else {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject prefabNetworkObject)) {
                    prefabNetworkObject.Despawn();
                }

                CropTiles.RemoveAt(index);
            }
        }
    }

    #endregion


    #region Water Crop Tile

    #region Watering Can

    [ServerRpc(RequireOwnership = false)]
    public void WaterCropTileServerRpc(Vector3IntSerializable[] wantToWaterTilePositionsSerializable, int usedEnergy, ServerRpcParams serverRpcParams = default) {
        if (wantToWaterTilePositionsSerializable == null || wantToWaterTilePositionsSerializable.Length == 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        using var canWaterTilePositions = new NativeList<Vector3Int>(Allocator.Temp);
        using var changeRuleTilePositions = new NativeList<Vector3Int>(Allocator.Temp);
        foreach (var positionSerializable in wantToWaterTilePositionsSerializable) {
            Vector3Int position = positionSerializable.ToVector3Int();
            TileBase tileBase = _targetTilemap.GetTile<TileBase>(position);

            if (IsPositionPlowed(position)) {
                canWaterTilePositions.Add(position);
            } else if (tileBase == _dirtDry) {
                changeRuleTilePositions.Add(position);
            }
        }

        HandleEnergyReduction(serverRpcParams, usedEnergy * wantToWaterTilePositionsSerializable.Length);
        HandleClientCallback(serverRpcParams, true);

        foreach (var position in canWaterTilePositions) {
            int index = FindCropTileIndexAtPosition(position);
            if (index >= 0) {
                CropTile cropTile = CropTiles[index];
                cropTile.IsWatered = true;
                CropTiles[index] = cropTile;
            }
        }
    }

    #endregion Watering Can

    private void OnChangeRainIntensity(int intensity) {
        if (intensity == 0) { // No rain
            DryAllCropTiles();
        } else {
            WaterAllCropTiles();
        }
    }

    private void WaterAllCropTiles() {
        // Iterate over each crop tile using indices to modify the NetworkList
        for (int i = 0; i < CropTiles.Count; i++) {
            CropTile cropTile = CropTiles[i];
            cropTile.IsWatered = true;
            CropTiles[i] = cropTile;
        }

        foreach (Vector3Int gridPosition in _targetTilemap.cellBounds.allPositionsWithin) {
            if (_targetTilemap.HasTile(gridPosition) && _targetTilemap.GetTile(gridPosition) == _dirtDry) {
                _targetTilemap.SetTile(gridPosition, _dirtWet);
            }
        }
    }

    private void DryAllCropTiles() {
        // Iterate over each crop tile using indices to modify the NetworkList
        for (int i = 0; i < CropTiles.Count; i++) {
            CropTile cropTile = CropTiles[i];
            cropTile.IsWatered = false;
            CropTiles[i] = cropTile;
        }

        foreach (Vector3Int gridPosition in _targetTilemap.cellBounds.allPositionsWithin) {
            if (_targetTilemap.HasTile(gridPosition) && _targetTilemap.GetTile(gridPosition) == _dirtWet) {
                _targetTilemap.SetTile(gridPosition, _dirtDry);
            }
        }
    }

    private void CheckIfWateredAndApplyDamage() {
        // Iterate over each crop tile using indices to modify the NetworkList
        for (int i = 0; i < CropTiles.Count; i++) {
            CropTile cropTile = CropTiles[i];

            if (cropTile.CropId == -1 || CropDatabase[cropTile.CropId].IsTree) {
                cropTile.IsWatered = false;
                CropTiles[i] = cropTile;
                continue;
            }

            cropTile.Damage += cropTile.IsWatered ? -_notWateredDamage : _notWateredDamage;

            // Clamp damage between 0 and max
            cropTile.Damage = Mathf.Clamp(cropTile.Damage, 0, _maxCropDamage);

            // Generate a random number and check if the crop will die based on its damage.
            if (UnityEngine.Random.Range(0, _maxCropDamage) < cropTile.Damage) {
                cropTile.Damage = _maxCropDamage;
            }

            cropTile.IsWatered = UnityEngine.Random.Range(0, 100) < cropTile.WaterScaler;
            cropTile.WaterScaler = 0f;

            if (cropTile.PrefabNetworkObjectId != 0 &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject networkObject)) {
                if (networkObject.TryGetComponent<HarvestCrop>(out var harvestCrop)) {
                    harvestCrop.SetFertilizerSprite(FertilizerTypes.Water); // Disable the sprite and set the color to white.
                }
            }

            CropTiles[i] = cropTile;
        }
    }

    private void CheckForWaterFertilizedCrops() {
        // Iterate over each crop tile using indices to modify the NetworkList
        for (int i = 0; i < CropTiles.Count; i++) {
            CropTile cropTile = CropTiles[i];

            if (cropTile.WaterScaler > 0f) {
                cropTile.IsWatered = UnityEngine.Random.Range(0, 100) < cropTile.WaterScaler;
                cropTile.WaterScaler = 0f;

                if (cropTile.PrefabNetworkObjectId != 0 &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject networkObject)) {
                    if (networkObject.TryGetComponent<HarvestCrop>(out var harvestCrop)) {
                        harvestCrop.SetFertilizerSprite(FertilizerTypes.Water); // Disable the sprite and set the color to white.
                    }
                }

                CropTiles[i] = cropTile;
            }
        }
    }

    #endregion Water Crop Tile


    #region Destroy Crop

    [ServerRpc(RequireOwnership = false)]
    public void DestroyCropTileServerRpc(Vector3IntSerializable positionSerializable, int usedEnergy, ToolSO.ToolTypes toolType, ServerRpcParams serverRpcParams = default) {
        Vector3Int position = positionSerializable.ToVector3Int();
        CropTile? cropTileData = GetCropTileAtPosition(position);

        if (!cropTileData.HasValue) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        CropTile cropTile = cropTileData.Value;

        bool success;
        if (cropTile.CropId >= 0) {
            CropSO cropSO = CropDatabase[cropTile.CropId];

            if (cropSO.IsTree) {
                success = false; // Trees are harvested differently
            } else {
                success = HandleToolAction(position, toolType);
            }
        } else {
            success = HandleToolAction(position, toolType);
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
        if (!IsPositionSeeded(position)) {
            return false;
        }

        int index = FindCropTileIndexAtPosition(position);
        if (index < 0) {
            return false;
        }

        CropTile cropTile = CropTiles[index];
        CropSO crop = CropDatabase[cropTile.CropId];

        if (cropTile.IsCropHarvestable() && !crop.IsTree) {
            HarvestCropServerRpc(new Vector3IntSerializable(position));
        }

        if (crop.IsHarvestedByScythe && crop.CanRegrow) {
            return true;
        } else {
            DespawnCrop(cropTile);
            RemoveCropTile(index);
            return true;
        }
    }

    private bool TryHandleCropWithPickaxe(Vector3Int position) {
        if (!IsPositionPlowed(position)) {
            return false;
        }

        int index = FindCropTileIndexAtPosition(position);
        if (index < 0) {
            return false;
        }

        CropTile cropTile = CropTiles[index];
        if (cropTile.CropId >= 0) {
            cropTile.CropId = -1;
            CropTiles[index] = cropTile;
            DespawnCrop(cropTile);
            return true;
        } else {
            DespawnCrop(cropTile);
            RemoveCropTile(index);
            return true;
        }
    }

    private void DespawnCrop(CropTile cropTile) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject prefabNetworkObject)) {
            prefabNetworkObject.Despawn();
        }
    }

    private void RemoveCropTile(int index) {
        CropTiles.RemoveAt(index);
    }

    private void DeleteSomeUnseededTiles() {
        List<CropTile> tilesToRemove = new List<CropTile>();

        // Iterate through CropTiles and identify tiles to remove
        foreach (var cropTile in CropTiles) {
            if (cropTile.CropId == -1 &&
                UnityEngine.Random.Range(0, 100) < _probabilityToDeleteUnseededTile) {
                tilesToRemove.Add(cropTile);
            }
        }

        // Remove identified tiles
        foreach (var cropTile in tilesToRemove) {
            _targetTilemap.SetTile(cropTile.CropPosition, null);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject prefabNetworkObject)) {
                prefabNetworkObject.Despawn();
            }

            CropTiles.Remove(cropTile); // Ensure to remove from the NetworkList
        }
    }

    #endregion Destroy Crop

    #region Crow attack

    // TODO
    private void Update() {
        Debug.Log("Press F12 to check protection and trigger crow attack");
        if (Input.GetKeyDown(KeyCode.F12)) {
            Debug.Log("CheckProtectionAndTriggerCrowAttack");
            CheckProtectionAndTriggerCrowAttack();
        }
    }

    // Methode zur Überprüfung des Schutzes und Auslösen von Krähenangriffen
    private void CheckProtectionAndTriggerCrowAttack() {
        List<CropTile> unprotectedCropTiles = new List<CropTile>();

        foreach (var cropTile in CropTiles) {
            if (cropTile.CropId < 0) {
                continue;
            }

            if (cropTile.GetCropStage() != CropStage.Seeded && !cropTile.IsCropHarvestable()) {
                Debug.Log($"Crop at {cropTile.CropPosition} is not harvestable or just seeded.");
                continue;
            }

            if (IsProtected(cropTile.CropPosition)) {
                Debug.Log($"Crop at {cropTile.CropPosition} is protected.");
                continue;
            }

            unprotectedCropTiles.Add(cropTile);
        }

        if (unprotectedCropTiles.Count == 0) {
            Debug.Log("All CropTiles are protected. No crow attacks possible.");
            return;
        }

        // Select 3 random unprotected CropTiles for crow attack
        List<CropTile> tilesToAttack = GetRandomCropTiles(unprotectedCropTiles, 3);

        foreach (var tile in tilesToAttack) {
            TriggerCrowAttack(tile);
        }
    }

    // Überprüft, ob ein CropTile an der gegebenen Position geschützt ist
    private bool IsProtected(Vector3Int position) {
        int[] scarecrowRadii = { 5, 9, 15 };
        ScarecrowType[] scarecrowTypes = { ScarecrowType.ScarecrowV1, ScarecrowType.ScarecrowV2, ScarecrowType.ScarecrowV3 };

        for (int i = 0; i < scarecrowRadii.Length; i++) {
            if (HasScarecrowInRange(position, scarecrowRadii[i], scarecrowTypes[i])) {
                return true;
            }
        }

        return false;
    }

    // Überprüft, ob innerhalb eines bestimmten Radius eine Vogelscheuche des angegebenen Typs existiert
    private bool HasScarecrowInRange(Vector3Int position, int radius, ScarecrowType type) {
        // Definiere den Bereich basierend auf dem Radius
        int minX = position.x - radius;
        int maxX = position.x + radius;
        int minY = position.y - radius;
        int maxY = position.y + radius;

        for (int x = minX; x <= maxX; x++) {
            for (int y = minY; y <= maxY; y++) {
                Vector3Int checkPos = new Vector3Int(x, y, position.z);

                PlaceableObjectData? placeableObjectData = PlaceableObjectsManager.Instance.GetCropTileAtPosition(checkPos);
                if (!placeableObjectData.HasValue) {
                    continue;
                }
                PlaceableObjectData placeableObject = placeableObjectData.Value;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObject.PrefabNetworkObjectId, out NetworkObject networkObject)) {
                    if (networkObject.GetComponent<Scarecrow>().ScarecrowSO.Type == type) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private List<CropTile> GetRandomCropTiles(List<CropTile> tiles, int count) {
        if (tiles.Count <= count) {
            return new List<CropTile>(tiles);
        }

        return tiles.OrderBy(t => Guid.NewGuid()).Take(count).ToList();
    }

    private void TriggerCrowAttack(CropTile tile) {
        DestroyCropTileServerRpc(new Vector3IntSerializable(tile.CropPosition), 0, ToolSO.ToolTypes.Pickaxe);

        // TODO
        // Show visual crow flying to the crop and destroying it
    }

    #endregion


    #region Save and Load

    public void SaveData(GameData data) {
        if (_saveCrops) {
            data.CropsOnMap = JsonConvert.SerializeObject(CropTiles);
        }
    }

    public void LoadData(GameData data) {
        if (!string.IsNullOrEmpty(data.CropsOnMap) && _loadCrops) {
            List<CropTile> cropTileList = JsonConvert.DeserializeObject<List<CropTile>>(data.CropsOnMap);
            CropTiles.Clear();

            for (int i = 0; i < cropTileList.Count; i++) {
                CropTile cropTile = cropTileList[i];

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out NetworkObject networkObject)) {
                    VisualizeCropChanges(cropTile, networkObject);
                }

                CropTiles.Add(cropTile);
                CreateCropPrefab(ref cropTile, CropDatabase[cropTile.CropId], cropTile.SeedItemId);
            }
        }
    }

    #endregion


    #region Utility Methods

    public CropTile? GetCropTileAtPosition(Vector3Int position) {
        for (int i = 0; i < CropTiles.Count; i++) {
            if (CropTiles[i].CropPosition.Equals(position)) {
                return CropTiles[i];
            }
        }
        return null;
    }

    public bool IsPositionPlowed(Vector3Int position) {
        return _targetTilemap.HasTile(position) &&
               (_targetTilemap.GetTile(position) == _dirtPlowedDry ||
                _targetTilemap.GetTile(position) == _dirtPlowedWet);
    }

    public bool IsPositionSeeded(Vector3Int position) {
        for (int i = 0; i < CropTiles.Count; i++) {
            if (CropTiles[i].CropPosition.Equals(position) && CropTiles[i].CropId >= 0) {
                return true;
            }
        }
        return false;
    }

    public bool CanPositionBeFertilized(Vector3Int position, int itemId) {
        CropTile? cropTileData = GetCropTileAtPosition(position);
        if (!cropTileData.HasValue) {
            return false;
        }

        CropTile cropTile = cropTileData.Value;
        FertilizerSO fertilizerSO = ItemManager.Instance.ItemDatabase[itemId] as FertilizerSO;

        if (fertilizerSO == null) {
            throw new ArgumentException("Invalid item ID for fertilizer.", nameof(itemId));
        }

        return fertilizerSO.FertilizerType switch {
            FertilizerTypes.GrowthTime => cropTile.GrowthTimeScaler < (fertilizerSO.FertilizerBonusValue / 100) + 1,
            FertilizerTypes.RegrowthTime => cropTile.RegrowthTimeScaler < (fertilizerSO.FertilizerBonusValue / 100) + 1 && cropTile.IsRegrowing,
            FertilizerTypes.Quality => cropTile.QualityScaler < fertilizerSO.FertilizerBonusValue,
            FertilizerTypes.Quantity => cropTile.QuantityScaler < (fertilizerSO.FertilizerBonusValue / 100) + 1,
            FertilizerTypes.Water => cropTile.WaterScaler < (fertilizerSO.FertilizerBonusValue / 100),
            _ => false,
        };
    }

    private int FindCropTileIndexAtPosition(Vector3Int position) {
        for (int i = 0; i < CropTiles.Count; i++) {
            if (CropTiles[i].CropPosition.Equals(position)) {
                return i;
            }
        }
        return -1;
    }

    #endregion


    #region Thunder Strike

    private void OnThunderStrike() {
        if (UnityEngine.Random.value >= PROBABILITY_OF_THUNDER_STRIKE) {
            return;
        }

        // Get all cropTiles that are trees and have finished growing
        List<CropTile> treeCropTiles = new List<CropTile>();
        foreach (var cropTile in CropTiles) {
            CropSO cropSO = CropDatabase[cropTile.CropId];
            if (cropSO != null && cropSO.IsTree && cropTile.IsCropDoneGrowing()) {
                treeCropTiles.Add(cropTile);
            }
        }

        if (treeCropTiles.Count == 0) {
            return;
        }

        // Select random cropTile
        int selectedIndex = UnityEngine.Random.Range(0, treeCropTiles.Count);
        CropTile selectedTreeCropTile = treeCropTiles[selectedIndex];

        // Set struck by lightning
        selectedTreeCropTile.IsStruckByLightning = true;
        CropTiles[FindCropTileIndexAtPosition(selectedTreeCropTile.CropPosition)] = selectedTreeCropTile;

        // Destroy crops on the tree if harvestable, let the tree drop coal instead
        if (selectedTreeCropTile.IsCropHarvestable()) {
            ItemSpawnManager.Instance.SpawnItemServerRpc(
                itemSlot: new ItemSlot(_coal.ItemId, CalculateItemCount(selectedTreeCropTile), 0),
                initialPosition: new Vector2(selectedTreeCropTile.CropPosition.x, selectedTreeCropTile.CropPosition.y) + selectedTreeCropTile.SpriteRendererOffset,
                motionDirection: Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle);

            HandleCropAfterHarvest(selectedTreeCropTile.CropPosition);
        }
    }

    #endregion

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
}
