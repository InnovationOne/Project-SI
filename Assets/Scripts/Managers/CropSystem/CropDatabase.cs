using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Database/Crop Database")]
public class CropDatabase : ScriptableObject {
    public List<CropSO> Crops;

    public void AssignCropIds() {
        // Assign unique crop IDs to each crop in the list
        for (int i = 0; i < Crops.Count; i++) {
            Crops[i].CropID = i;
        }
    }

    public CropSO GetCropFromCropId(int cropId) {
        if (cropId >= 0 && cropId < Crops.Count) {
            return Crops[cropId];
        } else {
            Debug.Log($"Crop with ID {cropId} does not exist!");
            return null;
        }
    }

    public List<CropSO> GetCropsFromDatabase() {
        return Crops;
    }
}

