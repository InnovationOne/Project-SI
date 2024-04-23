using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// This script defines a resource and how it can be hit
public enum ResourceNodeType {
    Tree, 
    Ore,
}

[CreateAssetMenu(menuName = "Tool Action/Gather Resource Node")]
public class GatherResourceNodeSO : ToolActionSO {
    [Header("Nodes the tool can hit")]
    [SerializeField] private List<ResourceNodeType> canHitNodesOfType;

    public override void OnApplyToTileMap(Vector3Int gridPosition, ItemSlot itemSlot) {
        Collider2D collider2D = Physics2D.OverlapPoint(new Vector2(gridPosition.x + 0.5f, gridPosition.y + 0.5f));
        if (collider2D != null) {
            ResourceNode resourceNode = collider2D.GetComponent<ResourceNode>();
            
            if (resourceNode != null && resourceNode.CanHitResourceNodeType(canHitNodesOfType)) {
                resourceNode.HitResourceNode((itemSlot.Item as ToolSO).UsageOrDamageOnAction[itemSlot.RarityID - 1]);

                //return true;
            }
            
        }

        //return false;
    }
}
