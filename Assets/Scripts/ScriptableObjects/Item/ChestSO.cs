using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ChestSO")]
public class ChestSO : ObjectSO {
    [Header("Chest Settings")]
    public int ItemSlotCount;

    [Header("Sprites")]
    public Sprite OpenedSprite;
    public Sprite ClosedSprite;
}
