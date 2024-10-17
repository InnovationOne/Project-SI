using System;
using UnityEngine;

[Serializable]
public struct Area {
    public int XSize;
    public int YSize;

    public Area(int xSize, int ySize) {
        XSize = xSize;
        YSize = ySize;
    }
}

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ToolSO/AreaToolSO")]
public class AreaToolSO : ToolSO {
    public Area[] Area;
}
