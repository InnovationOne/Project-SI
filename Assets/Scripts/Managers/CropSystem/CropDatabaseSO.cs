using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Database/Crop Database")]
public class CropDatabaseSO : ScriptableObject {
    // Use a private field with a public property to encapsulate the list of crops.
    [SerializeField] private List<CropSO> _crops = new List<CropSO>();
    public IReadOnlyList<CropSO> Crops => _crops;

    // This method assigns unique crop IDs to each crop in the list.
    public void AssignCropIds() {
        for (int i = 0; i < Crops.Count; i++) {
            Crops[i].CropID = i;
        }
    }

    public CropSO GetCropSOFromCropId(int cropId) {
        var crop = _crops.FirstOrDefault(c => c.CropID == cropId);
        if (crop == null) {
            Debug.LogError($"Crop with ID {cropId} does not exist!");
        }
        return crop;
    }
}

