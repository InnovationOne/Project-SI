using System.Collections.Generic;
using UnityEngine;

enum SprinklerSize {
    Size3x3 = 3,
    Size5x5 = 5,
    Size7x7 = 7,
    Size9x9 = 9,
}

public class SprinklerBehaviour : MonoBehaviour
{
    [SerializeField] private SprinklerSize _sprinklerSize;
    

    private void Start() {
        TimeAndWeatherManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;
    }

    private void TimeAndWeatherManager_OnNextDayStarted() {
        Vector3Int startPos = new Vector3Int((int)transform.position.x, (int)transform.position.y) - new Vector3Int((int)_sprinklerSize / 2 + 1, (int)_sprinklerSize / 2 + 1);

        var positionsToWater = new List<Vector3Int>();

        // Horizontal
        for (int i = 0; i < (int)_sprinklerSize; i++) {
            // Vertical
            for (int j = 0; j < (int)_sprinklerSize; j++) {
                positionsToWater.Add(startPos + new Vector3Int(i, j, 0));
            }
        }

        CropsManager.Instance.WaterTiles(positionsToWater, 0);
    }
}
