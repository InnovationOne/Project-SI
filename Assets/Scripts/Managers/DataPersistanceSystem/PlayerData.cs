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

    // Save in a local gamedata file
    public string Hotkeys;

    [Header("HP & Energy")]
    public float MaxHp;
    public float CurrentHp;
    public float MaxEnergy;
    public float CurrentEnergy;

    /*
    [Header("Clothing")]
    public Sprite hat;
    public Sprite hair;
    public Color32 hairColor;
    public Color32 eyeColor;
    public Sprite earrings;
    public Color32 skinColor;
    public Sprite shirt;
    public Sprite gloves;
    public Sprite pants;
    public Sprite shoes;

    [Header("Achivements")]
    public int distanceMoved;

    [Header("Hidden-Settings")]
    public bool skipTutorial;
    */
}