using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

[RequireComponent(typeof(NetworkObject))]
public class TimeManager : NetworkBehaviour, IDataPersistance {
    public enum ShortDayName { Mon, Tue, Wed, Thu, Fri, Sat, Sun }
    public enum TimeOfDay { Morning, Noon, Afternoon, Evening, Night }
    public enum SeasonName { Spring, Summer, Autumn, Winter }

    public event Action OnNextDayStarted;
    public event Action<int> OnNextSeasonStarted;
    public event Action<int, int> OnUpdateUITime;
    public event Action<int, int, int> OnUpdateUIDate;

    [Header("Time constants")]
    [SerializeField] private float _timeScale = 60f;

    const int TOTAL_SECONDS_IN_A_DAY = 86400;
    const int TIME_TO_WAKE_UP = 21600; // 6 AM
    const int TIME_TO_SLEEP = 7200;    // 2 AM next day
    const int MINUTES_TO_UPDATE_CLOCK = 10;
    const int MINUTES_TO_INVOKE_TIMEAGENTS = 10;

    public const int DAYS_PER_WEEK = 7;
    public const int DAYS_PER_SEASON = 28;
    const int SEASONS_PER_YEAR = 4;

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

    NetworkVariable<float> _currentTime = new(TIME_TO_WAKE_UP);
    public NetworkVariable<CustomDate> CurrentDate = new(new CustomDate { Day = 0, Season = 0, Year = 0 });

    bool _nextDayAvailable = false;
    private HashSet<TimeAgent> _timeAgents = new();

    float _clockUpdateTimer = 0f;
    float _agentInvokeTimer = 0f;

    public string GetDateTime => $"{CurrentDate.Value.Year + 1:D4}-{CurrentDate.Value.Season + 1:D2}-{CurrentDate.Value.Day + 1:D2} {GetHours():D2}:{GetMinutes():D2}";

    public TimeOfDay CurrentTimeOfDay => _currentTime.Value switch {
        >= 21600f and < 36000f => TimeOfDay.Morning,
        >= 36000f and < 50400f => TimeOfDay.Noon,
        >= 50400f and < 64800f => TimeOfDay.Afternoon,
        >= 64800f and < 79200f => TimeOfDay.Evening,
        _ => TimeOfDay.Night
    };

    const int TIMEAGENT_INVOKES_IN_A_DAY = 144;
    public int TotalTimeAgentInvokesThisDay { get; private set; } = 0;


    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
    }

    void Update() {
        if (IsServer) {
            ServerUpdateTime();
        }
        ClientUpdateUI();
        ClientInvokeTimeAgents();
    }

    void ServerUpdateTime() {
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

    void ClientUpdateUI() {
        if (IsServer) return;
        _clockUpdateTimer -= Time.deltaTime * _timeScale;
        if (_clockUpdateTimer <= 0f) {
            OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
            _clockUpdateTimer = MINUTES_TO_UPDATE_CLOCK * 60f;
        }
    }

    void ClientInvokeTimeAgents() {
        if (IsServer) return;
        _agentInvokeTimer -= Time.deltaTime * _timeScale;
        if (_agentInvokeTimer <= 0f) {
            InvokeTimeMinuteAgents();
            _agentInvokeTimer = MINUTES_TO_INVOKE_TIMEAGENTS * 60f;
        }
    }

    void InvokeTimeMinuteAgents() {
        TotalTimeAgentInvokesThisDay++;
        foreach (var agent in _timeAgents) {
            agent.InvokeMinute();
        }
    }

    public void SubscribeTimeAgent(TimeAgent timeAgent) {
        if (timeAgent == null || _timeAgents.Contains(timeAgent)) return;
        _timeAgents.Add(timeAgent);
    }

    public void UnsubscribeTimeAgent(TimeAgent timeAgent) {
        if (timeAgent == null) return;
        _timeAgents.Remove(timeAgent);
    }

    public void StartNextDay() {
        OnNextDayStarted?.Invoke();
        TotalTimeAgentInvokesThisDay = 0;
        ResetDayAndAdvanceTime();
        CheckAndAdvanceSeasonAndYear();
        UpdateUIAndInvokeEvents();
    }

    void ResetDayAndAdvanceTime() {
        _currentTime.Value = TIME_TO_WAKE_UP;
        var newDate = CurrentDate.Value;
        newDate.Day += 1;
        CurrentDate.Value = newDate;
        _nextDayAvailable = false;
        ResetDayAndAdvanceTimeClientRpc(NetworkManager.ServerClientId, newDate.Day, newDate.Season, newDate.Year, _currentTime.Value);
    }

    void CheckAndAdvanceSeasonAndYear() {
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
            CheckAndAdvanceSeasonClientRpc(currentDate.Day, currentDate.Season, currentDate.Year);
        }
    }

    void UpdateUIAndInvokeEvents() {
        OnNextDayStarted?.Invoke();
        OnNextSeasonStarted?.Invoke(CurrentDate.Value.Season);
    }

    void OnClientConnected(ulong clientId) {
        if (IsServer) {
            var date = CurrentDate.Value;
            ResetDayAndAdvanceTimeClientRpc(clientId, date.Day, date.Season, date.Year, _currentTime.Value);
        }
    }

    [ClientRpc]
    void ResetDayAndAdvanceTimeClientRpc(
        ulong clientId, int currentDay, int currentSeason, int currentYear, float currentTime) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            _currentTime.Value = currentTime;
            CurrentDate.Value = new CustomDate { Day = currentDay, Season = currentSeason, Year = currentYear };
            OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
            OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
        }
    }

    [ClientRpc]
    void CheckAndAdvanceSeasonClientRpc(int currentDay, int currentSeason, int currentYear) {
        CurrentDate.Value = new CustomDate { Day = currentDay, Season = currentSeason, Year = currentYear };
    }

    public void CheatStartNextDay() {
        if (IsServer) StartNextDay();
    }

    public void CheatSetTime(int hours, int minutes) {
        if (IsServer) {
            SetTime(hours, minutes);
        } else {
            CheatSetTimeServerRpc(hours, minutes);
        }
        OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
    }

    [ServerRpc(RequireOwnership = false)]
    void CheatSetTimeServerRpc(int hours, int minutes) => SetTime(hours, minutes);

    void SetTime(int hours, int minutes) => _currentTime.Value = Mathf.Clamp(hours, 0, 23) * 3600 + Mathf.Clamp(minutes, 0, 59) * 60;

    public void CheatSetDay(int day) {
        var newDate = CurrentDate.Value;
        newDate.Day = Mathf.Clamp(day, 0, DAYS_PER_SEASON - 1);
        CurrentDate.Value = newDate;
        OnUpdateUIDate?.Invoke(newDate.Day, newDate.Season, newDate.Year);
    }

    public void CheatSetSeason(int season) {
        var newDate = CurrentDate.Value;
        newDate.Season = Mathf.Clamp(season, 0, SEASONS_PER_YEAR - 1);
        CurrentDate.Value = newDate;
        OnUpdateUIDate?.Invoke(newDate.Day, newDate.Season, newDate.Year);
    }

    public void CheatSetYear(int year) {
        var newDate = CurrentDate.Value;
        newDate.Year = Mathf.Max(year, 0);
        CurrentDate.Value = newDate;
        OnUpdateUIDate?.Invoke(newDate.Day, newDate.Season, newDate.Year);
    }

    public void CheatSetDate(int day, int season, int year) {
        CurrentDate.Value = new CustomDate {
            Day = Mathf.Clamp(day, 0, DAYS_PER_SEASON - 1),
            Season = Mathf.Clamp(season, 0, SEASONS_PER_YEAR - 1),
            Year = Mathf.Max(year, 0)
        };
        OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
    }

    public void SaveData(GameData data) {
        data.CurrentDay = CurrentDate.Value.Day;
        data.CurrentSeason = CurrentDate.Value.Season;
        data.CurrentYear = CurrentDate.Value.Year;
    }

    public void LoadData(GameData data) {
        if (!IsServer) return;
        CurrentDate.Value = new CustomDate {
            Day = Mathf.Clamp(data.CurrentDay, 0, DAYS_PER_SEASON - 1),
            Season = Mathf.Clamp(data.CurrentSeason, 0, SEASONS_PER_YEAR - 1),
            Year = Mathf.Max(data.CurrentYear, 0)
        };
        OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
        OnUpdateUIDate?.Invoke(CurrentDate.Value.Day, CurrentDate.Value.Season, CurrentDate.Value.Year);
    }

    public float GetHours() => Mathf.Floor(_currentTime.Value / 3600f);

    public float GetMinutes() => Mathf.Floor((_currentTime.Value % 3600f) / 60f);

    public override void OnNetworkDespawn() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
