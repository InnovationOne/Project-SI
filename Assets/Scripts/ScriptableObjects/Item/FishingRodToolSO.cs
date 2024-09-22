using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ToolSO/FishingRodToolSO")]
public class FishingRodToolSO : ToolSO {
    public int[] BiteRate;
    public int[] CatchChance;
}
