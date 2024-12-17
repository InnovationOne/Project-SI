using System;
using UnityEngine;

[Serializable]
public class GameData {
    [Header("General Data")]
    public long LastPlayed;
    public int TimePlayed;

    [Header("Town Data")]
    public string TownName;
    public Sprite CityCoatOfArms;
    public int GroceryStoreLevel;

    [Header("Finanz Manager")]
    public int MoneyOfFarm;
    public int MoneyOfTown;

    [Header("Time Manager")]
    public int CurrentDay;
    public int CurrentSeason;
    public int CurrentYear;
    public int[] WeatherForecast;

    [Header("Crops Manager")]
    public string CropsOnMap;

    [Header("Objects Manager")]
    public string PlacedObjects;

    public string Recipes;
    public string Quests;
    public string PlayerData;

    [Header("QuestData")]
    public string QuestData;

    [Header("Ink Variables")]
    public string inkVariables;

    public GameData() {
        LastPlayed = 0;
        TimePlayed = 0;

        TownName = "No town founded";
        CityCoatOfArms = null;
        GroceryStoreLevel = 0;

        MoneyOfFarm = 0;
        MoneyOfTown = 0;

        CurrentDay = 0;
        CurrentSeason = 0;
        CurrentYear = 0;
        WeatherForecast = new int[3];

        CropsOnMap = string.Empty;

        PlacedObjects = string.Empty;

        Recipes = string.Empty;
        Quests = string.Empty;
        PlayerData = string.Empty;

        QuestData = string.Empty;

        inkVariables = string.Empty;
    }
}
