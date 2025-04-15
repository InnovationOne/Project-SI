using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/NPC/Schedule")]
public class NPCSchedule : ScriptableObject {
    [Header("0 = Monday, 6 = Sunday")]
    public NPCScheduleEntry[] Monday;
    public NPCScheduleEntry[] Tuesday;
    public NPCScheduleEntry[] Wednesday;
    public NPCScheduleEntry[] Thursday;
    public NPCScheduleEntry[] Friday;
    public NPCScheduleEntry[] Saturday;
    public NPCScheduleEntry[] Sunday;

    public NPCScheduleEntry[] GetTodaySchedule(int dayIndex) {
        return dayIndex switch {
            0 => Monday,
            1 => Tuesday,
            2 => Wednesday,
            3 => Thursday,
            4 => Friday,
            5 => Saturday,
            6 => Sunday,
            _ => null
        };
    }
}
