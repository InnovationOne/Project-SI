using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ChestSO")]
public class ChestSO : ObjectSO {
    [Header("Chest Settings")]
    public int ItemSlots;

    public Sprite OpenSprite;
    public Sprite ClosedSprite;
}
