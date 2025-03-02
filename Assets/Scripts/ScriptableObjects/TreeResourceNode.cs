using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tree resource node logic (harvesting, seeding, bee nests).
/// </summary>
public class TreeResourceNode : ResourceNodeBase, IInteractable {
    // Serialized Fields
    [SerializeField] ItemSpawnManager.SpreadType _spreadType;
    [SerializeField] ItemSO _beeNest;
    [SerializeField] SeedSO _seedToDrop;
    public SeedSO SeedToDrop => _seedToDrop;
    [SerializeField] ItemSO _coal;

    // Constants
    const float PROBABILITY_TO_SEED = 0.1f;
    const float PROBABILITY_TO_SEED_AFTER_SHAKE = 0.02f;
    const float BEE_NEST_PROBABILITY = 0.05f;

    public float MaxDistanceToPlayer => 1f;
    public bool CircleInteract => true;

    private bool _doneGrowing {
        get {
            var pos = Vector3Int.FloorToInt(transform.position);
            var cropTileOpt = _cropsManager.GetCropTileAtPosition(pos);
            if (!cropTileOpt.HasValue) return false;
            var cropTile = cropTileOpt.Value;
            return cropTile.IsCropDoneGrowing(_cropsManager.CropDatabase);
        }
    }

    public override bool CanHitResourceNodeType(HashSet<ResourceNodeType> canBeHit) => canBeHit.Contains(ResourceNodeType.Tree);

    public override void SetSeed(SeedSO seed) => _seedToDrop = seed;

    protected override void PerformTypeSpecificNextDayActions() => AttemptSeedSpawn();

    protected override void PlaySound(bool isDestroyed) {
        if (isDestroyed) {
            _audioManager.PlayOneShot(_fmodEvents.Axe_Breake_Wood, transform.position);
        } else {
            _audioManager.PlayOneShot(_fmodEvents.Axe_Hit_Wood, transform.position);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void HitResourceNodeServerRpc(int damage, ServerRpcParams rpcParams = default) {
        var selectedTool = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();
        if (selectedTool.RarityId < _minimumToolRarity) {
            _audioManager.PlayOneShot(_fmodEvents.Hit_Unhittable_Object, transform.position);
            HandleClientCallback(rpcParams, false);
            return;
        }

        ApplyDamage(damage);
        if (_networkCurrentHp.Value > 0) {
            HandleCropTileOnHit();
        }

        if (_networkCurrentHp.Value <= 0) {
            HandleNodeDestruction();
        }

        HandleClientCallback(rpcParams, true);
    }

    void AttemptSeedSpawn() {        
        if (_doneGrowing && Random.value < PROBABILITY_TO_SEED) {
            var seedItemSlot = new ItemSlot(_seedToDrop.ItemId, 1, 0);
            var spawnPosition = GetRandomAdjacentPosition(Vector3Int.FloorToInt(transform.position));
            GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                seedItemSlot, 
                spawnPosition, 
                Vector2.zero, 
                spreadType: ItemSpawnManager.SpreadType.Circle);
        }
    }

    void HandleCropTileOnHit() {
        // If done growing, we can attempt harvesting or special drops.
        if (_doneGrowing) {
            Interact(null);
            return;
        }

        if (!_networkHitShookToday.Value && Random.value < BEE_NEST_PROBABILITY) {
            SpawnBeeNest();
            _networkHitShookToday.Value = true;
        }
    }

    void SpawnBeeNest() {
        var beeNestSlot = new ItemSlot(_beeNest.ItemId, 1, 0);
        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            beeNestSlot, 
            transform.position, 
            Vector2.zero, 
            spreadType: ItemSpawnManager.SpreadType.Circle);

        // TODO: Bee attack logic here.
    }

    protected override void HandleNodeDestruction() {
        SpawnDroppedItems();
        DestroyNodeAcrossNetwork();
        _cropsManager.DestroyTree(Vector3Int.FloorToInt(transform.position));
    }

    void SpawnDroppedItems() {
        int dropCount = Random.Range(_minDropCount, _maxDropCount + 1);
        var spawnPos = new Vector2(transform.position.x + _boxCollider2D.offset.x,
                                   transform.position.y + _boxCollider2D.offset.y);

        var itemSlot = new ItemSlot(_itemSO.ItemId, dropCount, _rarityID);
        var motionDirection = _playerMovementController.LastMotionDirection;

        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            itemSlot, 
            spawnPos, 
            motionDirection, 
            spreadType: _spreadType);
    }

    public void Interact(PlayerController player) {
        if (_networkHitShookToday.Value) return;
        var pos = Vector3Int.FloorToInt(transform.position);
        var cropTileOpt = _cropsManager.GetCropTileAtPosition(pos);
        if (!cropTileOpt.HasValue) {
            Debug.LogError("No cropTile found.");
            return;
        }

        var cropTile = cropTileOpt.Value;
        var db = _cropsManager.CropDatabase;

        if (cropTile.IsCropDoneGrowing(db)) {
            if (cropTile.IsCropHarvestable(db)) {
                Harvest(cropTile);
            } else if (cropTile.IsStruckByLightning) {
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    new ItemSlot(_coal.ItemId, GameManager.Instance.CropsManager.CalculateItemCount(cropTile), 0),
                    new Vector2(cropTile.CropPosition.x, cropTile.CropPosition.y) + cropTile.SpriteRendererOffset,
                    Vector2.zero,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            } else if (Random.value < PROBABILITY_TO_SEED_AFTER_SHAKE) {
                var itemId = db[cropTile.CropId].ItemForSeeding.ItemId;
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    new ItemSlot(itemId, GameManager.Instance.CropsManager.CalculateItemCount(cropTile), 0),
                    new Vector2(cropTile.CropPosition.x, cropTile.CropPosition.y) + cropTile.SpriteRendererOffset,
                    Vector2.zero,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }

        _networkHitShookToday.Value = true;
    }

    void Harvest(CropTile cropTile) {
        var harvestPos = cropTile.CropPosition + Vector3Int.FloorToInt(cropTile.SpriteRendererOffset);
        GameManager.Instance.CropsManager.HarvestTreeServerRpc(new Vector3IntSerializable(harvestPos));
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }
    public void InitializePreLoad(int itemId) { }
}
