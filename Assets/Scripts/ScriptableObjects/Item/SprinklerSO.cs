using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/SprinklerSO")]
public class SprinklerSO : ObjectSO {
    public enum SprinklerArea {
        Area3x3 = 3,
        Area5x5 = 5,
        Area7x7 = 7,
        Area9x9 = 9,
    }

    public SprinklerArea Area;
}
