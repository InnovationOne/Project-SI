using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Manages in-game time and date, handling synchronization across the network.
/// Utilizes Singleton pattern for global access.
/// </summary>
public class TimeManager : NetworkBehaviour, IDataPersistance {
    // Enums for time representation
    public enum ShortDayName { Mon, Tue, Wed, Thu, Fri, Sat, Sun }
    public enum TimeOfDay { Morning, Noon, Afternoon, Evening, Night }
    public enum SeasonName { Spring, Summer, Autumn, Winter }

    // Singleton Instance
    public static TimeManager Instance { get; private set; }

    // Events
    public event Action OnNextDayStarted;
    public event Action<int> OnNextSeasonStarted;
    public event Action<int, int> OnUpdateUITime;
    public event Action<int, int, int> OnUpdateUIDate;

    // Serialized Fields
    [Header("Time constants")]
    [SerializeField] private float _timeScale = 60f; // Time scale for the in-game time

    // Constants
    private const int TOTAL_SECONDS_IN_A_DAY = 86400;
    private const int TIME_TO_WAKE_UP = 21600; // 6 AM
    private const int TIME_TO_SLEEP = 7200;    // 2 AM next day
    private const int MINUTES_TO_UPDATE_CLOCK = 10;
    private const int MINUTES_TO_INVOKE_TIMEAGENTS = 10;

    public const int DAYS_PER_WEEK = 7;
    public const int DAYS_PER_SEASON = 28;
    private const int SEASONS_PER_YEAR = 4;

    // Networked Variables - Consolidated into a single struct
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

    private NetworkVariable<float> _currentTime = new NetworkVariable<float>(TIME_TO_WAKE_UP);
    public NetworkVariable<CustomDate> CurrentDate = new NetworkVariable<CustomDate>(new CustomDate { Day = 0, Season = 0, Year = 0 });

    // Other Variables
    private bool _nextDayAvailable = false;
    private readonly HashSet<TimeAgent> _timeAgents = new HashSet<TimeAgent>();

    // Cached UI update timers
    private float _clockUpdateTimer = 0f;
    private float _agentInvokeTimer = 0f;

    // Properties
    public string GetDateTime => $"{CurrentDate.Value.Year + 1:D4}-{CurrentDate.Value.Season + 1:D2}-{CurrentDate.Value.Day + 1:D2} {GetHours():D2}:{GetMinutes():D2}";

    public TimeOfDay CurrentTimeOfDay {
        get {
            return _currentTime.Value switch {
                >= 21600f and < 36000f => TimeOfDay.Morning,   // 6 AM - 10 AM
                >= 36000f and < 50400f => TimeOfDay.Noon,      // 10 AM - 2 PM
                >= 50400f and < 64800f => TimeOfDay.Afternoon, // 2 PM - 6 PM
                >= 64800f and < 79200f => TimeOfDay.Evening,   // 6 PM - 10 PM
                _ => TimeOfDay.Night                           // 10 PM - 2 AM next day
            };
        }
    }

    [Header("Time agents")]
    private const int TIMEAGENT_INVOKES_IN_A_DAY = 144;
    public int TotalTimeAgentInvokesThisDay { get; private set; } = 0;

    /// <summary>
    /// Awake for Singleton Pattern
    /// </summary>
    private void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogWarning("Multiple instances of TimeManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Initializes network-related callbacks upon spawning.
    /// </summary>
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        // Initial UI Update
        OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
    }

    #region Client Connection Handling

    /// <summary>
    /// Handles client connections by synchronizing current time and date.
    /// </summary>
    /// <param name="clientId">The ID of the connected client.</param>
    private void OnClientConnected(ulong clientId) {
        if (IsServer) {
            ResetDayAndAdvanceTimeClientRpc(clientId, CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year, _currentTime.Value);
        }
    }

    /// <summary>
    /// Client RPC to reset and synchronize time and date.
    /// </summary>
    /// <param name="clientId">The ID of the client to synchronize.</param>
    /// <param name="currentDay">Current day.</param>
    /// <param name="currentSeason">Current season.</param>
    /// <param name="currentYear">Current year.</param>
    /// <param name="currentTime">Current time in seconds.</param>
    [ClientRpc]
    private void ResetDayAndAdvanceTimeClientRpc(ulong clientId, int currentDay, int currentSeason, int currentYear, float currentTime) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            _currentTime.Value = currentTime;
            CurrentDate.Value = new CustomDate { Day = currentDay, Season = currentSeason, Year = currentYear };

            OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
            OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
        }
    }

    #endregion

    #region Update Loop

    /// <summary>
    /// Updates time and UI every frame.
    /// </summary>
    private void Update() {
        if (IsServer) {
            ServerUpdateTime();
            _currentTime.Value += Time.deltaTime * _timeScale;
        }

        ClientUpdateUI();
        ClientInvokeTimeAgents();
    }

    /// <summary>
    /// Server-side time update handling.
    /// </summary>
    private void ServerUpdateTime() {
        float deltaTime = Time.deltaTime * _timeScale;
        _currentTime.Value += deltaTime;
        _clockUpdateTimer += deltaTime;
        _agentInvokeTimer += deltaTime;

        if (_currentTime.Value >= TOTAL_SECONDS_IN_A_DAY) {
            _currentTime.Value = 0f;
            _nextDayAvailable = true;
        }

        if (_currentTime.Value >= TIME_TO_SLEEP && _nextDayAvailable) {
            StartNextDay();
        }
    }

    /// <summary>
    /// Client-side UI update handling.
    /// </summary>
    private void ClientUpdateUI() {
        if (!IsServer) {
            _clockUpdateTimer -= Time.deltaTime * _timeScale;

            if (_clockUpdateTimer <= 0f) {
                OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
                _clockUpdateTimer = MINUTES_TO_UPDATE_CLOCK * 60f;
            }
        }
    }

    /// <summary>
    /// Client-side invocation of time agents.
    /// </summary>
    private void ClientInvokeTimeAgents() {
        if (!IsServer) {
            _agentInvokeTimer -= Time.deltaTime * _timeScale;

            if (_agentInvokeTimer <= 0f) {
                InvokeTimeMinuteAgents();
                _agentInvokeTimer = MINUTES_TO_INVOKE_TIMEAGENTS * 60f;
            }
        }
    }

    #endregion

    #region Time Management

    /// <summary>
    /// Invokes minute-based actions for all subscribed TimeAgents.
    /// </summary>
    private void InvokeTimeMinuteAgents() {
        TotalTimeAgentInvokesThisDay++;
        foreach (var agent in _timeAgents) {
            agent.InvokeMinute();
        }
    }

    /// <summary>
    /// Subscribes a TimeAgent to receive time updates.
    /// </summary>
    /// <param name="timeAgent">The TimeAgent to subscribe.</param>
    public void SubscribeTimeAgent(TimeAgent timeAgent) {
        if ((timeAgent == null) || _timeAgents.Contains(timeAgent)) {
            return;
        }
        _timeAgents.Add(timeAgent);
    }

    /// <summary>
    /// Unsubscribes a TimeAgent from receiving time updates.
    /// </summary>
    /// <param name="timeAgent">The TimeAgent to unsubscribe.</param>
    public void UnsubscribeTimeAgent(TimeAgent timeAgent) {
        if (timeAgent == null) {
            return;
        }
        _timeAgents.Remove(timeAgent);
    }

    /// <summary>
    /// Initiates the transition to the next day.
    /// </summary>
    private void StartNextDay() {
        InvokeTimeAgentsIfNeeded();
        ResetDayAndAdvanceTime();
        CheckAndAdvanceSeasonAndYear();
        UpdateUIAndInvokeEvents();
    }

    /// <summary>
    /// Invokes necessary events before starting the next day.
    /// </summary>
    private void InvokeTimeAgentsIfNeeded() {
        OnNextDayStarted?.Invoke();
        TotalTimeAgentInvokesThisDay = 0;
    }

    /// <summary>
    /// Resets the day count and advances the time to wake-up time.
    /// </summary>
    private void ResetDayAndAdvanceTime() {
        _currentTime.Value = TIME_TO_WAKE_UP;
        var newDate = CurrentDate.Value;
        newDate.Day += 1;
        CurrentDate.Value = newDate;
        _nextDayAvailable = false;

        // Update clients with new day
        ResetDayAndAdvanceTimeClientRpc(NetworkManager.ServerClientId, CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year, _currentTime.Value);
    }

    /// <summary>
    /// Checks and advances the season and year if necessary.
    /// </summary>
    private void CheckAndAdvanceSeasonAndYear() {
        var currentDate = CurrentDate.Value;
        if (currentDate.Day >= DAYS_PER_SEASON) {
            currentDate.Day = 0;
            currentDate.Season += 1;
            OnNextSeasonStarted?.Invoke(currentDate.Season);

            if (currentDate.Season >= SEASONS_PER_YEAR) {
                currentDate.Season = 0;
                currentDate.Year += 1;
            }

            CurrentDate.Value = currentDate;

            // Update clients with new season and year
            CheckAndAdvanceSeasonClientRpc(currentDate.Day, currentDate.Season, currentDate.Year);
        }
    }

    /// <summary>
    /// Client RPC to synchronize season and year.
    /// </summary>
    /// <param name="currentDay">Current day.</param>
    /// <param name="currentSeason">Current season.</param>
    /// <param name="currentYear">Current year.</param>
    [ClientRpc]
    private void CheckAndAdvanceSeasonClientRpc(int currentDay, int currentSeason, int currentYear) {
        CurrentDate.Value = new CustomDate { Day = currentDay, Season = currentSeason, Year = currentYear };
    }

    /// <summary>
    /// Updates UI elements and invokes related events.
    /// </summary>
    private void UpdateUIAndInvokeEvents() {
        OnNextDayStarted?.Invoke();
        OnNextSeasonStarted?.Invoke(CurrentDate.Value.Season);
    }

    #endregion

    #region Cheat Console

    /// <summary>
    /// Forces the start of the next day via cheat command.
    /// </summary>
    public void CheatStartNextDay() {
        if (IsServer) {
            StartNextDay();
        }
    }

    /// <summary>
    /// Sets the in-game time via cheat command.
    /// </summary>
    /// <param name="hours">Hour component.</param>
    /// <param name="minutes">Minute component.</param>
    public void CheatSetTime(int hours, int minutes) {
        if (IsServer) {
            SetTime(hours, minutes);
        } else {
            CheatSetTimeServerRpc(hours, minutes);
        }
        OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
    }

    /// <summary>
    /// Server RPC to set the time.
    /// </summary>
    /// <param name="hours">Hour component.</param>
    /// <param name="minutes">Minute component.</param>
    [ServerRpc(RequireOwnership = false)]
    private void CheatSetTimeServerRpc(int hours, int minutes) {
        SetTime(hours, minutes);
    }

    /// <summary>
    /// Sets the current time.
    /// </summary>
    /// <param name="hours">Hour component.</param>
    /// <param name="minutes">Minute component.</param>
    private void SetTime(int hours, int minutes) {
        _currentTime.Value = Mathf.Clamp(hours, 0, 23) * 3600 + Mathf.Clamp(minutes, 0, 59) * 60;
    }

    /// <summary>
    /// Sets the current day via cheat command.
    /// </summary>
    /// <param name="day">Day to set.</param>
    public void CheatSetDay(int day) {
        var newDate = CurrentDate.Value;
        newDate.Day = Mathf.Clamp(day, 0, DAYS_PER_SEASON - 1);
        CurrentDate.Value = newDate;
        OnUpdateUIDate?.Invoke(newDate.Day, newDate.Season, newDate.Year);
    }

    /// <summary>
    /// Sets the current season via cheat command.
    /// </summary>
    /// <param name="season">Season to set.</param>
    public void CheatSetSeason(int season) {
        var newDate = CurrentDate.Value;
        newDate.Season = Mathf.Clamp(season, 0, SEASONS_PER_YEAR - 1);
        CurrentDate.Value = newDate;
        OnUpdateUIDate?.Invoke(newDate.Day, newDate.Season, newDate.Year);
    }

    /// <summary>
    /// Sets the current year via cheat command.
    /// </summary>
    /// <param name="year">Year to set.</param>
    public void CheatSetYear(int year) {
        var newDate = CurrentDate.Value;
        newDate.Year = Mathf.Max(year, 0);
        CurrentDate.Value = newDate;
        OnUpdateUIDate?.Invoke(newDate.Day, newDate.Season, newDate.Year);
    }

    /// <summary>
    /// Sets the full date via cheat command.
    /// </summary>
    /// <param name="day">Day to set.</param>
    /// <param name="season">Season to set.</param>
    /// <param name="year">Year to set.</param>
    public void CheatSetDate(int day, int season, int year) {
        CurrentDate.Value = new CustomDate {
            Day = Mathf.Clamp(day, 0, DAYS_PER_SEASON - 1),
            Season = Mathf.Clamp(season, 0, SEASONS_PER_YEAR - 1),
            Year = Mathf.Max(year, 0)
        };
        OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
    }

    #endregion

    #region Save & Load

    /// <summary>
    /// Saves the current date data.
    /// </summary>
    /// <param name="data">GameData object to save into.</param>
    public void SaveData(GameData data) {
        data.CurrentDay = CurrentDate.Value.Day;
        data.CurrentSeason = CurrentDate.Value.Season;
        data.CurrentYear = CurrentDate.Value.Year;
    }

    /// <summary>
    /// Loads the saved date data.
    /// </summary>
    /// <param name="data">GameData object to load from.</param>
    /// <param name="data">GameData object to load from.</param>
    public void LoadData(GameData data) {
        if (IsServer) {
            CurrentDate.Value = new CustomDate {
                Day = Mathf.Clamp(data.CurrentDay, 0, DAYS_PER_SEASON - 1),
                Season = Mathf.Clamp(data.CurrentSeason, 0, SEASONS_PER_YEAR - 1),
                Year = Mathf.Max(data.CurrentYear, 0)
            };

            OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
            OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Retrieves the current hour.
    /// </summary>
    public float GetHours() => Mathf.Floor(_currentTime.Value / 3600f);

    /// <summary>
    /// Retrieves the current minute.
    /// </summary>
    public float GetMinutes() => Mathf.Floor((_currentTime.Value % 3600f) / 60f);

    #endregion

    /// <summary>
    /// Cleans up event subscriptions when destroyed.
    /// </summary>
    private void OnDestroy() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
