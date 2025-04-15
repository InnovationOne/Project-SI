using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Analytics;

[Serializable]
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData> {
    [Header("Game")]
    public ulong ClientId;

    [Header("Player")]
    public FixedString64Bytes Name;
    public Gender Gender;
    public Vector2 Position;
    public Vector2 LastDirection;
    public Vector2 RespawnPosition;

    public int InventorySize;
    public FixedString4096Bytes Inventory;
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


    public PlayerData(ulong clientId) {
        ClientId = clientId;
        Name = string.Empty;
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Gender);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref LastDirection);
        serializer.SerializeValue(ref RespawnPosition);

        serializer.SerializeValue(ref InventorySize);
        serializer.SerializeValue(ref Inventory);
        serializer.SerializeValue(ref ToolbeltSize);
        serializer.SerializeValue(ref LastSelectedToolbeltSlot);

        serializer.SerializeValue(ref MaxHp);
        serializer.SerializeValue(ref CurrentHp);
        serializer.SerializeValue(ref MaxEnergy);
        serializer.SerializeValue(ref CurrentEnergy);

        serializer.SerializeValue(ref HandsStyleIndex);
        serializer.SerializeValue(ref HelmetStyleIndex);
        serializer.SerializeValue(ref HairStyleIndex);
        serializer.SerializeValue(ref HairColor);
        serializer.SerializeValue(ref EyeColor);
        serializer.SerializeValue(ref BeltStyleIndex);
        serializer.SerializeValue(ref TorsoStyleIndex);
        serializer.SerializeValue(ref LegsStyleIndex);
        serializer.SerializeValue(ref FeetStyleIndex);
        serializer.SerializeValue(ref SkinStyleIndex);
        serializer.SerializeValue(ref SkinColor);
        serializer.SerializeValue(ref HeadStyleIndex);

        serializer.SerializeValue(ref DistanceMoved);

        serializer.SerializeValue(ref SkipIntro);
    }

    public bool Equals(PlayerData other) {
        return ClientId == other.ClientId &&
               Name.Equals(other.Name) &&
               Gender == other.Gender &&
               Position == other.Position &&
               LastDirection == other.LastDirection &&
               RespawnPosition == other.RespawnPosition &&

               InventorySize == other.InventorySize &&
               Inventory.Equals(other.Inventory) &&
               ToolbeltSize == other.ToolbeltSize &&
               LastSelectedToolbeltSlot == other.LastSelectedToolbeltSlot &&

               MaxHp == other.MaxHp &&
               CurrentHp == other.CurrentHp &&
               MaxEnergy == other.MaxEnergy &&
               CurrentEnergy == other.CurrentEnergy &&

               HandsStyleIndex == other.HandsStyleIndex &&
               HelmetStyleIndex == other.HandsStyleIndex &&
               HairStyleIndex == other.HairStyleIndex &&
               HairColor.Equals(other.HairColor) &&
               EyeColor.Equals(other.EyeColor) &&
               BeltStyleIndex == other.BeltStyleIndex &&
               TorsoStyleIndex == other.TorsoStyleIndex &&
               LegsStyleIndex == other.LegsStyleIndex &&
               FeetStyleIndex == other.FeetStyleIndex &&
               SkinStyleIndex == other.SkinStyleIndex &&
               SkinColor.Equals(other.SkinColor) &&
               HeadStyleIndex == other.HeadStyleIndex &&

               DistanceMoved == other.DistanceMoved &&

               SkipIntro == other.SkipIntro;
    }
}