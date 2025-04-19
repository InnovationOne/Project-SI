using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/BuildingSO")]
public class BuildingSO : ObjectSO {
    public int BuildTimeDays;
    public int MoneyCost;
    public GameObject FinishedBuildingPrefab;
}
