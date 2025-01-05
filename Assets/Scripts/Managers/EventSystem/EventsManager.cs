using UnityEngine;

// Verwalte alle Events, die im Spiel auftreten k�nnen
public class EventsManager : MonoBehaviour {
    public QuestEvents QuestEvents;
    public ItemPickedUpEvents ItemPickedUpEvents;

    private void Awake() {
        QuestEvents = new QuestEvents();
        ItemPickedUpEvents = new ItemPickedUpEvents();
    }
}
