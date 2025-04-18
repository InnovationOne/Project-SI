using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ProducerSO")]
public class ItemProducerSO : ObjectSO {
    [Header("Recipe")]
    public RecipeSO Recipe;

    [Header("Advanced")]
    public int SpeedMultiply = 1;
    public int AmountMultiply = 1;

    [Header("Sprites")]
    public Sprite ActiveSprite;
    public Sprite InactiveSprite;
}
