using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/SprinklerSO")]
public class SprinklerSO : ObjectSO {
    public enum SprinklerArea {
        Area1x1 = 1,
        Area3x3 = 3,
        Area5x5 = 5,
        Area7x7 = 7,
    }

    public SprinklerArea Area;
}
