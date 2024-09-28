using System.Collections.Generic;
using UnityEngine;

// This script defines a resource and how it can be hit
[CreateAssetMenu(menuName = "Tool Action/Gather Resource Node")]
public class GatherResourceNodeSO : ToolActionSO {

    [Header("Nodes the tool can hit")]
    [SerializeField] private HashSet<ResourceNodeType> _canHitNodesOfType = new HashSet<ResourceNodeType>();

    [Header("Physics Settings")]
    [SerializeField] private LayerMask _resourceNodeLayerMask; // Assign in Inspector to filter relevant colliders

    // Reusable Vector2 to minimize memory allocations
    private Vector2 _bottomLeft = Vector2.zero;
    private Vector2 _topRight = Vector2.zero;

    public override void OnApplyToTileMap(Vector3Int gridPosition, ItemSlot itemSlot) {
        // Define the area of the tile with minimal memory allocations
        _bottomLeft.Set(gridPosition.x + 0.1f, gridPosition.y + 0.1f);
        _topRight.Set(gridPosition.x + 0.9f, gridPosition.y + 0.9f);

        // Get all colliders overlapping the tile area with specified LayerMask
        Collider2D[] colliders = Physics2D.OverlapAreaAll(_bottomLeft, _topRight, _resourceNodeLayerMask);

        if (colliders.Length == 0) {
            Debug.LogWarning("No colliders found in the specified tile area.");
            return; // Early exit if no colliders found
        }

        // Access the tool from the ItemDatabase using ItemId
        var tool = ItemManager.Instance.ItemDatabase[itemSlot.ItemId] as AxePickaxeToolSO;
        if (tool == null) {
            Debug.LogError($"ItemDatabase does not contain an item with ItemId: {itemSlot.ItemId}");
            return; // Early exit if tool not found or incorrect type
        }

        // Ensure rarity index is within bounds
        int rarityIndex = itemSlot.RarityId - 1;
        if (rarityIndex < 0 || rarityIndex >= tool.DamageOnAction.Length) {
            Debug.LogError($"Tool with ItemId {itemSlot.ItemId} is not of type AxePickaxeToolSO or is null.");
            return; // Early exit if rarityId is invalid
        }

        int damage = tool.DamageOnAction[rarityIndex];

        foreach (Collider2D collider in colliders) {
            if (collider.TryGetComponent<ResourceNode>(out var resourceNode)) {
                if (resourceNode.CanHitResourceNodeType(_canHitNodesOfType)) {
                    // Apply damage or usage to the resource node via server RPC
                    resourceNode.HitResourceNodeServerRpc(damage);
                    break; // Exit after hitting the first valid resource node
                } else {
                    Debug.LogWarning($"ResourceNode of type {resourceNode.name} cannot be hit with the current tool.");
                }
            } else {
                Debug.LogWarning($"Collider on GameObject: {collider.gameObject.name} does not have a ResourceNode component.");
            }
        }
    }
}
