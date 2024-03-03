using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CropDatabase {
    private const string CROPS_RESOURCE_PATH = "Crops/CropSOs";
    private const string TEMPLATE_NAME = "_Template";

    private readonly List<CropSO> _crops;

    public CropDatabase() {
        // Initialize the _crops list by loading all CropSOs from the Resources folder.
        _crops = Resources.LoadAll<CropSO>(CROPS_RESOURCE_PATH).ToList();

        // Remove the _Template item if it exists
        RemoveTemplateCrop();

        // Assign unique crop IDs
        AssignCropIds();
    }

    private void RemoveTemplateCrop() {
        // Find and remove the _Template item if it exists
        var templateCrop = _crops.FirstOrDefault(crop => crop.name == TEMPLATE_NAME);
        if (templateCrop != null) {
            _crops.Remove(templateCrop);
        }
    }

    private void AssignCropIds() {
        // Assign unique crop IDs to each crop in the list
        for (int i = 0; i < _crops.Count; i++) {
            _crops[i].CropID = i;
        }
    }

    public CropSO GetCropFromCropId(int cropId) {
        if (cropId >= 0 && cropId < _crops.Count) {
            return _crops[cropId];
        } else {
            Debug.Log($"Crop with ID {cropId} does not exist!");
            return null;
        }
    }

    public List<CropSO> GetCropsFromDatabase() {
        return _crops;
    }
}

