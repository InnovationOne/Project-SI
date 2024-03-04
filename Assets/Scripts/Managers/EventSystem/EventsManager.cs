using UnityEngine;

// Verwalte alle Events, die im Spiel auftreten können
public class EventsManager : MonoBehaviour {
    public static EventsManager Instance { get; private set; }

    public QuestEvents QuestEvents;
    public ItemPickedUpEvents ItemPickedUpEvents;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of EventsManager in the scene!");
            return;
        }
        Instance = this;

        QuestEvents = new QuestEvents();
        ItemPickedUpEvents = new ItemPickedUpEvents();
    }
}
