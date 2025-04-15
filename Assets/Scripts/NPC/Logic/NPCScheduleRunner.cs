using UnityEngine;

[RequireComponent(typeof(NPC))]
public class NPCScheduleRunner : MonoBehaviour {
    [SerializeField] private NPCSchedule _schedule;
    [SerializeField] private float _updateInterval = 1f;

    private NPC _npc;
    private NPCScheduleEntry[] _today;
    private int _activeIndex = -1;
    private float _timer;
    private PathfindingGrid _grid;

    private void Awake() {
        _npc = GetComponent<NPC>();
    }

    private void Start() {
        _grid = FindFirstObjectByType<PathfindingGrid>();
        UpdateTodaySchedule();
    }

    private void Update() {
        _timer += Time.deltaTime;
        if (_timer >= _updateInterval) {
            _timer = 0f;
            EvaluateSchedule();
        }
    }

    private void UpdateTodaySchedule() {
        int weekday = TimeManager.Instance.CurrentDate.Day % TimeManager.DAYS_PER_WEEK;
        _today = _schedule.GetTodaySchedule(weekday);
    }

    private void EvaluateSchedule() {
        float now = GameManager.Instance.TimeManager.LocalTime;
        int day = GameManager.Instance.TimeManager.CurrentDate.Day % TimeManager.DAYS_PER_WEEK;
        _today = _schedule.GetTodaySchedule(day);

        for (int i = 0; i < _today.Length; i++) {
            var entry = _today[i];
            float leaveTime = GetDepartureTime(entry);

            if (now >= leaveTime && now < entry.DepartureTimeInSeconds) {
                if (_activeIndex != i) {
                    _activeIndex = i;

                    if (ShouldSkipEntry(entry)) {
                        Debug.Log($"{_npc.Definition.NPCName} decided to skip {entry.Label} due to personality traits.");
                        continue;
                    }

                    bool run = now > entry.ArrivalTimeInSeconds;
                    GoTo(entry, run);
                }
                break;
            }
        }
    }

    private bool ShouldSkipEntry(NPCScheduleEntry entry) {
        float chance = entry.Activity switch {
            NPCScheduleEntry.ActivityType.Work   => 1f - _npc.Definition.WorkEthic,
            NPCScheduleEntry.ActivityType.Social => 1f - _npc.Definition.Sociability,
            NPCScheduleEntry.ActivityType.Sleep  => 1f - _npc.Definition.Punctuality,
            NPCScheduleEntry.ActivityType.Wander => 1f - _npc.Definition.Independence,
            NPCScheduleEntry.ActivityType.Eat    => 0.1f,
            _ => 0f,
        };
        return Random.value < chance;
    }

    /*
    private void ReplaceWithAlternativeActivity() {
        // Example: NPC decides to wander instead
        Vector3 randomLocation = GetRandomLocation();
        var mover = GetComponent<NPCMovementController>();
        mover.MoveTo(randomLocation, false);
        Debug.Log($"{_npc.Definition.NPCName} is wandering to a new location.");
    }
    */

    private float GetDepartureTime(NPCScheduleEntry entry) {
        // Use the new Unity API instead of FindObjectOfType
        var mover = GetComponent<NPCMovementController>();
        float speed = mover != null ? mover.WalkSpeed : 1.5f;
        float estimate = _grid.EstimateTravelTimeInSeconds(transform.position, entry.Location, speed);
        return entry.ArrivalTimeInSeconds - estimate;
    }

    private void GoTo(NPCScheduleEntry entry, bool run) {
        var mover = GetComponent<NPCMovementController>();
        mover.MoveTo(entry.Location, run);
        Debug.Log($"{_npc.Definition.NPCName} going to {entry.Label} {(run ? "running" : "walking")}");
    }
}
