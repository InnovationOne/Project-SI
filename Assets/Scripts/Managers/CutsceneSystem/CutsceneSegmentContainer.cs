using FMODUnity;
using System;
using UnityEngine;

[Serializable]
public class CutsceneSegmentContainer {
    public enum SegmentTypes {
        End,
        MainNPCChat,
        TextBubbleChat,
        SpawnCharacter,
        RemoveCharacter,
        CharacterAnimate,
        CharacterMove,
        CharacterTargetFurniture,
        CharacterTargetExit,
        CameraMove,
        CameraFollow,
        OpenDoor,
        CloseDoor,
        ShowWall,
        HideWall,
        HideObject,
        PlayAudio,
        RedirectPlayerSpawn,
        NPCGiveGift,
        MoneyUpdate,
        CharacterEmoji,
        ShowGameCanvas,
        HideGameCanvas,
        CharacterDirection,
        ShowLetterboxing,
        HideLetterboxing,
        ChangeScene
    }

    public enum SegmentCompleteTypes {
        Timer,
        Chat,
        Movement
    }

    [Header("General settings")]
    [Tooltip("A short description for editor reference.")]
    public string SegmentDescription;
    [Tooltip("Select the type of this segment.")]
    public SegmentTypes SegmentType;
    [Tooltip("How this segment will determine its completion.")]
    public SegmentCompleteTypes CompleteType;
    [Tooltip("Delay before this segment starts (seconds).")]
    public float SegmentStartDelay;

    [Header("Timer Settings")]
    [Tooltip("Time to wait if using a timer-based completion.")]
    public float SegmentTimer;

    [Header("Dialogue Settings")]
    [Tooltip("Dialogue text (if not using Ink).")]
    public string DialogueText;
    [Tooltip("Ink JSON file for dialogue (if using Ink).")]
    public TextAsset InkJSON;

    [Header("Spawn Character Settings")]
    [Tooltip("Character prefab to spawn.")]
    public GameObject CharacterPrefab;
    [Tooltip("Spawn point for the character.")]
    public Vector2 SpawnPoint;

    [Header("Movement Settings")]
    [Tooltip("Target position (Vector2) for CharacterMove (Pfadfindung).")]
    public Vector2 MoveDestination;
    [Tooltip("Target object (Transform) for CharacterTargetFurniture (e.g. a piece of furniture).")]
    public Vector2 TargetFurniture;
    [Tooltip("Exit point (Transform) for CharacterTargetExit.")]
    public Vector2 ExitPoint;
    [Tooltip("Optional: ID of the exit point for scene logic.")]
    public string ExitPointID;
    [Tooltip("Distance threshold to consider movement complete.")]
    public float ArrivalThreshold = 0.5f;

    [Header("Camera Settings")]
    [Tooltip("Target position (Vector3) for the camera (CameraMove).")]
    public Vector2 TargetPosition;

    [Header("Audio Settings")]
    [Tooltip("FMOD event to be played.")]
    public EventReference AudioEvent;

    [Header("NPC Gift Settings")]
    [Tooltip("The ItemSlot to be given to the player (NPCGiveGift).")]
    public ItemSlot ItemSlot;

    [Header("Money Settings")]
    [Tooltip("Amount of money added to the player (MoneyUpdate).")]
    public int MoneyAmount;

    [Header("Character Direction Settings")]
    [Tooltip("Direction vector (Vector2) for CharacterDirection.")]
    public Vector2 Direction;

    [Header("Scene Settings")]
    [Tooltip("Name of the scene to be loaded (ChangeScene).")]
    public string SceneName;

    [Header("Pathfinding & Movement")]
    [Tooltip("Moving object (e.g. the player or an NPC) for movement segments.")]
    public GameObject PrimaryTarget;
}