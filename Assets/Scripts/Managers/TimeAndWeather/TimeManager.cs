using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using FMODUnity;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

/// <summary>
/// Responsible for in-game time progression, day-night cycle, TimeAgent invocations,
/// church bell ringing, and networking.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class TimeManager : NetworkBehaviour, IDataPersistance {
    public static TimeManager Instance { get; private set; }

    public enum ShortDayName { Mon, Tue, Wed, Thu, Fri, Sat, Sun }
    public enum DateSuffix { st, nd, rd, th }
    public enum TimeOfDay { Morning, Noon, Afternoon, Evening, Night }
    public enum SeasonName { Spring, Summer, Autumn, Winter }

    #region -------------------- Inspector_Fields --------------------

    [Header("Data Loading/Saving")]
    [SerializeField] bool _loadData = true;
    [SerializeField] bool _saveData = true;

    [Header("Time Sync Settings")]
    [Tooltip("In-game seconds between each time sync to clients.")]
    [SerializeField] float _syncIntervalSeconds = 300f;  // 5 minutes (5 * 60)

    [Tooltip("In-game seconds between TimeAgent invokes.")]
    [SerializeField] float _timeAgentIntervalSeconds = 300f; // 5 minutes

    [Header("Time constants")]
    [Tooltip("Scale real-time seconds into game-time seconds.")]
    [SerializeField] float _timeScale = 60f; // 1 real second = 1 in-game minute by default.

    [SerializeField] Transform LightsRoot;

    [Header("Day Light")]
    [SerializeField] Light2D DayLight;
    [SerializeField] Gradient DayLightGradient;

    [Header("Night Light")]
    [SerializeField] Light2D NightLight;
    [SerializeField] Gradient NightLightGradient;

    [Header("Ambient Light")]
    [SerializeField] Light2D AmbientLight;
    [SerializeField] Gradient AmbientLightGradient;

    [Header("RimLights")]
    [SerializeField] Light2D DayLightRim;
    [SerializeField] Gradient SunRimLightGradient;
    [SerializeField] Light2D NightLightRim;
    [SerializeField] Gradient MoonRimLightGradient;

    [Tooltip("The angle 0 = upward, going clockwise to 1 along the day")]
    [SerializeField] AnimationCurve ShadowAngle;
    [Tooltip("The scale of the normal shadow length (0 to 1) along the day")]
    [SerializeField] AnimationCurve ShadowLength;

    List<ShadowInstance> _shadows = new();
    List<LightInterpolator> _lightBlenders = new();

    [Header("Church Audio")]
    [Tooltip("FMOD Emitter for playing bell sounds.")]
    [SerializeField] StudioEventEmitter _churchAudio;

    #endregion -------------------- Inspector_Fields --------------------

    // Time variables:
    float _localTime = 21600f; // 6 AM
    public float LocalTime => _localTime;
    readonly NetworkVariable<float> _networkTime = new(
        21600f, // default 6 AM
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Serializable]
    public struct CustomDate : INetworkSerializable {
        public int Day;
        public int Season;
        public int Year;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref Day);
            serializer.SerializeValue(ref Season);
            serializer.SerializeValue(ref Year);
        }
    }

    CustomDate _currentDate = new() { Day = 0, Season = 0, Year = 0 };
    readonly NetworkVariable<CustomDate> _networkDate = new(
        new CustomDate { Day = 0, Season = 0, Year = 0 },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Day/season/agents
    bool _nextDayAvailable = false;
    float _timeSinceLastSync = 0f;
    float _timeSinceLastAgentInvoke = 0f;
    public int TotalTimeAgentInvokesThisDay { get; private set; } = 0;

    // Timed constants (one day = 24 hours * 3600 = 86400s)
    const int TOTAL_SECONDS_IN_A_DAY = 86400;
    const int TIME_TO_WAKE_UP = 21600; // 6 AM
    const int TIME_TO_SLEEP = 7200;    // 2 AM next day
    public const int DAYS_PER_WEEK = 7;
    public const int DAYS_PER_SEASON = 28;
    public const int SEASONS_PER_YEAR = 4;

    // Bells logic
    readonly int[] CHURCH_AUDIO_HOURS = { 6, 12, 18 };
    int _lastBellsHour = -1;
    int _lastBellsMinute = -1;

    // TimeAgents
    HashSet<TimeAgent> _timeAgents = new();

    // Events
    public event Action OnNextDayStarted;
    public event Action<int> OnNextSeasonStarted;       // season
    public event Action<int, int> OnUpdateUITime;       // hour, minute
    public event Action<int, int, int, int> OnUpdateUIDate;  // day, season, year, old season


    #region -------------------- Unity_Callbacks --------------------

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of TimeManager in the scene!");
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        // Listen for time/date changes on clients
        _networkTime.OnValueChanged += OnNetworkTimeChanged;
        _networkDate.OnValueChanged += OnNetworkDateChanged;

        // Initialize server values
        if (IsServer) {
            _networkTime.Value = _localTime;
            _networkDate.Value = _currentDate;
        }
    }

    void Update() {
        if (!IsServer) return;

        float delta = Time.deltaTime * _timeScale;
        _localTime += delta;

        // Check day rollover
        if (_localTime >= TOTAL_SECONDS_IN_A_DAY) {
            _localTime = 0;
            _nextDayAvailable = true;
        }

        // Check if we can roll over into next day.
        if (_localTime >= TIME_TO_SLEEP && _nextDayAvailable) {
            StartNextDay();
        }

        // Sync to clients periodically
        _timeSinceLastSync += delta;
        if (_timeSinceLastSync >= _syncIntervalSeconds) {
            _timeSinceLastSync = 0f;
            _networkTime.Value = _localTime;
            _networkDate.Value = _currentDate;
        }

        // Invoke TimeAgents periodically
        _timeSinceLastAgentInvoke += delta;
        if (_timeSinceLastAgentInvoke >= _timeAgentIntervalSeconds) {
            _timeSinceLastAgentInvoke = 0f;
            InvokeTimeAgents();
        }

        // Check for bells
        CheckChurchBells();

        // Update day-night lighting
        UpdateLight();
    }

    public override void OnNetworkDespawn() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        base.OnNetworkDespawn();
    }

    #endregion -------------------- Unity_Callbacks --------------------

    #region -------------------- NetworkVariable_Callbacks --------------------

    void OnNetworkTimeChanged(float oldVal, float newVal) {
        OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
    }

    void OnNetworkDateChanged(CustomDate oldVal, CustomDate newVal) {
        OnUpdateUIDate?.Invoke(newVal.Day, newVal.Season, newVal.Year, oldVal.Season);
    }

    #endregion -------------------- NetworkVariable_Callbacks --------------------

    #region -------------------- Server_Logic --------------------

    void OnClientConnected(ulong clientId) {
        if (IsServer) {
            // Immediately push current time/date to that client
            _networkTime.Value = _localTime;
            _networkDate.Value = _currentDate;
        }
    }

    // Wraps up the current day, ensures any missing agent invocations occur, then starts the next day at 6 AM.
    public void StartNextDay() {
        // Before rolling to next day, invoke any missing ticks from the previous day
        int totalInvokesPerDay = (int)(TOTAL_SECONDS_IN_A_DAY / _timeAgentIntervalSeconds);
        int missingInvokes = totalInvokesPerDay - TotalTimeAgentInvokesThisDay;
        for (int i = 0; i < missingInvokes; i++) {
            InvokeTimeAgents();
        }

        OnNextDayStarted?.Invoke();
        TotalTimeAgentInvokesThisDay = 0;

        _localTime = TIME_TO_WAKE_UP;
        _nextDayAvailable = false;

        _lastBellsHour = -1;
        _lastBellsMinute = -1;

        _currentDate.Day++;
        // Check if we need to roll over the season.
        if (_currentDate.Day >= DAYS_PER_SEASON) {
            _currentDate.Day = 0;
            _currentDate.Season++;
            OnNextSeasonStarted?.Invoke(_currentDate.Season);

            // Check if we roll the year.
            if (_currentDate.Season >= SEASONS_PER_YEAR) {
                _currentDate.Season = 0;
                _currentDate.Year++;
            }
        }

        // Sync changes
        _networkTime.Value = _localTime;
        _networkDate.Value = _currentDate;
    }

    // Rings bells once per day at 6, 12, and 18 hours exactly.
    void CheckChurchBells() {
        int currentHour = Mathf.FloorToInt(GetHours());
        int currentMinute = Mathf.FloorToInt(GetMinutes());

        if (currentMinute == 0) {
            foreach (int h in CHURCH_AUDIO_HOURS) {
                if (currentHour == h && (currentHour != _lastBellsHour || currentMinute != _lastBellsMinute)) {
                    RingBellsClientRpc();
                    _lastBellsHour = currentHour;
                    _lastBellsMinute = currentMinute;
                    return;
                }
            }
        }
    }

    void InvokeTimeAgents() {
        TotalTimeAgentInvokesThisDay++;
        foreach (var agent in _timeAgents) {
            agent.InvokeMinute();
        }
    }

    [ClientRpc]
    void RingBellsClientRpc() {
        if (_churchAudio == null) return;
        _churchAudio.Stop();
        _churchAudio.Play();
    }

    #endregion -------------------- Server_Logic --------------------

    #region -------------------- DayNight_Cycle --------------------

    // Blends between day and night color, optionally factoring in weather brightness.
    public void UpdateLight(float ratio = -1f) {
        if (ratio < 0) ratio = _localTime / TOTAL_SECONDS_IN_A_DAY;

        DayLight.color = DayLightGradient.Evaluate(ratio);
        NightLight.color = NightLightGradient.Evaluate(ratio);

        AmbientLight.color = AmbientLightGradient.Evaluate(ratio);
        DayLightRim.color = SunRimLightGradient.Evaluate(ratio);
        NightLightRim.color = MoonRimLightGradient.Evaluate(ratio);

        LightsRoot.rotation = Quaternion.Euler(0, 0, 360.0f * ratio);

        UpdateShadow(ratio);
    }

    void UpdateShadow(float ratio) {
        var currentShadowAngle = ShadowAngle.Evaluate(ratio);
        var currentShadowLength = ShadowLength.Evaluate(ratio);

        var opposedAngle = currentShadowAngle + 0.5f;
        while (currentShadowAngle > 1.0f) currentShadowAngle -= 1.0f;


        foreach (var shadow in _shadows) {
            var t = shadow.transform;
            //use 1.0-angle so that the angle goes clo
            t.eulerAngles = new Vector3(0, 0, currentShadowAngle * 360.0f);
            t.localScale = new Vector3(1, 1f * shadow.BaseLength * currentShadowLength, 1);
        }

        foreach (var handler in _lightBlenders) {
            handler.SetRatio(ratio);
        }
    }

    public void RegisterShadow(ShadowInstance shadow) {
        _shadows.Add(shadow);
    }

    public void UnregisterShadow(ShadowInstance shadow) {
        _shadows.Remove(shadow);
    }

    public void RegisterLightBlender(LightInterpolator handler) {
        _lightBlenders.Add(handler);
    }

    public void UnregisterLightBlender(LightInterpolator handler) {
        _lightBlenders.Remove(handler);
    }

    #endregion -------------------- DayNight_Cycle --------------------

    #region -------------------- Client_Subscriptions --------------------

    public void SubscribeTimeAgent(TimeAgent agent) {
        if (agent == null || _timeAgents.Contains(agent)) return;
        _timeAgents.Add(agent);
    }

    public void UnsubscribeTimeAgent(TimeAgent agent) {
        if (agent == null) return;
        _timeAgents.Remove(agent);
    }

    #endregion -------------------- Client_Subscriptions --------------------

    #region -------------------- Debug_Cheat --------------------

    [ServerRpc(RequireOwnership = false)]
    public void CheatSetTimeServerRpc(int hours, int minutes) {
        int h = Mathf.Clamp(hours, 0, 23);
        int m = Mathf.Clamp(minutes, 0, 59);
        _localTime = (h * 3600) + (m * 60);
        _networkTime.Value = _localTime;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheatSetDayServerRpc(int day) {
        _currentDate.Day = Mathf.Clamp(day, 0, DAYS_PER_SEASON - 1);
        _networkDate.Value = _currentDate;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheatSetSeasonServerRpc(int season) {
        _currentDate.Season = Mathf.Clamp(season, 0, SEASONS_PER_YEAR - 1);
        _networkDate.Value = _currentDate;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheatSetYearServerRpc(int year) {
        _currentDate.Year = Mathf.Max(year, 0);
        _networkDate.Value = _currentDate;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheatStartNextDayServerRpc() {
        StartNextDay();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheatStartNextSeasonServerRpc() {
        int remainingDays = DAYS_PER_SEASON - _currentDate.Day;
        for (int i = 0; i < remainingDays; i++) {
            StartNextDay();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheatStartNextYearServerRpc() {
        int remainingDays = (SEASONS_PER_YEAR * DAYS_PER_SEASON) - (_currentDate.Season * DAYS_PER_SEASON + _currentDate.Day);
        for (int i = 0; i < remainingDays; i++) {
            StartNextDay();
        }
    }

    #endregion -------------------- Debug_Cheat --------------------

    #region -------------------- Data_Persistance --------------------

    public void SaveData(GameData data) {
        if (!_saveData) return;
        data.CurrentDay = _currentDate.Day;
        data.CurrentSeason = _currentDate.Season;
        data.CurrentYear = _currentDate.Year;
    }

    public void LoadData(GameData data) {
        if (!IsServer || !_loadData) return;
        _currentDate.Day = Mathf.Clamp(data.CurrentDay, 0, DAYS_PER_SEASON - 1);
        _currentDate.Season = Mathf.Clamp(data.CurrentSeason, 0, SEASONS_PER_YEAR - 1);
        _currentDate.Year = Mathf.Max(data.CurrentYear, 0);

        _networkTime.Value = _localTime;
        _networkDate.Value = _currentDate;
    }

    #endregion -------------------- Data_Persistance --------------------

    #region -------------------- Helper_Methode --------------------

    public float GetHours() {
        float reference = IsServer ? _localTime : _networkTime.Value;
        return Mathf.Floor(reference / 3600f);
    }

    public float GetMinutes() {
        float reference = IsServer ? _localTime : _networkTime.Value;
        return Mathf.Floor((reference % 3600f) / 60f);
    }

    public TimeOfDay CurrentTimeOfDay {
        get {
            float reference = IsServer ? _localTime : _networkTime.Value;
            return reference switch {
                >= 21600f and < 36000f => TimeOfDay.Morning,   // 6 AM - 10 AM
                >= 36000f and < 50400f => TimeOfDay.Noon,      // 10 AM - 2 PM
                >= 50400f and < 64800f => TimeOfDay.Afternoon, // 2 PM - 6 PM
                >= 64800f and < 79200f => TimeOfDay.Evening,   // 6 PM - 10 PM
                _ => TimeOfDay.Night
            };
        }
    }

    public CustomDate CurrentDate => IsServer ? _currentDate : _networkDate.Value;

    #endregion -------------------- Helper_Methode --------------------

#if UNITY_EDITOR
    [CustomEditor(typeof(TimeManager))]
    class DayCycleEditor : Editor {
        TimeManager _manager;

        public override VisualElement CreateInspectorGUI() {
            _manager = target as TimeManager;
            var root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var slider = new Slider(0.0f, 1.0f) {
                label = "Test time 0:00"
            };
            slider.RegisterValueChangedCallback(evt => {
                _manager.UpdateLight(evt.newValue);

                slider.label = $"Test Time {GetTimeAsString(evt.newValue)} ({evt.newValue:F2})";
                SceneView.RepaintAll();
            });

            root.RegisterCallback<ClickEvent>(evt => {
                _manager.UpdateLight(slider.value);
                SceneView.RepaintAll();
            });

            root.Add(slider);

            return root;
        }

        string GetTimeAsString(float ratio) {
            var time = ratio * 24.0f;
            var hour = Mathf.FloorToInt(time);
            var minute = Mathf.FloorToInt((time - Mathf.FloorToInt(time)) * 60.0f);
            return $"{hour:D2}:{minute:D2}";
        }
    }
#endif
}

