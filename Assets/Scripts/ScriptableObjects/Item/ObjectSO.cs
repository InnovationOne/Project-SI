using UnityEngine;

// This class contains information used in an item e.g. tool, seed, oven etc.
[CreateAssetMenu(menuName = "Scriptable Objects/Object")]
public class ObjectSO : ItemSO {
    public enum ObjectTypes {
        ItemProducer,
        ItemConverter,
    }

    [Header("Place Object Settings")]
    public RecipeSO.RecipeTypes RecipeType;
    public int ProduceTimeInPercent;
    public ObjectTypes ObjectType;

    [Header("Pick-Up")]
    public ItemSO ItemToPickUpObject;

    [Header("Visuals")]
    public Sprite HighlightSprite;
    public Sprite ActiveSprite;
    public Sprite InactiveSprite;
}
