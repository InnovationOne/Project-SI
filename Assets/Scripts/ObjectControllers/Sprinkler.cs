using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a sprinkler object in the game.
/// </summary>
public class Sprinkler : MonoBehaviour {
    private ObjectVisual _visual;
    private int _itemId;

    private void Start() {
        TimeManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;
    }

    private void OnDestroy() {
        TimeManager.Instance.OnNextDayStarted -= TimeAndWeatherManager_OnNextDayStarted;
    }

    /// <summary>
    /// Initializes the sprinkler with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the sprinkler item.</param>
    public void Initialize(int itemId) {
        _itemId = itemId; 
        _visual = GetComponentInChildren<ObjectVisual>();
        _visual.SetSprite(SprinklerSO.InactiveSprite);
    }

    private void TimeAndWeatherManager_OnNextDayStarted() {
        List<Vector3Int> positionsToWater = CalculateWateringPositions();
        CropsManager.Instance.WaterTiles(positionsToWater, 0);
    }

    /// <summary>
    /// Calculates the positions to water based on the sprinkler's area.
    /// </summary>
    /// <returns>A list of positions to water.</returns>
    private List<Vector3Int> CalculateWateringPositions() {
        Vector3Int startPos = 
            new Vector3Int((int)transform.position.x, (int)transform.position.y) -
            new Vector3Int((int)SprinklerSO.Area / 2 + 1, (int)SprinklerSO.Area / 2 + 1);

        List<Vector3Int> positionsToWater = new();
        for (int i = 0; i < (int)SprinklerSO.Area; i++) {
            for (int j = 0; j < (int)SprinklerSO.Area; j++) {
                positionsToWater.Add(startPos + new Vector3Int(i, j));
            }
        }

        // For the first sprinkler type that just waters left, right, up, and down from the sprinkler and not a true 3x3 area.
        if (SprinklerSO.Area == SprinklerSO.SprinklerArea.Area1x1) {
            Vector3Int[] removePositions = new Vector3Int[] {
                new(-1, 1), // Top left
                new(1, 1), // Top right
                new(1, -1), // Bottom right
                new(-1, -1) // Bottom left
            };
            Vector3Int currentPosition = new((int)transform.position.x, (int)transform.position.y);

            foreach (Vector3Int removePosition in removePositions) {
                positionsToWater.Remove(currentPosition + removePosition);
            }
        }

        return positionsToWater;
    }

    /// <summary>
    /// Gets the SprinklerSO associated with this Sprinkler instance.
    /// </summary>
    private SprinklerSO SprinklerSO => ItemManager.Instance.ItemDatabase[_itemId] as SprinklerSO;
}
