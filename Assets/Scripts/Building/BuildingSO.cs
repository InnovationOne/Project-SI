using System;
using UnityEngine;

[Serializable]
public struct ResourceCosts {
    public ItemSlot[] ResourceCost;
    public int MoneyCost;
}

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/Building")]
public class BuildingSO : ItemSO {
    public string BuildingName;
    public int BuildTimeDays;
    public int UpgradeTimeDays;
    public int[] Capacity;
    public ResourceCosts[] ResourceCosts;
    public bool[] HasIncubator;
    public bool[] HasAutomaticFeeding;
    public bool[] HasTimedDoor;
    public bool[] HasWeatherDoor;

    public AnimalSize AnimalSize;
}
