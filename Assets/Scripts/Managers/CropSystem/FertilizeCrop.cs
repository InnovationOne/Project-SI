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
public class FertilizeCrop : NetworkBehaviour {

    [SerializeField] List<FertilizerSpriteRendererPair> _fertilizerSpriteRenderersList = new();

    Dictionary<FertilizerSO.FertilizerTypes, SpriteRenderer> _fertilizerSpriteRenderers = new();
    
    private void Awake() {
        InitializeFertilizerRenderers();
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
}
