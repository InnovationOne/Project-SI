using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/NPC/DailyRoutine/Location")]
public class NPCLocation : ScriptableObject {
    public Vector3 Position;
    public float LeaveTimeInvoke;
}
