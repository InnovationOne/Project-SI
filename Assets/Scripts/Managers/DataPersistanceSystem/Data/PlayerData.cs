using System;
using UnityEngine;
using UnityEngine.Analytics;

[Serializable]
public class PlayerData {
    [Header("Game")]
    public ulong OwnerClientId;

    [Header("Player")]
    public string Name;
    public Gender Gender;
    public Vector2 Position;
    public Vector2 LastDirection;
    public Vector2 RespawnPosition;

    public int InventorySize;
    public string Inventory;
    public int ToolbeltSize;
    public int LastSelectedToolbeltSlot;

    [Header("HP & Energy")]
    public float MaxHp;
    public float CurrentHp;
    public float MaxEnergy;
    public float CurrentEnergy;

    [Header("Clothing")]
    public int HandsStyleIndex;
    public int HelmetStyleIndex;
    public int HairStyleIndex;
    public Color32 HairColor;
    public Color32 EyeColor;
    public int BeltStyleIndex;
    public int TorsoStyleIndex;
    public int LegsStyleIndex;
    public int FeetStyleIndex;
    public int SkinStyleIndex;
    public Color32 SkinColor;
    public int HeadStyleIndex;

    [Header("Achivements")]
    public int DistanceMoved;

    [Header("Hidden-Settings")]
    public bool SkipIntro;


    public PlayerData(ulong ownerClientId) {
        OwnerClientId = ownerClientId;
        Name = "";
        Gender = Gender.Male;
        Position = Vector2.zero;
        LastDirection = Vector2.zero;
        RespawnPosition = Vector2.zero;

        InventorySize = 0;
        Inventory = string.Empty;
        ToolbeltSize = 0;
        LastSelectedToolbeltSlot = 0;

        MaxHp = 100f;
        CurrentHp = 100f;
        MaxEnergy = 100f;
        CurrentEnergy = 100f;

        HandsStyleIndex = 0;
        HelmetStyleIndex = 0;
        HairStyleIndex = 0;
        HairColor = Color.black;
        EyeColor = Color.blue;
        BeltStyleIndex = 0;
        TorsoStyleIndex = 0;
        LegsStyleIndex = 0;
        FeetStyleIndex = 0;
        SkinStyleIndex = 0;
        SkinColor = Color.white;
        HeadStyleIndex = 0;

        DistanceMoved = 0;

        SkipIntro = false;
    }
}