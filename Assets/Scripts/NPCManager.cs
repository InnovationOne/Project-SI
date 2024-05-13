using UnityEngine;

[RequireComponent(typeof(TimeAgent))]
public class NPCManager : MonoBehaviour {
    public static NPCManager Instance { get; private set; }

    public NPC[] NPCs;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of NPCManager in the scene!");
            return;
        }
        Instance = this;
    }

    private void Start() {
        foreach (NPC npc in NPCs) {
            TimeAgent timeAgent = npc.GetComponent<TimeAgent>();
            timeAgent.DailyRoutine = npc.DailyRoutines[0];
        }
    }
}
