using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a sprinkler object in the game.
/// </summary>
public class Sprinkler : MonoBehaviour {
    [SerializeField] private ObjectVisual _visual;
    private int _itemId;

    private void Start() {
        TimeAndWeatherManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;
    }

    /// <summary>
    /// Initializes the sprinkler with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the sprinkler item.</param>
    public void Initialize(int itemId) {
        _itemId = itemId;
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

        return positionsToWater;
    }

    /// <summary>
    /// Gets the SprinklerSO associated with this Sprinkler instance.
    /// </summary>
    private SprinklerSO SprinklerSO => ItemManager.Instance.ItemDatabase[_itemId] as SprinklerSO;
}
