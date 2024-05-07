using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Object")]
public class ObjectSO : ItemSO {
    public enum ObjectTypes {
        ItemProducer,
        ItemConverter,
        Chest,
    }

    [Header("Place Object Settings")]
    public RecipeSO.RecipeTypes RecipeType;
    public int ProduceTimeInPercent;
    public ObjectTypes ObjectType;
    public bool CloseUIAndObjectOnPlayerLeave;

    [Header("Pick-Up")]
    public ItemSO ItemToPickUpObject;

    [Header("Visuals")]
    public Sprite HighlightSprite;
    public Sprite ActiveSprite;
    public Sprite InactiveSprite;
}
