using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CropTileContainer {
    public List<CropTile> CropTiles { get; private set; }

    public CropTileContainer() { 
        CropTiles = new List<CropTile>();
    }

    // Find a CropTile at a specific position.
    public CropTile GetCropTileAtPosition(Vector3Int position) {
        return CropTiles.Find(tile => tile.CropPosition == position);
    }

    // Check if a position is plowed (a CropTile exists at that position).
    public bool IsPositionPlowed(Vector3Int position) {
        return GetCropTileAtPosition(position) != null;
    }

    // Check if a position is seeded (a CropTile exists with a growing crop at that position).
    public bool IsPositionSeeded(Vector3Int position) {
        return CropTiles.Any(tile => tile.CropPosition == position && tile.CropId >= 0);
    }

    // Try to add a CropTile to the container. Returns true if added, false otherwise.
    public bool TryAddCropTileToContainer(CropTile crop) {
        if (IsPositionPlowed(crop.CropPosition)) {
            Debug.LogWarning("Cannot add a cropTile that has already been added to the crop tile container!");
            return false;
        }

        CropTiles.Add(crop);
        return true;
    }

    // Remove a CropTile from the container.
    public void RemoveCropTileFromContainer(CropTile crop) {
        if (!IsPositionPlowed(crop.CropPosition)) {
            Debug.LogWarning("The crop you want to remove isn't in the CropTileContainer");
            return;
        }

        CropTiles.Remove(crop);
    }

    // Clear all CropTiles from the container.
    public void ClearCropTileContainer() {
        CropTiles.Clear();
    }

    public List<string> SerializeCropTileContainer(List<CropTile> cropTiles) {
        var cropsContainerJSON = new List<string>();
        foreach (var cropTile in cropTiles) {
            var cropTileData = new CropTileData {
                CropId = cropTile.CropId,
                CropPosition = cropTile.CropPosition,
                CurrentGrowTimer = cropTile.CurrentGrowthTimer,
                IsRegrowing = cropTile.IsRegrowing,
                Damage = cropTile.Damage,
                SpriteRendererXPosition = cropTile.SpriteRendererOffset.x,
                SpriteRendererYPosition = cropTile.SpriteRendererOffset.y,
                SpriteRendererXScale = cropTile.SpriteRendererXScale,
            };
            var cropTileJSON = JsonConvert.SerializeObject(cropTileData);
            cropsContainerJSON.Add(cropTileJSON);
        }
        return cropsContainerJSON;
    }

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
                SpriteRendererOffset = new Vector3(cropTileData.SpriteRendererXPosition, cropTileData.SpriteRendererYPosition, -5),
                SpriteRendererXScale = cropTileData.SpriteRendererXScale,
            });
        }
        return cropTiles;
    }
}
