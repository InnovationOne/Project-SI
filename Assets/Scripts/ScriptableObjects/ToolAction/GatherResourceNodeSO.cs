using System.Collections.Generic;
using UnityEngine;

// This script defines a resource and how it can be hit
[CreateAssetMenu(menuName = "Tool Action/Gather Resource Node")]
public class GatherResourceNodeSO : ToolActionSO {

    [Header("Nodes the tool can hit")]
    [SerializeField] private ResourceNodeType[] _canHitNodesOfTypeArray;

    // Using ReadOnly to prevent modification at runtime
    private HashSet<ResourceNodeType> _canHitNodesOfType;

    [Header("Physics Settings")]
    [SerializeField] private LayerMask _resourceNodeLayerMask;

    // Cached reference to ItemManager to reduce repeated access
    private ItemManager _itemManager;

    private bool _isInitialized = false;

    /// <summary>
    /// Initializes the ScriptableObject. Should be called once during play mode.
    /// </summary>
    public void Initialize() {
        if (_isInitialized) {
            return;
        }

        _canHitNodesOfType = new HashSet<ResourceNodeType>(_canHitNodesOfTypeArray);
        _itemManager = ItemManager.Instance;
        _isInitialized = true;
    }

    public override void OnApplyToTileMap(Vector3Int gridPosition, ItemSlot itemSlot) {
        if (!_isInitialized) {
            Initialize();
        }

        // Define the area of the tile using local variables
        Vector2 bottomLeft = new Vector2(gridPosition.x + 0.1f, gridPosition.y + 0.1f);
        Vector2 topRight = new Vector2(gridPosition.x + 0.9f, gridPosition.y + 0.9f);

        // Get all colliders overlapping the tile area with specified LayerMask
        Collider2D[] colliders = Physics2D.OverlapAreaAll(bottomLeft, topRight, _resourceNodeLayerMask);

        if (colliders.Length == 0) {
            Debug.LogWarning("No colliders found in the specified tile area.");
            return; // Early exit if no colliders found
        }

        // Access the tool from the ItemDatabase using ItemId
        var tool = _itemManager.ItemDatabase[itemSlot.ItemId] as AxePickaxeToolSO;
        if (tool == null) {
            Debug.LogError($"ItemDatabase does not contain a valid AxePickaxeToolSO with ItemId: {itemSlot.ItemId}");
            return; // Early exit if tool not found or incorrect type
        }

        // Ensure rarity index is within bounds
        int rarityIndex = itemSlot.RarityId - 1;
        if (rarityIndex < 0 || rarityIndex >= tool.DamageOnAction.Length) {
            Debug.LogError($"Invalid RarityId: {itemSlot.RarityId} for tool with ItemId: {itemSlot.ItemId}");
            return; // Early exit if rarityId is invalid
        }

        int damage = tool.DamageOnAction[rarityIndex];

        foreach (Collider2D collider in colliders) {
            if (collider.TryGetComponent<ResourceNodeBase>(out var resourceNode)) {
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
