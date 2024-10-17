using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents an Ore resource node in the game, handling interactions, networking, and resource management specific to ores.
/// </summary>
public class OreResourceNode : ResourceNodeBase {
    [SerializeField] private ItemSpawnManager.SpreadType _spreadType;

    public override bool CanHitResourceNodeType(HashSet<ResourceNodeType> canBeHit) {
        return canBeHit.Contains(ResourceNodeType.Ore);
        //return canBeHit?.Contains(ResourceNodeType.Ore) ?? false;
    }

    public override void SetSeed(SeedSO seed) { }

    protected override void PerformTypeSpecificNextDayActions() { }

    protected override void PlaySound() {
        switch (ResourceNodeType.Ore) {
            case ResourceNodeType.Ore:
                // TODO: Impliment soundeffect for hitting ore.
                // _audioManager.PlayOneShot(_fmodEvents.HitOreSFX, transform.position);
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

        ApplyDamage(damage);

        if (_networkCurrentHp.Value > 0) {
            // Implement ore-specific hit logic if any
        }

        if (_networkCurrentHp.Value <= 0) {
            HandleNodeDestruction();
        }
    }

    protected override void HandleNodeDestruction() {
        SpawnDroppedItems();
        DestroyNodeAcrossNetwork();
    }

    /// <summary>
    /// Spawns items dropped from the destroyed ore node.
    /// </summary>
    private void SpawnDroppedItems() {
        int dropCount = Random.Range(_minDropCount, _maxDropCount + 1);
        Vector2 spawnPosition = new(
            transform.position.x + _boxCollider2D.offset.x,
            transform.position.y + _boxCollider2D.offset.y
        );

        ItemSlot itemSlot = new(_itemSO.ItemId, dropCount, _rarityID);
        Vector2 motionDirection = _playerMovementController.LastMotionDirection;

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: itemSlot,
            initialPosition: spawnPosition,
            motionDirection: motionDirection,
            spreadType: _spreadType
        );
    }
}
