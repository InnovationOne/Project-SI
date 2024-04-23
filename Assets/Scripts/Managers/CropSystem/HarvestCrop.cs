using UnityEngine;

public class HarvestCrop : Interactable {
    private Vector3Int _cropPosition;
    private SpriteRenderer _growthTimeFertilizerSpriteRenderer;
    private SpriteRenderer _qualityFertilizerSpriteRenderer;
    private SpriteRenderer _quantityFertilizerSpriteRenderer;
    private SpriteRenderer _regrowthTimeFertilizerSpriteRenderer;
    private SpriteRenderer _waterFertilizerSpriteRenderer;

    private void Start() {
        _growthTimeFertilizerSpriteRenderer = transform.Find("GrowthTimeFertilizerSprite").GetComponent<SpriteRenderer>();
        _qualityFertilizerSpriteRenderer = transform.Find("QualityFertilizerSprite").GetComponent<SpriteRenderer>();
        _quantityFertilizerSpriteRenderer = transform.Find("QuantityFertilizerSprite").GetComponent<SpriteRenderer>();
        _regrowthTimeFertilizerSpriteRenderer = transform.Find("RegrowthTimeFertilizerSprite").GetComponent<SpriteRenderer>();
        _waterFertilizerSpriteRenderer = transform.Find("WaterFertilizerSprite").GetComponent<SpriteRenderer>();
    }

    public override void Interact(Player player) {
        CropsManager.Instance.HarvestCropServerRpc(_cropPosition);
    }

    public void SetCropPosition(Vector3Int position) {
        _cropPosition = position;
    }

    public void SetGrowthTimeFertilizerSprite(Color? color = null) {
        _growthTimeFertilizerSpriteRenderer.enabled = !_growthTimeFertilizerSpriteRenderer.enabled;
        _growthTimeFertilizerSpriteRenderer.color = color ?? Color.white; // Set the color to white if no color value was passed
    }

    public void SetQualityFertilizerSprite(Color? color = null) {
        _qualityFertilizerSpriteRenderer.enabled = !_qualityFertilizerSpriteRenderer.enabled;
        _qualityFertilizerSpriteRenderer.color = color ?? Color.white; // Set the color to white if no color value was passed
    }

    public void SetQuantityFertilizerSprite(Color? color = null) {
        _quantityFertilizerSpriteRenderer.enabled = !_quantityFertilizerSpriteRenderer.enabled;
        _quantityFertilizerSpriteRenderer.color = color ?? Color.white; // Set the color to white if no color value was passed
    }

    public void SetRegrowthTimeFertilizerSprite(Color? color = null) {
        _regrowthTimeFertilizerSpriteRenderer.enabled = !_regrowthTimeFertilizerSpriteRenderer.enabled;
        _regrowthTimeFertilizerSpriteRenderer.color = color ?? Color.white; // Set the color to white if no color value was passed
    }

    public void SetWaterFertilizerSprite(Color? color = null) {
        _waterFertilizerSpriteRenderer.enabled = !_waterFertilizerSpriteRenderer.enabled;
        _waterFertilizerSpriteRenderer.color = color ?? Color.white; // Set the color to white if no color value was passed
    }
}
