using System.Collections.Generic;
using UnityEngine;

// Tool action for gathering resources from resource nodes.
[CreateAssetMenu(menuName = "Tool Action/Gather Resource Node")]
public class GatherResourceNodeSO : ToolActionSO {

    [Header("Nodes the tool can hit")]
    [SerializeField] ResourceNodeType[] _canHitNodesOfTypeArray;
    [SerializeField] LayerMask _resourceNodeLayerMask;

    HashSet<ResourceNodeType> _canHitNodesOfType;
    ItemManager _itemManager;
    PlayerToolsAndWeaponController _playerToolsAndWeaponController;
    bool _init = false;

    void OnEnable() => _init = false;

    void InitIfNeeded() {
        if (_init) return;
        _canHitNodesOfType = new HashSet<ResourceNodeType>(_canHitNodesOfTypeArray);
        _itemManager = GameManager.Instance.ItemManager;
        _playerToolsAndWeaponController = PlayerController.LocalInstance.PlayerToolsAndWeaponController;
        _init = true;
    }

    public override void OnApplyToTileMap(Vector3Int gridPosition, ItemSlot itemSlot) {
        InitIfNeeded();

        var bottomLeft = new Vector2(gridPosition.x + 0.1f, gridPosition.y + 0.1f);
        var topRight = new Vector2(gridPosition.x + 0.9f, gridPosition.y + 0.9f);

        var colliders = Physics2D.OverlapAreaAll(bottomLeft, topRight, _resourceNodeLayerMask);
        if (colliders.Length == 0) {
            Debug.LogWarning("No resource nodes found.");
            _playerToolsAndWeaponController.ClientCallbackClientRpc(false);
            return;
        }

        var tool = _itemManager.ItemDatabase[itemSlot.ItemId] as AxePickaxeToolSO;
        if (tool == null) {
            Debug.LogError($"No valid AxePickaxeToolSO found for ItemId: {itemSlot.ItemId}");
            _playerToolsAndWeaponController.ClientCallbackClientRpc(false);
            return;
        }

        int rarityIndex = itemSlot.RarityId - 1;
        if (rarityIndex < 0 || rarityIndex >= tool.DamageOnAction.Length) {
            Debug.LogError($"Invalid RarityId: {itemSlot.RarityId} for tool ItemId: {itemSlot.ItemId}");
            _playerToolsAndWeaponController.ClientCallbackClientRpc(false);
            return;
        }

        int damage = tool.DamageOnAction[rarityIndex];

        bool hitSomething = false;
        foreach (var collider in colliders) {
            if (collider.TryGetComponent<ResourceNodeBase>(out var resourceNode) && resourceNode.CanHitResourceNodeType(_canHitNodesOfType)) {
                resourceNode.HitResourceNodeServerRpc(damage);
                hitSomething = true;
                break;
            }
        }

        if (!hitSomething) {
            Debug.LogWarning("No valid resource nodes of the allowed types hit by the tool.");
            _playerToolsAndWeaponController.ClientCallbackClientRpc(false);
        }
    }
}
