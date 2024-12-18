using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using static CropTile;
using static FertilizerSO;

/// <summary>
/// Manages the crops in the game, including planting, growing, and harvesting.
/// Implements the IDataPersistance interface for data persistence.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class CropsManager : NetworkBehaviour, IDataPersistance {
    public static CropsManager Instance { get; private set; }

    [Header("Debug: Save and Load")]
    [SerializeField] bool _saveCrops = true;
    [SerializeField] bool _loadCrops = true;

    [Header("Params: Crop")]
    [SerializeField] float _cropRenderPositionSpread = 0.1f;
    [SerializeField] int _notWateredDamage = 25;
    [SerializeField] int _maxCropDamage = 100;
    [SerializeField] int _probabilityToDeleteUnseededTile = 25;
    [SerializeField] int[] _probabilityToSpawnRarity = { 70, 20, 8, 2 }; //70% Copper, 20% Iron, 8% Gold, 2% Diamond

    [Header("Reference: TileBases")]
    [SerializeField] RuleTile _dirtDry;
    [SerializeField] RuleTile _dirtWet;
    [SerializeField] RuleTile _dirtPlowedDry;
    [SerializeField] RuleTile _dirtPlowedWet;
    [SerializeField] TileBase[] _tilesThatCanBePlowed;
    public TileBase[] TilesThatCanBePlowed => _tilesThatCanBePlowed;

    [Header("Reference: Fertilizer")]
    [SerializeField] List<FertilizerSO> _growthFertilizer;
    [SerializeField] List<FertilizerSO> _regrowthFertilizer;
    [SerializeField] List<FertilizerSO> _qualityFertilizer;
    [SerializeField] List<FertilizerSO> _quantityFertilizer;
    [SerializeField] List<FertilizerSO> _waterFertilizer;

    [Header("Reference: After Harvest")]
    [SerializeField] Sprite _cropHole;

    [Header("Reference: Prefab")]
    [SerializeField] GameObject _cropsSpritePrefab;
    [SerializeField] GameObject _cropsTreeSpritePrefab;

    [Header("Reference: Database")]
    public CropDatabaseSO CropDatabase; // Public for external references.

    [Header("Reference: Items")]
    [SerializeField] ItemSO _coal;
    const float PROBABILITY_OF_THUNDER_STRIKE = 0.01f;

    // References
    Tilemap _targetTilemap;
    PlaceableObjectsManager _placeableObjectsManager;

    public NetworkList<CropTile> CropTiles { get; private set; }

    // Scarecrow protection configuration
    readonly int[] _scarecrowRadii = { 5, 9, 15 };
    readonly ScarecrowType[] _scarecrowTypes = { ScarecrowType.ScarecrowV1, ScarecrowType.ScarecrowV2, ScarecrowType.ScarecrowV3 };


    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one CropsManager instance in the scene!");
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
        CropTiles = new NetworkList<CropTile>();
        CropTiles.OnListChanged += OnCropTilesListChanged;
    }

    void Start() {
        _placeableObjectsManager = PlaceableObjectsManager.Instance;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            TimeManager.Instance.OnNextDayStarted += OnNextDayStarted;
            TimeManager.Instance.OnNextSeasonStarted += OnNextSeasonStarted;
            WeatherManager.Instance.OnChangeRainIntensity += OnChangeRainIntensity;
            WeatherManager.Instance.OnThunderStrike += OnThunderStrike;
        }
    }

    void OnDestroy() {
        CropTiles.OnListChanged -= OnCropTilesListChanged;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsServer) {
            TimeManager.Instance.OnNextDayStarted -= OnNextDayStarted;
            TimeManager.Instance.OnNextSeasonStarted -= OnNextSeasonStarted;
            WeatherManager.Instance.OnChangeRainIntensity -= OnChangeRainIntensity;
            WeatherManager.Instance.OnThunderStrike -= OnThunderStrike;
        }
    }


    #region -------------------- Network List Handlers --------------------

    void OnCropTilesListChanged(NetworkListEvent<CropTile> changeEvent) {
        switch (changeEvent.Type) {
            case NetworkListEvent<CropTile>.EventType.Add:
                HandleCropTileAdd(changeEvent.Value);
                break;
            case NetworkListEvent<CropTile>.EventType.RemoveAt:
                HandleCropTileRemoveAt(changeEvent.Value);
                break;
            case NetworkListEvent<CropTile>.EventType.Value:
                HandleCropTileValueChange(changeEvent.Value);
                break;
            case NetworkListEvent<CropTile>.EventType.Clear:
                HandleCropTilesClear();
                break;
        }
    }

    // Adds a new CropTile visualization.
    void HandleCropTileAdd(CropTile cropTile) {
        VisualizeTileChanges(cropTile);
    }

    // Removes a CropTile and updates tilemap accordingly.
    void HandleCropTileRemoveAt(CropTile cropTile) {
        _targetTilemap.SetTile(cropTile.CropPosition, cropTile.IsWatered ? _dirtWet : _dirtDry);
    }

    // Updates a changed CropTile (e.g. growth stage).
    void HandleCropTileValueChange(CropTile cropTile) {
        VisualizeTileChanges(cropTile);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out var networkObject)) {
            VisualizeCropChanges(cropTile, networkObject);
        }
    }

    // Clears all CropTiles.
    void HandleCropTilesClear() {
        if (!IsServer) return;

        // Clear all crops on server side.
        foreach (var cropTile in CropTiles) {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropTile.PrefabNetworkObjectId, out var networkObject)) {
                networkObject.Despawn();
            }
            _targetTilemap.SetTile(cropTile.CropPosition, cropTile.IsWatered ? _dirtWet : _dirtDry);
        }
    }

    #endregion -------------------- Network List Handlers --------------------

    #region -------------------- Day & Season Changes --------------------
    public void TestOnNextDayStarted() {
        OnNextDayStarted();
    }

    // Called at start of next day. Could be jobified for performance if data structures allow.
    void OnNextDayStarted() {
        // Increase growth timers of non-harvestable crops.
        for (int i = 0; i < CropTiles.Count; i++) {
            var tile = CropTiles[i];
            if (tile.CropId >= 0 && !tile.IsCropHarvestable(CropDatabase)) {
                tile.CurrentGrowthTimer++;
                CropTiles[i] = tile;
            }
        }

        RandomlyDeleteSomeUnseededTiles();
        CheckIfWateredAndApplyDamage();
    }

    // Called at start of next season, handle seasonal changes to crops.
    void OnNextSeasonStarted(int nextSeasonIndex) {
        for (int i = 0; i < CropTiles.Count; i++) {
            var tile = CropTiles[i];
            if (tile.CropId < 0) continue;
            var crop = CropDatabase[tile.CropId];
            if (!crop.SeasonsToGrow.Contains((TimeManager.SeasonName)nextSeasonIndex)) {
                tile.Damage = _maxCropDamage;
                CropTiles[i] = tile;
            }
        }
    }

    #endregion -------------------- Day & Season Changes --------------------


    #region -------------------- Visualization --------------------
    // Updates ground tiles (dry/wet/plowed).
    void VisualizeTileChanges(CropTile tile) {
        var pos = tile.CropPosition;
        if (!_targetTilemap.HasTile(pos)) return;

        bool isWet = tile.IsWatered || _targetTilemap.GetTile<RuleTile>(pos) == _dirtWet;
        bool hasValue = GetCropTileAtPosition(pos).HasValue;
        bool isTree = tile.CropId >= 0 && CropDatabase[tile.CropId].IsTree;

        if (!isTree) {
            _targetTilemap.SetTile(pos, hasValue ? (isWet ? _dirtPlowedWet : _dirtPlowedDry) : (isWet ? _dirtWet : _dirtDry));
        }
    }

    // Updates crop sprites and fertilizer visuals.
    void VisualizeCropChanges(CropTile tile, NetworkObject networkObject) {
        if (tile.CropId < 0) return;
        var crop = CropDatabase[tile.CropId];
        if (crop == null) {
            Debug.LogError($"CropSO with ID {tile.CropId} not found.");
            return;
        }

        if (!networkObject.TryGetComponent<SpriteRenderer>(out var sr)) return;
        if (!networkObject.TryGetComponent<HarvestCrop>(out var harvestCrop)) return;

        UpdateCropSprite(tile, crop, sr);
        UpdateFertilizerSprites(tile, harvestCrop);
    }

    // Chooses correct sprite for crop stage or dead state.
    void UpdateCropSprite(CropTile tile, CropSO cropSO, SpriteRenderer sr) {
        if (tile.IsDead() && tile.GetCropStage(CropDatabase) != CropStage.Seeded) {
            var idx = Mathf.Max(0, (int)tile.GetCropStage(CropDatabase) - 1);
            if (idx < cropSO.DeadSpritesGrowthStages.Count) {
                sr.sprite = cropSO.DeadSpritesGrowthStages[idx];
            }
        } else {
            int stageIdx = Mathf.Clamp((int)tile.GetCropStage(CropDatabase) - 1, 0, cropSO.SpritesGrowthStages.Count - 1);
            sr.sprite = cropSO.SpritesGrowthStages[stageIdx];
        }
    }

    // Shows fertilizer visuals based on tile scalers and fertilizer sets.
    void UpdateFertilizerSprites(CropTile tile, HarvestCrop harvestCrop) {
        var fertilizerMappings = new Dictionary<FertilizerTypes, List<FertilizerSO>> {
            { FertilizerTypes.GrowthTime, _growthFertilizer },
            { FertilizerTypes.RegrowthTime, _regrowthFertilizer },
            { FertilizerTypes.Quality, _qualityFertilizer },
            { FertilizerTypes.Quantity, _quantityFertilizer },
            { FertilizerTypes.Water, _waterFertilizer }
        };

        foreach (var mapping in fertilizerMappings) {
            float scaler = mapping.Key switch {
                FertilizerTypes.GrowthTime => tile.GrowthTimeScaler,
                FertilizerTypes.RegrowthTime => tile.RegrowthTimeScaler,
                FertilizerTypes.Quality => tile.QualityScaler,
                FertilizerTypes.Quantity => tile.QuantityScaler,
                FertilizerTypes.Water => tile.WaterScaler,
                _ => 1f
            };

            // Show fertilizer sprite if bonus applied.
            bool showFertilizer = mapping.Key == FertilizerTypes.Water ? scaler > 0f : scaler > 1f;
            if (!showFertilizer) continue;

            var list = mapping.Value;
            int bonusValue = (mapping.Key == FertilizerTypes.Water)
                ? Mathf.RoundToInt(scaler)
                : Mathf.RoundToInt((scaler - 1f) * 100);

            var fert = list.Find(f => f.FertilizerBonusValue == bonusValue);
            if (fert != null) {
                harvestCrop.SetFertilizerSprite(fert.FertilizerType, fert.FertilizerCropTileColor);
            }
        }
    }

    // Instantiates and spawns a crop prefab, assigns NetworkObjectId.
    void CreateCropPrefab(ref CropTile tile, bool isTree, int itemId) {
        if (!IsServer) return;
        var prefabInstance = Instantiate(isTree ? _cropsTreeSpritePrefab : _cropsSpritePrefab, transform);
        var harvestCrop = prefabInstance.GetComponent<HarvestCrop>();

        if (isTree && ItemManager.Instance.ItemDatabase[itemId] is SeedSO seedSOForTree) {
            var resourceNode = prefabInstance.GetComponent<ResourceNodeBase>();
            resourceNode.SetSeed(seedSOForTree);
        }

        Vector3 worldPos = _targetTilemap.GetCellCenterWorld(tile.CropPosition);
        prefabInstance.transform.position = worldPos + new Vector3(tile.SpriteRendererOffset.x, tile.SpriteRendererOffset.y + 0.5f, -0.1f);
        prefabInstance.transform.localScale = new Vector3(tile.SpriteRendererXScale, 1, 1);

        if (CropDatabase[tile.CropId].HasCollider) {
            prefabInstance.GetComponentInChildren<BoxCollider2D>().enabled = true;
        }

        harvestCrop.SetCropPosition(tile.CropPosition);

        if (!prefabInstance.TryGetComponent<NetworkObject>(out var netObj)) {
            netObj = prefabInstance.AddComponent<NetworkObject>();
        }

        netObj.Spawn();
        tile.PrefabNetworkObjectId = netObj.NetworkObjectId;
    }

    #endregion -------------------- Visualization --------------------


    #region -------------------- Plowing --------------------
    // Plow multiple tiles if possible.
    [ServerRpc(RequireOwnership = false)]
    public void PlowTilesServerRpc(Vector3IntSerializable[] positionsSerializable, int usedEnergy, ServerRpcParams serverRpcParams = default) {
        if (positionsSerializable == null || positionsSerializable.Length == 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        using var plowablePositions = new NativeList<Vector3Int>(Allocator.Temp);
        foreach (var posSer in positionsSerializable) {
            var pos = posSer.ToVector3Int();
            if (CanPlowTile(pos)) plowablePositions.Add(pos);
        }

        bool hasPlowable = plowablePositions.Length > 0;
        HandleClientCallback(serverRpcParams, hasPlowable);
        if (hasPlowable) HandleEnergyReduction(serverRpcParams, usedEnergy * plowablePositions.Length);

        for (int i = 0; i < plowablePositions.Length; i++) {
            var position = plowablePositions[i];
            CropTile tile = new() {
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
            CropTiles.Add(tile);
        }
    }

    bool CanPlowTile(Vector3Int position) {
        // Checks if tile is valid for plowing.
        var pObjData = _placeableObjectsManager.GetCropTileAtPosition(position);
        return !IsPositionPlowed(position) &&
               Array.Exists(_tilesThatCanBePlowed, t => t == _targetTilemap.GetTile(position)) &&
               !pObjData.HasValue;
    }

    void HandleEnergyReduction(ServerRpcParams serverRpcParams, int totalUsedEnergy) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject.TryGetComponent<PlayerHealthAndEnergyController>(out var ec)) {
                ec.AdjustEnergy(-totalUsedEnergy);
            }
        }
    }

    #endregion -------------------- Plowing --------------------

    #region -------------------- Seeding --------------------
    [ServerRpc(RequireOwnership = false)]
    public void SeedTileServerRpc(Vector3IntSerializable posSer, int itemId, int initialGrowthTimer = 0, ServerRpcParams serverRpcParams = default) {
        var pos = posSer.ToVector3Int();
        if (ItemManager.Instance.ItemDatabase[itemId] is not SeedSO seedSO) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        var cropToGrow = seedSO.CropToGrow;
        if (cropToGrow == null) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        bool isTree = cropToGrow.IsTree;
        bool canSeed = (isTree && CanPlowTile(pos)) || IsPositionPlowed(pos);
        bool canGrowInSeason = cropToGrow.SeasonsToGrow.Contains((TimeManager.SeasonName)TimeManager.Instance.CurrentDate.Value.Season);

        Collider2D[] colliders = Physics2D.OverlapPointAll(new Vector2(pos.x, pos.y));
        bool inGreenhouse = false;
        foreach (var collider in colliders) {
            if (collider.TryGetComponent<Greenhouse>(out _)) {
                inGreenhouse = true;
                break;
            }
        }


        if (!canSeed || IsPositionSeeded(pos) || (!canGrowInSeason && !inGreenhouse)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        var cropTileOpt = GetCropTileAtPosition(pos);
        if (!cropTileOpt.HasValue && !isTree) {
            // For normal crops, we must have a CropTile (plowed) at position.
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        if (isTree && !cropTileOpt.HasValue) {
            // If tree is not on a plowed tile, create a new tile for it
            CropTile treeTile = new() {
                CropId = cropToGrow.CropID,
                CropPosition = pos,
                IsWatered = _targetTilemap.GetTile<RuleTile>(pos) == _dirtWet,
                CurrentGrowthTimer = initialGrowthTimer,
                IsRegrowing = false,
                Damage = 0,
                SpriteRendererOffset = GenerateRandomSpriteRendererPosition(),
                SpriteRendererXScale = UnityEngine.Random.value < 0.5f ? 1 : -1,
                InGreenhouse = false,
                IsStruckByLightning = false,
                SeedItemId = itemId,
                GrowthTimeScaler = 1f,
                RegrowthTimeScaler = 1f,
                QualityScaler = 1f,
                QuantityScaler = 1f,
                WaterScaler = 0f
            };
            CropTiles.Add(treeTile);
            Debug.Log($"Tree planted at {pos} with GrowthTimer {initialGrowthTimer}");
            cropTileOpt = treeTile;
        }

        HandleItemReduction(serverRpcParams, itemId);
        HandleClientCallback(serverRpcParams, true);

        var changedTile = cropTileOpt.Value;
        changedTile.CropId = cropToGrow.CropID;
        changedTile.SeedItemId = itemId;
        changedTile.CurrentGrowthTimer = initialGrowthTimer;

        CreateCropPrefab(ref changedTile, cropToGrow.IsTree, itemId);        
        int idx = FindCropTileIndexAtPosition(pos);
        if (idx >= 0) CropTiles[idx] = changedTile;
    }

    void HandleItemReduction(ServerRpcParams serverRpcParams, int itemId) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) {
            if (client.PlayerObject.TryGetComponent<PlayerInventoryController>(out var invC)) {
                invC.InventoryContainer.RemoveItem(new ItemSlot(itemId, 1, 0));
            }
        }
    }

    Vector3 GenerateRandomSpriteRendererPosition() {
        return new Vector3(
            UnityEngine.Random.Range(-_cropRenderPositionSpread, _cropRenderPositionSpread),
            UnityEngine.Random.Range(-_cropRenderPositionSpread, _cropRenderPositionSpread),
            0f
        );
    }

    #endregion -------------------- Seeding --------------------

    #region -------------------- Fertilizing --------------------
    [ServerRpc(RequireOwnership = false)]
    public void FertilizeTileServerRpc(Vector3IntSerializable posSer, int itemId, ServerRpcParams serverRpcParams = default) {
        var position = posSer.ToVector3Int();
        var tileOpt = GetCropTileAtPosition(position);
        if (!tileOpt.HasValue) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        var tile = tileOpt.Value;
        if (tile.CropId < 0 || !IsPositionPlowed(position) || !IsPositionSeeded(position)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        if (!CanPositionBeFertilized(position, itemId)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleItemReduction(serverRpcParams, itemId);
        HandleClientCallback(serverRpcParams, true);

        int idx = FindCropTileIndexAtPosition(position);
        if (idx < 0) return;

        var fertSO = ItemManager.Instance.ItemDatabase[itemId] as FertilizerSO;
        if (fertSO == null) {
            Debug.LogError($"FertilizerSO not found for itemId {itemId}");
            return;
        }

        SetFertilizerValue(ref tile, fertSO);
        CropTiles[idx] = tile;
    }

    void SetFertilizerValue(ref CropTile tile, FertilizerSO fertSO) {
        switch (fertSO.FertilizerType) {
            case FertilizerTypes.GrowthTime:
                tile.GrowthTimeScaler = (fertSO.FertilizerBonusValue / 100f) + 1f;
                break;
            case FertilizerTypes.RegrowthTime:
                tile.RegrowthTimeScaler = (fertSO.FertilizerBonusValue / 100f) + 1f;
                break;
            case FertilizerTypes.Quality:
                tile.QualityScaler = fertSO.FertilizerBonusValue;
                break;
            case FertilizerTypes.Quantity:
                tile.QuantityScaler = (fertSO.FertilizerBonusValue / 100f) + 1f;
                break;
            case FertilizerTypes.Water:
                tile.WaterScaler = fertSO.FertilizerBonusValue;
                break;
            default:
                Debug.LogWarning("Unsupported Fertilizer Type.");
                break;
        }
    }

    #endregion -------------------- Fertilizing --------------------

    #region -------------------- Watering --------------------
    [ServerRpc(RequireOwnership = false)]
    public void WaterCropTileServerRpc(Vector3IntSerializable[] positions, int usedEnergy, ServerRpcParams serverRpcParams = default) {
        if (positions == null || positions.Length == 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleEnergyReduction(serverRpcParams, usedEnergy * positions.Length);
        HandleClientCallback(serverRpcParams, true);

        foreach (var posSer in positions) {
            var pos = posSer.ToVector3Int();
            if (IsPositionPlowed(pos)) {
                int idx = FindCropTileIndexAtPosition(pos);
                if (idx >= 0) {
                    var tile = CropTiles[idx];
                    tile.IsWatered = true;
                    CropTiles[idx] = tile;
                }
            } else if (_targetTilemap.GetTile(pos) == _dirtDry) {
                _targetTilemap.SetTile(pos, _dirtWet);
            }
        }
    }

    void OnChangeRainIntensity(int intensity) {
        // If raining, water all. If not, dry all.
        if (intensity == 0) DryAllCropTiles();
        else WaterAllCropTiles();
    }

    void WaterAllCropTiles() {
        for (int i = 0; i < CropTiles.Count; i++) {
            var tile = CropTiles[i];
            tile.IsWatered = true;
            CropTiles[i] = tile;
        }

        // Refresh tilemap visually.
        foreach (var pos in _targetTilemap.cellBounds.allPositionsWithin) {
            if (_targetTilemap.HasTile(pos) && _targetTilemap.GetTile(pos) == _dirtDry) {
                _targetTilemap.SetTile(pos, _dirtWet);
            }
        }
    }

    void DryAllCropTiles() {
        for (int i = 0; i < CropTiles.Count; i++) {
            var tile = CropTiles[i];
            tile.IsWatered = false;
            CropTiles[i] = tile;
        }

        foreach (var pos in _targetTilemap.cellBounds.allPositionsWithin) {
            if (_targetTilemap.HasTile(pos) && _targetTilemap.GetTile(pos) == _dirtWet) {
                Debug.Log($"Drying tile at {pos}");
                _targetTilemap.SetTile(pos, _dirtDry);
            }
        }
    }

    void CheckIfWateredAndApplyDamage() {
        for (int i = 0; i < CropTiles.Count; i++) {
            var tile = CropTiles[i];
            if (tile.CropId == -1 || CropDatabase[tile.CropId].IsTree) {
                tile.IsWatered = false;
                CropTiles[i] = tile;
                continue;
            }

            tile.Damage += tile.IsWatered ? -_notWateredDamage : _notWateredDamage;
            tile.Damage = Mathf.Clamp(tile.Damage, 0, _maxCropDamage);

            // Random chance of death based on damage.
            if (UnityEngine.Random.Range(0, _maxCropDamage) < tile.Damage) {
                tile.Damage = _maxCropDamage;
            }

            // Water fertilizer applied once, then reset.
            tile.IsWatered = UnityEngine.Random.Range(0, 100) < tile.WaterScaler;
            tile.WaterScaler = 0f;

            // Update visuals if needed.
            if (tile.PrefabNetworkObjectId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tile.PrefabNetworkObjectId, out var netObj)) {
                if (netObj.TryGetComponent<HarvestCrop>(out var hc)) {
                    hc.SetFertilizerSprite(FertilizerTypes.Water);
                }
            }

            CropTiles[i] = tile;
        }
    }

    #endregion -------------------- Watering --------------------

    #region -------------------- Harvest --------------------
    [ServerRpc(RequireOwnership = false)]
    public void HarvestCropServerRpc(Vector3IntSerializable posSer, ServerRpcParams serverRpcParams = default) {
        var pos = posSer.ToVector3Int();
        var tileOpt = GetCropTileAtPosition(pos);
        if (!tileOpt.HasValue) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        var tile = tileOpt.Value;
        if (!CanHarvestCrop(pos, tile)) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        HandleClientCallback(serverRpcParams, true);

        int count = CalculateItemCount(tile);
        int rarity = CalculateItemRarity(tile.QualityScaler);
        var cropSO = CropDatabase[tile.CropId];

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            new ItemSlot(cropSO.ItemToGrowAndSpawn.ItemId, count, rarity),
            _targetTilemap.CellToWorld(pos),
            Vector2.zero,
            spreadType: ItemSpawnManager.SpreadType.Circle
        );

        HandleCropAfterHarvest(pos);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HarvestTreeServerRpc(Vector3IntSerializable posSer, ServerRpcParams serverRpcParams = default) {
        var pos = posSer.ToVector3Int();
        int idx = FindCropTileIndexAtPosition(pos);
        if (idx < 0) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        var tile = CropTiles[idx];
        int count = CalculateItemCount(tile);
        int rarity = CalculateItemRarity(tile.QualityScaler);

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            new ItemSlot(tile.CropId, count, rarity),
            _targetTilemap.CellToWorld(pos),
            Vector2.zero,
            spreadType: ItemSpawnManager.SpreadType.Circle
        );

        HandleCropAfterHarvest(pos);
        HandleClientCallback(serverRpcParams, true);
    }

    bool CanHarvestCrop(Vector3Int pos, CropTile tile) {
        if (!IsPositionSeeded(pos) || !tile.IsCropHarvestable(CropDatabase) || CropDatabase[tile.CropId].IsTree) {
            return false;
        }
        return true;
    }

    public int CalculateItemCount(CropTile tile) {
        var crop = CropDatabase[tile.CropId];
        int minItems = Mathf.RoundToInt(crop.MinItemAmountToSpawn * tile.QuantityScaler);
        int maxItems = Mathf.RoundToInt(crop.MaxItemAmountToSpawn * tile.QuantityScaler);
        return UnityEngine.Random.Range(minItems, maxItems + 1);
    }

    int CalculateItemRarity(float rarityBonus) {
        int roll = UnityEngine.Random.Range(0, 100);
        int cumulative = 0;
        // Rarety is reversed in original code. Keep logic consistent.
        for (int i = _probabilityToSpawnRarity.Length - 1; i >= 0; i--) {
            cumulative += _probabilityToSpawnRarity[i];
            if (roll < cumulative + Mathf.RoundToInt(rarityBonus)) {
                return i;
            }
        }
        return _probabilityToSpawnRarity.Length - 1;
    }

    void HandleCropAfterHarvest(Vector3Int position) {
        if (!IsServer) return;
        int idx = FindCropTileIndexAtPosition(position);
        if (idx < 0) return;

        var tile = CropTiles[idx];
        var crop = CropDatabase[tile.CropId];
        if (crop.CanRegrow) {
            tile.IsRegrowing = true;
            tile.CurrentGrowthTimer -= crop.DaysToRegrow;
            CropTiles[idx] = tile;
        } else {
            DespawnCrop(tile);
            CropTiles.RemoveAt(idx);
        }
    }

    #endregion -------------------- Harvest --------------------

    #region -------------------- Thunder Strike --------------------
    void OnThunderStrike() {
        if (UnityEngine.Random.value >= PROBABILITY_OF_THUNDER_STRIKE) return;

        // Find grown trees.
        List<CropTile> treeTiles = new();
        foreach (var t in CropTiles) {
            var crop = CropDatabase[t.CropId];
            if (crop != null && crop.IsTree && t.IsCropDoneGrowing(CropDatabase)) {
                treeTiles.Add(t);
            }
        }
        if (treeTiles.Count == 0) return;

        int selected = UnityEngine.Random.Range(0, treeTiles.Count);
        var selectedTile = treeTiles[selected];

        selectedTile.IsStruckByLightning = true;
        int index = FindCropTileIndexAtPosition(selectedTile.CropPosition);
        if (index >= 0) CropTiles[index] = selectedTile;

        if (selectedTile.IsCropHarvestable(CropDatabase)) {
            ItemSpawnManager.Instance.SpawnItemServerRpc(
                new ItemSlot(_coal.ItemId, CalculateItemCount(selectedTile), 0),
                new Vector2(selectedTile.CropPosition.x, selectedTile.CropPosition.y) + selectedTile.SpriteRendererOffset,
                Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle
            );
            HandleCropAfterHarvest(selectedTile.CropPosition);
        }
    }

    #endregion -------------------- Thunder Strike --------------------

    #region -------------------- Destroy Crop --------------------
    [ServerRpc(RequireOwnership = false)]
    public void DestroyCropTileServerRpc(Vector3IntSerializable posSer, int usedEnergy, ToolSO.ToolTypes toolType, ServerRpcParams serverRpcParams = default) {
        var pos = posSer.ToVector3Int();
        var tileOpt = GetCropTileAtPosition(pos);
        if (!tileOpt.HasValue) {
            HandleClientCallback(serverRpcParams, false);
            return;
        }

        bool success = HandleToolAction(pos, toolType);
        HandleClientCallback(serverRpcParams, success);
        HandleEnergyReduction(serverRpcParams, usedEnergy);
    }

    bool HandleToolAction(Vector3Int pos, ToolSO.ToolTypes toolType) {
        return toolType switch {
            ToolSO.ToolTypes.Scythe => TryHandleCropWithScythe(pos),
            ToolSO.ToolTypes.Pickaxe => TryHandleCropWithPickaxe(pos),
            _ => false,
        };
    }

    bool TryHandleCropWithScythe(Vector3Int pos) {
        if (!IsPositionSeeded(pos)) return false;

        int idx = FindCropTileIndexAtPosition(pos);
        if (idx < 0) return false;

        var tile = CropTiles[idx];
        var crop = CropDatabase[tile.CropId];
        if (tile.IsCropHarvestable(CropDatabase) && !crop.IsTree) {
            HarvestCropServerRpc(new Vector3IntSerializable(pos));
        }

        // If it's harvested by scythe and can regrow, leave it.
        if (crop.IsHarvestedByScythe && crop.CanRegrow) return true;

        DespawnCrop(tile);
        CropTiles.RemoveAt(idx);
        return true;
    }

    bool TryHandleCropWithPickaxe(Vector3Int pos) {
        if (!IsPositionPlowed(pos)) return false;
        int idx = FindCropTileIndexAtPosition(pos);
        if (idx < 0) return false;

        var tile = CropTiles[idx];
        if (tile.CropId >= 0) {
            tile.CropId = -1;
            CropTiles[idx] = tile;
            DespawnCrop(tile);
            return true;
        } else {
            DespawnCrop(tile);
            CropTiles.RemoveAt(idx);
            return true;
        }
    }

    void DespawnCrop(CropTile tile) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tile.PrefabNetworkObjectId, out var netObj)) {
            netObj.Despawn();
        }
    }

    void RandomlyDeleteSomeUnseededTiles() {
        foreach (var t in CropTiles) {
            if (t.CropId == -1 && UnityEngine.Random.Range(0, 100) < _probabilityToDeleteUnseededTile) {
                int idx = FindCropTileIndexAtPosition(t.CropPosition);
                if (idx < 0) continue;
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(t.PrefabNetworkObjectId, out var netObj)) {
                    netObj.Despawn();
                }
                CropTiles.RemoveAt(idx);
            }
        }
    }

    public void DestroyTree(Vector3Int pos) {
        int idx = FindCropTileIndexAtPosition(pos);
        if (idx < 0) return;
        var tile = CropTiles[idx];
        if (IsPositionPlowed(pos)) {
            tile.CropId = -1;
            CropTiles[idx] = tile;
            DespawnCrop(tile);
            return;
        } else {
            DespawnCrop(tile);
            CropTiles.RemoveAt(idx);
            return;
        }
    }

    #endregion -------------------- Destroy Crop --------------------

    #region -------------------- Crow Attack (Debug) --------------------

    // TODO
    private void Update() {
        // Debugging only
        // Debug.Log("Press F12 to check protection and trigger crow attack");
        if (Input.GetKeyDown(KeyCode.F12)) {
            Debug.Log("CheckProtectionAndTriggerCrowAttack");
            CheckProtectionAndTriggerCrowAttack();
        }
    }

    void CheckProtectionAndTriggerCrowAttack() {
        List<CropTile> unprotected = new();
        foreach (var tile in CropTiles) {
            if (tile.CropId < 0) continue;
            if (tile.GetCropStage(CropDatabase) != CropStage.Seeded && !tile.IsCropHarvestable(CropDatabase)) continue;
            if (IsProtected(tile.CropPosition)) continue;
            unprotected.Add(tile);
        }

        if (unprotected.Count == 0) return;

        var tilesToAttack = GetRandomCropTiles(unprotected, 3);
        foreach (var t in tilesToAttack) {
            TriggerCrowAttack(t);
        }
    }

    bool IsProtected(Vector3Int pos) {
        for (int i = 0; i < _scarecrowRadii.Length; i++) {
            if (HasScarecrowInRange(pos, _scarecrowRadii[i], _scarecrowTypes[i])) return true;
        }
        return false;
    }

    bool HasScarecrowInRange(Vector3Int position, float radius, ScarecrowType type) {
        float adjustedRadius = radius - 0.5f;
        float radiusSq = adjustedRadius * adjustedRadius;
        var centerWorldPos = _targetTilemap.CellToWorld(position) + _targetTilemap.tileAnchor;

        int minX = Mathf.FloorToInt(centerWorldPos.x - adjustedRadius);
        int maxX = Mathf.CeilToInt(centerWorldPos.x + adjustedRadius);
        int minY = Mathf.FloorToInt(centerWorldPos.y - adjustedRadius);
        int maxY = Mathf.CeilToInt(centerWorldPos.y + adjustedRadius);

        for (int x = minX; x <= maxX; x++) {
            for (int y = minY; y <= maxY; y++) {
                var checkPos = new Vector3Int(x, y, position.z);
                Vector3 tileWorldPos = _targetTilemap.CellToWorld(checkPos) + _targetTilemap.tileAnchor;
                float dx = tileWorldPos.x - centerWorldPos.x;
                float dy = tileWorldPos.y - centerWorldPos.y;
                float distSq = dx * dx + dy * dy;

                if (distSq <= radiusSq) {
                    var pObjData = _placeableObjectsManager.GetCropTileAtPosition(checkPos);
                    if (!pObjData.HasValue) continue;

                    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pObjData.Value.PrefabNetworkObjectId, out var netObj)) {
                        if (netObj.TryGetComponent<Scarecrow>(out var scarecrow) && scarecrow.ScarecrowSO.Type == type) {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    List<CropTile> GetRandomCropTiles(List<CropTile> tiles, int count) {
        if (tiles.Count <= count) return new List<CropTile>(tiles);

        List<CropTile> selected = new();
        HashSet<int> chosenIndices = new();
        for (int i = 0; i < count; i++) {
            int idx;
            do { idx = UnityEngine.Random.Range(0, tiles.Count); } while (chosenIndices.Contains(idx));
            chosenIndices.Add(idx);
            selected.Add(tiles[idx]);
        }
        return selected;
    }

    void TriggerCrowAttack(CropTile tile) {
        DestroyCropTileServerRpc(new Vector3IntSerializable(tile.CropPosition), 0, ToolSO.ToolTypes.Pickaxe);
        // TODO: Add crow animation, etc.
    }

    #endregion -------------------- Crow Attack --------------------

    #region -------------------- Save & Load --------------------

    public void SaveData(GameData data) {
        if (_saveCrops) {
            data.CropsOnMap = JsonConvert.SerializeObject(CropTiles);
        }
    }

    public void LoadData(GameData data) {
        if (string.IsNullOrEmpty(data.CropsOnMap) || !_loadCrops) return;
        var loadedList = JsonConvert.DeserializeObject<List<CropTile>>(data.CropsOnMap);
        CropTiles.Clear();

        for (int i = 0; i < loadedList.Count; i++) {
            var tile = loadedList[i];

            // Re-visualize crops if prefab exists.
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tile.PrefabNetworkObjectId, out var netObj)) {
                VisualizeCropChanges(tile, netObj);
            }

            CropTiles.Add(tile);
            CreateCropPrefab(ref tile, CropDatabase[tile.CropId].IsTree, tile.SeedItemId);
        }
    }

    #endregion -------------------- Save & Load --------------------

    #region -------------------- Utility --------------------
    public CropTile? GetCropTileAtPosition(Vector3Int pos) {
        var lookPos = new Vector2Int(pos.x, pos.y);
        for (int i = 0; i < CropTiles.Count; i++) {
            var cropPos = new Vector2Int(CropTiles[i].CropPosition.x, CropTiles[i].CropPosition.y);
            if (cropPos.Equals(lookPos)) return CropTiles[i];
        }
        return null;
    }

    public bool IsPositionPlowed(Vector3Int pos) {
        if (!_targetTilemap.HasTile(pos)) return false;
        var tile = _targetTilemap.GetTile(pos);
        return tile == _dirtPlowedDry || tile == _dirtPlowedWet;
    }

    public bool IsPositionSeeded(Vector3Int pos) {
        for (int i = 0; i < CropTiles.Count; i++) {
            if (CropTiles[i].CropPosition.Equals(pos) && CropTiles[i].CropId >= 0) return true;
        }
        return false;
    }

    public bool CanPositionBeFertilized(Vector3Int pos, int itemId) {
        var tileOpt = GetCropTileAtPosition(pos);
        if (!tileOpt.HasValue) return false;

        var tile = tileOpt.Value;
        if (ItemManager.Instance.ItemDatabase[itemId] is not FertilizerSO fert) return false;

        return fert.FertilizerType switch {
            FertilizerTypes.GrowthTime => tile.GrowthTimeScaler < ((fert.FertilizerBonusValue / 100f) + 1f),
            FertilizerTypes.RegrowthTime => tile.RegrowthTimeScaler < ((fert.FertilizerBonusValue / 100f) + 1f) && tile.IsRegrowing,
            FertilizerTypes.Quality => tile.QualityScaler < fert.FertilizerBonusValue,
            FertilizerTypes.Quantity => tile.QuantityScaler < ((fert.FertilizerBonusValue / 100f) + 1f),
            FertilizerTypes.Water => tile.WaterScaler < (fert.FertilizerBonusValue / 100f),
            _ => false,
        };
    }

    int FindCropTileIndexAtPosition(Vector3Int pos) {
        for (int i = 0; i < CropTiles.Count; i++) {
            if (CropTiles[i].CropPosition.Equals(pos)) return i;
        }
        return -1;
    }

    void HandleClientCallback(ServerRpcParams serverRpcParams, bool success) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        if (!NetworkManager.ConnectedClients.ContainsKey(clientId)) return;

        var client = NetworkManager.ConnectedClients[clientId];
        if (client.PlayerObject.TryGetComponent<PlayerToolsAndWeaponController>(out var ptw)) {
            ptw.ClientCallbackClientRpc(success);
        }
    }

    #endregion -------------------- Utility --------------------
}
