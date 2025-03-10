using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ore resource node logic: similar to trees but simpler.
/// </summary>
public class OreResourceNode : ResourceNodeBase {
    [SerializeField] ItemSpawnManager.SpreadType _spreadType;

    public override bool CanHitResourceNodeType(HashSet<ResourceNodeType> canBeHit) => canBeHit.Contains(ResourceNodeType.Ore);

    public override void SetSeed(SeedSO seed) { }

    protected override void PerformTypeSpecificNextDayActions() { }

    protected override void PlaySound(bool isDestroyed) {
        if (isDestroyed) {
            _audioManager.PlayOneShot(_fmodEvents.Pickaxe_Breake_Rock, transform.position);
        } else {
            _audioManager.PlayOneShot(_fmodEvents.Pickaxe_Hit_Rock, transform.position);
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

        if (_networkCurrentHp.Value <= 0) {
            HandleNodeDestruction();
        }

        HandleClientCallback(rpcParams, true);
    }

    protected override void HandleNodeDestruction() {
        SpawnDroppedItems();
        DestroyNodeAcrossNetwork();
    }

    void SpawnDroppedItems() {
        int dropCount = Random.Range(_minDropCount, _maxDropCount + 1);
        var spawnPos = new Vector2(transform.position.x + _boxCollider2D.offset.x,
                                   transform.position.y + _boxCollider2D.offset.y);

        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            new ItemSlot(_itemSO.ItemId, dropCount, _rarityID), 
            spawnPos,
            _playerMovementController.LastMotionDirection, 
            spreadType: _spreadType);
    }
}
