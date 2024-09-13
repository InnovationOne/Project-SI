using System.Collections.Generic;
using UnityEngine;

// This script defines a resource and how it can be hit
[CreateAssetMenu(menuName = "Tool Action/Gather Resource Node")]
public class GatherResourceNodeSO : ToolActionSO {
    public enum ResourceNodeType {
        Tree,
        Ore,
    }

    [Header("Nodes the tool can hit")]
    [SerializeField] private List<ResourceNodeType> canHitNodesOfType;

    public override void OnApplyToTileMap(Vector3Int gridPosition, ItemSlot itemSlot) {
        // Define the area of the tile
        Vector2 bottomLeft = new Vector2(gridPosition.x + 0.1f, gridPosition.y + 0.1f);
        Vector2 topRight = new Vector2(gridPosition.x + 0.9f, gridPosition.y + 0.9f);

        // Get all colliders overlapping the tile area
        Collider2D[] colliders = Physics2D.OverlapAreaAll(bottomLeft, topRight);

        foreach (Collider2D collider in colliders) {
            ResourceNode resourceNode = collider.gameObject.GetComponent<ResourceNode>();
            if (resourceNode != null && resourceNode.CanHitResourceNodeType(canHitNodesOfType)) {
                // Apply damage or usage to the resource node
                ToolSO tool = ItemManager.Instance.ItemDatabase[itemSlot.ItemId] as ToolSO;
                int damage = tool.UsageOrDamageOnAction[itemSlot.RarityId - 1];
                resourceNode.HitResourceNode(damage);
                break; // Exit after hitting the first valid resource node
            }
        }
    }
}
