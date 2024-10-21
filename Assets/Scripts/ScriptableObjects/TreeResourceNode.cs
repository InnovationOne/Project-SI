using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a Tree resource node in the game, handling interactions, networking, and resource management specific to trees.
/// </summary>
public class TreeResourceNode : ResourceNodeBase, IInteractable {
    // Serialized Fields
    [SerializeField] private ItemSpawnManager.SpreadType _spreadType;
    [SerializeField] private ItemSO _beeNest;
    [SerializeField] private SeedSO _seedToDrop;
    [SerializeField] private ItemSO _coal;

    // Constants
    private const float PROBABILITY_TO_SEED = 0.1f;
    private const float PROBABILITY_TO_SEED_AFTER_SHAKE = 0.02f;
    private const float BEE_NEST_PROBABILITY = 0.05f;

    public float MaxDistanceToPlayer => 0f;

    public override bool CanHitResourceNodeType(HashSet<ResourceNodeType> canBeHit) {
        return canBeHit.Contains(ResourceNodeType.Tree);
    }

    public override void SetSeed(SeedSO seed) {
        _seedToDrop = seed;
    }

    protected override void PerformTypeSpecificNextDayActions() {
        AttemptSeedSpawn();
    }

    protected override void PlaySound() {
        switch (ResourceNodeType.Tree) {
            case ResourceNodeType.Tree:
                _audioManager.PlayOneShot(_fmodEvents.HitTreeSFX, transform.position);
                break;
            default:
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public override void HitResourceNodeServerRpc(int damage, ServerRpcParams rpcParams = default) {
        ItemSlot selectedTool = _playerToolbeltController.GetCurrentlySelectedToolbeltItemSlot();

        if (selectedTool.RarityId < _minimumToolRarity) {
            Debug.Log("Tool rarity too low.");
            // TODO: Implement bounce back animation & sound
            return;
        }

        Vector3Int pos = Vector3Int.FloorToInt(transform.position);
        CropTile? cropTileData = _cropsManager.GetCropTileAtPosition(pos);
        if (!cropTileData.HasValue) {
            Debug.Log("No cropTile found.");
            return;
        }

        CropTile cropTile = cropTileData.Value;

        if (!cropTile.IsCropDoneGrowing()) {
            return;
        }

        ApplyDamage(damage);

        if (_networkCurrentHp.Value > 0) {
            HandleCropTileOnHit(cropTile);
        }

        if (_networkCurrentHp.Value <= 0) {
            HandleNodeDestruction();
        }
    }

    /// <summary>
    /// Attempts to spawn a seed based on probability and crop state.
    /// </summary>
    private void AttemptSeedSpawn() {
        Vector3Int pos = Vector3Int.FloorToInt(transform.position);
        CropTile? cropTileData = _cropsManager.GetCropTileAtPosition(pos);
        if (!cropTileData.HasValue) {
            Debug.LogError("No cropTile found.");
        }

        CropTile cropTile = cropTileData.Value;

        if (cropTile.IsCropDoneGrowing() &&
            Random.value < PROBABILITY_TO_SEED) {
            ItemSlot seedItemSlot = new ItemSlot(_seedToDrop.ItemId, 1, 0);
            Vector3 spawnPosition = GetRandomAdjacentPosition(pos);

            ItemSpawnManager.Instance.SpawnItemServerRpc(
                itemSlot: seedItemSlot,
                initialPosition: spawnPosition,
                motionDirection: Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle
            );
        }
    }

    /// <summary>
    /// Handles interactions with the crop tile when the node is hit.
    /// Manages harvesting and potential bee nest spawning.
    /// </summary>
    /// <param name="cropTile">The crop tile being interacted with.</param>
    private void HandleCropTileOnHit(CropTile cropTile) {
        if (cropTile.IsCropDoneGrowing()) {
            Interact(null);
            return;
        }

        if (!_networkHitShookToday.Value && Random.value < BEE_NEST_PROBABILITY) {
            SpawnBeeNest();
            _networkHitShookToday.Value = true;
        }
    }

    /// <summary>
    /// Spawns a bee nest item at the resource node's position.
    /// </summary>
    private void SpawnBeeNest() {
        ItemSlot beeNestSlot = new ItemSlot(_beeNest.ItemId, 1, 0);
        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: beeNestSlot,
            initialPosition: transform.position,
            motionDirection: Vector2.zero,
            spreadType: ItemSpawnManager.SpreadType.Circle
        );

        // TODO: Implement bee attack on player
    }

    protected override void HandleNodeDestruction() {
        SpawnDroppedItems();
        DestroyNodeAcrossNetwork();
    }

    /// <summary>
    /// Spawns items dropped from the destroyed resource node.
    /// </summary>
    private void SpawnDroppedItems() {
        int dropCount = Random.Range(_minDropCount, _maxDropCount + 1);
        Vector2 spawnPosition = new Vector2(
            transform.position.x + _boxCollider2D.offset.x,
            transform.position.y + _boxCollider2D.offset.y
        );

        ItemSlot itemSlot = new ItemSlot(_itemSO.ItemId, dropCount, _rarityID);
        Vector2 motionDirection = _playerMovementController.LastMotionDirection;

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: itemSlot,
            initialPosition: spawnPosition,
            motionDirection: motionDirection,
            spreadType: _spreadType
        );
    }

    public void Interact(Player player) {
        if (_networkHitShookToday.Value) {
            return;
        }

        Vector3Int pos = Vector3Int.FloorToInt(transform.position);
        CropTile? cropTileData = _cropsManager.GetCropTileAtPosition(pos);
        if (!cropTileData.HasValue) {
            Debug.LogError("No cropTile found.");
        }

        CropTile cropTile = cropTileData.Value;

        if (cropTile.IsCropDoneGrowing()) {
            if (cropTile.IsCropHarvestable()) {
                Harvest(cropTile); // Harvest
            } else if (cropTile.IsStruckByLightning) {
                SpawnItem(cropTile, _coal.ItemId); // Spawn coal
            } else if (Random.value < PROBABILITY_TO_SEED_AFTER_SHAKE) {
                SpawnItem(cropTile, CropsManager.Instance.CropDatabase[cropTile.CropId].ItemForSeeding.ItemId); // Spawn seeds
            }
        }

        _networkHitShookToday.Value = true;
    }

    private void Harvest(CropTile cropTile) {
        CropsManager.Instance.HarvestTreeServerRpc(new Vector3IntSerializable(new Vector3Int(cropTile.CropPosition.x, cropTile.CropPosition.y) + Vector3Int.FloorToInt(cropTile.SpriteRendererOffset)));
    }

    private void SpawnItem(CropTile cropTile, int itemId) {
        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(itemId, CropsManager.Instance.CalculateItemCount(cropTile), 0),
            initialPosition: new Vector2(cropTile.CropPosition.x, cropTile.CropPosition.y) + cropTile.SpriteRendererOffset,
            motionDirection: Vector2.zero,
            spreadType: ItemSpawnManager.SpreadType.Circle);
    }

    public void PickUpItemsInPlacedObject(Player player) { }

    public void InitializePreLoad(int itemId) { }
}
