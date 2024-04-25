using Ink.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CropTileContainer {
    private Dictionary<Vector3Int, CropTile> _cropTileMap = new Dictionary<Vector3Int, CropTile>();
    public Dictionary<Vector3Int, CropTile> CropTileMap { get { return _cropTileMap; } }

    
    /// <summary>
    /// Retrieves the CropTile at the specified position.
    /// </summary>
    /// <param name="position">The position of the CropTile.</param>
    /// <returns>The CropTile at the specified position, or null if no CropTile exists at that position.</returns>
    public CropTile GetCropTileAtPosition(Vector3Int position) {
        _cropTileMap.TryGetValue(position, out CropTile tile);
        return tile;
    }

    
    /// <summary>
    /// Checks if a position is plowed in the crop tile map.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True if the position is plowed, false otherwise.</returns>
    public bool IsPositionPlowed(Vector3Int position) => _cropTileMap.ContainsKey(position);

    /// <summary>
    /// Checks if a given position is seeded with a crop.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True if the position is seeded with a crop, false otherwise.</returns>
    public bool IsPositionSeeded(Vector3Int position) => _cropTileMap.TryGetValue(position, out CropTile tile) && tile.CropId >= 0;
    
    /// <summary>
    /// Checks if a position can be fertilized with a specific item.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="itemId">The ID of the fertilizer item.</param>
    /// <returns>True if the position can be fertilized, false otherwise.</returns>
    public bool CanPositionBeFertilized(Vector3Int position, int itemId) {
        if (!_cropTileMap.TryGetValue(position, out CropTile cropTile)) {
            return false;
        }

        if (ItemManager.Instance.ItemDatabase.Items[itemId] is not FertilizerSO fertilizerSO) {
            throw new ArgumentException("Invalid item ID for fertilizer.", nameof(itemId));
        }

        return fertilizerSO.FertilizerType switch {
            FertilizerSO.FertilizerTypes.GrowthTime => cropTile.GrowthTimeScaler < (fertilizerSO.FertilizerBonusValue / 100) + 1,
            FertilizerSO.FertilizerTypes.RegrowthTime => cropTile.RegrowthTimeScaler < (fertilizerSO.FertilizerBonusValue / 100) + 1 && cropTile.IsRegrowing,
            FertilizerSO.FertilizerTypes.Quality => cropTile.QualityScaler < fertilizerSO.FertilizerBonusValue,
            FertilizerSO.FertilizerTypes.Quantity => cropTile.QuantityScaler < (fertilizerSO.FertilizerBonusValue / 100) + 1,
            FertilizerSO.FertilizerTypes.Water => cropTile.WaterScaler < (fertilizerSO.FertilizerBonusValue / 100),
            _ => throw new NotSupportedException("Unsupported fertilizer type"),
        };
    }

    /// <summary>
    /// Adds a CropTile to the container.
    /// </summary>
    /// <param name="crop">The CropTile to add.</param>
    /// <returns>True if the CropTile was successfully added, false otherwise.</returns>
    public bool AddCropTileToContainer(CropTile crop) {
        if (IsPositionPlowed(crop.CropPosition)) {
            throw new InvalidOperationException("A CropTile at this position has already been added to the container.");
        }

        _cropTileMap.Add(crop.CropPosition, crop);
        return true;
    }

    /// <summary>
    /// Removes a crop tile from the container.
    /// </summary>
    /// <param name="crop">The crop tile to remove.</param>
    /// <exception cref="KeyNotFoundException">Thrown if the crop tile is not found in the container.</exception>
    public void RemoveCropTileFromContainer(CropTile crop) {
        if (!IsPositionPlowed(crop.CropPosition)) {
            throw new KeyNotFoundException("The crop you want to remove isn't in the CropTileContainer.");
        }

        _cropTileMap.Remove(crop.CropPosition);
    }

    /// <summary>
    /// Clears the crop tile container by removing all crop tiles from the map.
    /// </summary>
    public void ClearCropTileContainer() {
        _cropTileMap.Clear();
    }

    /// <summary>
    /// Serializes the crop tile container into a list of JSON strings.
    /// </summary>
    /// <returns>A list of JSON strings representing the crop tile container.</returns>
    public List<string> SerializeCropTileContainer() {
        var cropsContainerJSON = new List<string>();
        foreach (var cropTile in _cropTileMap.Values) {
            var cropTileData = new CropTileData {
                CropId = cropTile.CropId,
                CropPosition = cropTile.CropPosition,
                CurrentGrowTimer = cropTile.CurrentGrowthTimer,
                IsRegrowing = cropTile.IsRegrowing,
                Damage = cropTile.Damage,
                SpriteRendererXPosition = cropTile.SpriteRendererOffset.x,
                SpriteRendererYPosition = cropTile.SpriteRendererOffset.y,
                SpriteRendererXScale = cropTile.SpriteRendererXScale,
                GrowthTimeScaler = cropTile.GrowthTimeScaler,
                RegrowthTimeScaler = cropTile.RegrowthTimeScaler,
                QualityScaler = cropTile.QualityScaler,
                QuantityScaler = cropTile.QuantityScaler,
                WaterScaler = cropTile.WaterScaler,
            };
            cropsContainerJSON.Add(JsonConvert.SerializeObject(cropTileData));
        }
        return cropsContainerJSON;
    }

    /// <summary>
    /// Deserializes a JSON string into a list of CropTile objects.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A list of CropTile objects.</returns>
    public List<CropTile> DeserializeCropTileContainer(string json) {
        var cropTiles = new List<CropTile>();
        var cropTileContainerJSON = JsonConvert.DeserializeObject<List<string>>(json);
        foreach (var cropTilesJSON in cropTileContainerJSON) {
            var cropTileData = JsonConvert.DeserializeObject<CropTileData>(cropTilesJSON);
            cropTiles.Add(new CropTile {
                CropId = cropTileData.CropId,
                CropPosition = cropTileData.CropPosition,
                CurrentGrowthTimer = cropTileData.CurrentGrowTimer,
                IsRegrowing = cropTileData.IsRegrowing,
                Damage = cropTileData.Damage,
                SpriteRendererOffset = new Vector2(cropTileData.SpriteRendererXPosition, cropTileData.SpriteRendererYPosition),
                SpriteRendererXScale = cropTileData.SpriteRendererXScale,
                GrowthTimeScaler = cropTileData.GrowthTimeScaler,
                RegrowthTimeScaler = cropTileData.RegrowthTimeScaler,
                QualityScaler = cropTileData.QualityScaler,
                QuantityScaler = cropTileData.QuantityScaler,
                WaterScaler = cropTileData.WaterScaler,
            });
        }

        return cropTiles;
    }
}
