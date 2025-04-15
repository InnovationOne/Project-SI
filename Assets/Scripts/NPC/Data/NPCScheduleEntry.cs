using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/NPC/Schedule Entry")]
public class NPCScheduleEntry : ScriptableObject {
    public string Label;

    public Vector3 Location;
    public int ArrivalTimeInSeconds;    // e.g., 21600 for 6 AM
    public int DepartureTimeInSeconds;  // e.g., 57600 for 4 PM

    public ActivityType Activity;

    public enum ActivityType {
        Sleep,
        Work,
        Eat,
        Wander,
        Social,
        Idle
    }
}
