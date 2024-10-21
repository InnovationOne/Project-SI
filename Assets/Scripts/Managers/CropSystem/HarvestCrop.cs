using System;
using System.Collections.Generic;
using UnityEngine;

public class HarvestCrop : MonoBehaviour, IInteractable {
    // A list of fertilizer types and their corresponding sprite renderers to be converted into a dictionary.
    [SerializeField] private List<FertilizerSpriteRendererPair> _fertilizerSpriteRenderersList = new(); 
    // A dictionary of fertilizer types and their corresponding sprite renderers.
    private Dictionary<FertilizerSO.FertilizerTypes, SpriteRenderer> _fertilizerSpriteRenderers = new(); 
    private Vector3Int _cropPosition; // The position of the crop in the grid.
    private bool _initialized = false; // Whether the fertilizer sprite renderers have been initialized.

    [NonSerialized] private float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

    /// <summary>
    /// Initializes the fertilizer sprite renderers dictionary based on the fertilizer sprite renderers list.
    /// </summary>
    private void InitializeFertilizerRenderers() {
        if (_fertilizerSpriteRenderersList.Count == 0) {
            Debug.LogWarning("FertilizerSpriteRendererList is empty. Ensure that it is properly populated in the Unity Editor.");
            return;
        }

        _fertilizerSpriteRenderers = new Dictionary<FertilizerSO.FertilizerTypes, SpriteRenderer>(_fertilizerSpriteRenderersList.Count);
        foreach (var pair in _fertilizerSpriteRenderersList) {
            if (pair.SpriteRenderer == null) {
                Debug.LogError($"Missing SpriteRenderer for {pair.FertilizerType}");
                continue;
            }
            _fertilizerSpriteRenderers[pair.FertilizerType] = pair.SpriteRenderer;
        }
    }

    /// <summary>
    /// Interacts with the crop by harvesting it.
    /// </summary>
    /// <param name="player">The player interacting with the crop.</param>
    public void Interact(Player player) {
        CropsManager.Instance.HarvestCropServerRpc(new Vector3IntSerializable(_cropPosition));
    }

    /// <summary>
    /// Sets the position of the crop.
    /// </summary>
    /// <param name="position">The position to set.</param>
    public void SetCropPosition(Vector3Int position) {
        _cropPosition = position;
    }

    /// <summary>
    /// Sets the fertilizer sprite for a given fertilizer type.
    /// </summary>
    /// <param name="fertilizerType">The type of fertilizer.</param>
    /// <param name="color">The color of the sprite (optional).</param>
    public void SetFertilizerSprite(FertilizerSO.FertilizerTypes fertilizerType, Color? color = null) {
        if (!_initialized) {
            InitializeFertilizerRenderers();
            _initialized = true;
        }

        if (_fertilizerSpriteRenderers.TryGetValue(fertilizerType, out var spriteRenderer)) {
            ToggleSprite(spriteRenderer, color);
        }
    }

    /// <summary>
    /// Toggles the visibility of a SpriteRenderer and sets its color.
    /// </summary>
    /// <param name="spriteRenderer">The SpriteRenderer to toggle.</param>
    /// <param name="color">The color to set the SpriteRenderer to. If null, the color will be set to white.</param>
    private void ToggleSprite(SpriteRenderer spriteRenderer, Color? color) {
        if (spriteRenderer == null) {
            Debug.LogError("Attempted to toggle a non-existent SpriteRenderer.");
            return;
        }
        spriteRenderer.enabled = !spriteRenderer.enabled;
        spriteRenderer.color = color ?? Color.white;
    }

    public void PickUpItemsInPlacedObject(Player player) { }

    public void InitializePreLoad(int itemId) { }

    /// <summary>
    /// Represents a pair of a fertilizer type and a sprite renderer for a list that is converted into a dictionary.
    /// </summary>
    [Serializable]
    public class FertilizerSpriteRendererPair {
        public FertilizerSO.FertilizerTypes FertilizerType;
        public SpriteRenderer SpriteRenderer;
    }
}
