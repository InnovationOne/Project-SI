using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class FertilizerSpriteRendererPair {
    public FertilizerSO.FertilizerTypes FertilizerType;
    public SpriteRenderer SpriteRenderer;
}

[RequireComponent(typeof(NetworkObject))]
public class HarvestCrop : NetworkBehaviour, IInteractable {
    [SerializeField] List<FertilizerSpriteRendererPair> _fertilizerSpriteRenderersList = new();

    [NonSerialized] float _maxDistanceToPlayer;
    public float MaxDistanceToPlayer => _maxDistanceToPlayer;

    Dictionary<FertilizerSO.FertilizerTypes, SpriteRenderer> _fertilizerSpriteRenderers = new(); 
    Vector3Int _cropPosition;
    CropsManager _cropsManager;

    private void Awake() {
        InitializeFertilizerRenderers();
    }

    private void Start() {
        _cropsManager = GameManager.Instance.CropsManager;
    }

    // Build the dictionary from the serialized list for quick runtime lookups.
    private void InitializeFertilizerRenderers() {
        _fertilizerSpriteRenderers = new Dictionary<FertilizerSO.FertilizerTypes, SpriteRenderer>(_fertilizerSpriteRenderersList.Count);
        foreach (var kvp in _fertilizerSpriteRenderersList) {
            if (kvp.SpriteRenderer == null) {
                Debug.LogError($"[HarvestCrop] Missing SpriteRenderer for {kvp.FertilizerType}");
                continue;
            }
            _fertilizerSpriteRenderers[kvp.FertilizerType] = kvp.SpriteRenderer;
        }
    }

    // Called by other game systems to trigger harvesting over the network.
    public void Interact(PlayerController player) => _cropsManager.HarvestCropServerRpc(new Vector3IntSerializable(_cropPosition));

    // Set the crop's position so that networking and gameplay systems know where it is in the world.
    public void SetCropPosition(Vector3Int position) => _cropPosition = position;

    // Update the displayed fertilizer sprite for a given fertilizer type, optionally changing its color.
    public void SetFertilizerSprite(FertilizerSO.FertilizerTypes fertilizerType, Color? color = null) {
        if (_fertilizerSpriteRenderers == null) {
            Debug.LogError("[HarvestCrop] Fertilizer renderers not initialized. Ensure Awake() was called properly.");
            return;
        }

        if (_fertilizerSpriteRenderers.TryGetValue(fertilizerType, out var spriteRenderer)) {
            ToggleSprite(spriteRenderer, color);
        } else {
            Debug.LogWarning($"[HarvestCrop] No sprite renderer found for fertilizer type: {fertilizerType}");
        }
    }

    // Enable/disable a sprite renderer and apply the chosen color.
    private void ToggleSprite(SpriteRenderer spriteRenderer, Color? color) {
        if (spriteRenderer == null) return;
        spriteRenderer.enabled = !spriteRenderer.enabled;
        spriteRenderer.color = color ?? Color.white;
    }

    // Placeholder for picking up items if the crop is part of a placed object with loot.
    public void PickUpItemsInPlacedObject(PlayerController player) { }

    // Placeholder for any pre-load initialization with a specific item ID.
    public void InitializePreLoad(int itemId) { }

}
