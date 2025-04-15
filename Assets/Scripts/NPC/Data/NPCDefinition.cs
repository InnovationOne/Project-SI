using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/NPC/NPC Definition")]
public class NPCDefinition : ScriptableObject {
    [Header("Basic Info")]
    public string NPCName;
    public Sprite Portrait;
    public GameObject Prefab;

    [Header("Home & Work")]
    public Transform HomeLocation;
    public Transform DefaultWorkLocation;

    [Header("Personality")]
    [Range(0f, 1f)] public float Punctuality;  // High: always on time
    [Range(0f, 1f)] public float Sociability;  // High: greets people often
    [Range(0f, 1f)] public float WorkEthic;    // High: rarely skips work
    [Range(0f, 1f)] public float Spirituality; // High: more likely to attend church
    [Range(0f, 1f)] public float Independence; // High: more random decisions

    [Header("Dialogue")]
    public TextAsset DialogueScript; // Optional ink file or similar
}
