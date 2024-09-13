using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using Unity.Netcode;
using System.Linq;

// This script manages the time and weather
public class TimeAndWeatherManager : NetworkBehaviour, IDataPersistance {
    public enum ShortDayName { Mon, Tue, Wed, Thu, Fri, Sat, Sun }
    public enum WeatherName { Sun, Clouds, Wind, Rain, Thunder, Snow, Event, Marriage }
    public enum SeasonName { Spring, Summer, Autumn, Winter }
    public enum TimeOfDay { Morning, Noon, Afternoon, Evening, Night }

    public static TimeAndWeatherManager Instance { get; private set; }

    // Events
    public event Action OnNextDayStarted;
    public event Action<int> OnNextSeasonStarted;
    public event Action<int, int> OnUpdateUITime;
    public event Action<int, int, int> OnUpdateUIDate;
    public event Action<int[], int> OnUpdateUIWeather;
    public event Action<int> OnChangeRainIntensity;

    // Serialized Fields
    [Header("Day and night time curve")]
    [SerializeField] private Color _nightLightColor;
    [SerializeField] private Color _dayLightColor = Color.white;
    [SerializeField] private AnimationCurve _nightTimeCurve;
    [SerializeField] private Light2D _globalLight;

    [Header("Time constants")]
    [SerializeField] private float _timeScale = 60f; // Time scale for the ingame time (e.g. 60 means 1 minute ingame is equal to 1 second in real life)

    // Constants
    private const int TOTAL_SECONDS_IN_A_DAY = 86400;
    private const int TIME_TO_WAKE_UP = 21600;
    private const int TIME_TO_SLEEP = 7200;
    private const int MINUTES_TO_UPDATE_CLOCK = 10;
    private const int MINUTES_TO_INVOKE_TIMEAGENTS = 10;

    public const int DAYS_PER_WEEK = 7;
    public const int DAYS_PER_SEASON = 28;
    private const int SEASONS_PER_YEAR = 4;

    // Networked Variables
    private NetworkVariable<float> _currentTime = new NetworkVariable<float>(21600f);
    public NetworkVariable<int> CurrentDay = new NetworkVariable<int>(0);
    public NetworkVariable<int> CurrentSeason = new NetworkVariable<int>(0);
    private NetworkVariable<int> _currentYear = new NetworkVariable<int>(0);

    // Other Variables
    private bool _nextDayAvailable = false;
    private bool _updatedTime = false;
    private List<TimeAgent> _timeAgents = new List<TimeAgent>();

    public string GetDateTime => $"{_currentYear.Value + 1:D4}-{CurrentSeason.Value + 1:D2}-{CurrentDay.Value + 1:D2} {(int)GetHours():D2}:{(int)GetMinutes():D2}";


    public TimeOfDay CurrentTimeOfDay {
        get {
            if (_currentTime.Value >= 21600f && _currentTime.Value < 36000f) return TimeOfDay.Morning;
            if (_currentTime.Value >= 36000f && _currentTime.Value < 50400f) return TimeOfDay.Noon;
            if (_currentTime.Value >= 50400f && _currentTime.Value < 64800f) return TimeOfDay.Afternoon;
            if (_currentTime.Value >= 64800f && _currentTime.Value < 79200f) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }
    }

    [Header("Time agents")]    
    private const int TIMEAGENT_INVOKES_IN_A_DAY = 144;
    public int TotalTimeAgentInvokesThisDay { get; private set; } = 0;
    private bool _updatedTimeAgent = false;
    
    [Header("Weather settings")]
    private const int LIGHT_RAIN_INTENSITY = 60;
    private const int HEAVY_RAIN_INTENSITY = 100;
    [SerializeField] private int _weatherStation = 0;
    private readonly int[] _weatherProbability = { 40, 60, 85 };
    private readonly float[] _daytimeColors = { 0.9f, 0.8f, 0.5f };
    private List<WeatherName> _weatherForecast = new List<WeatherName> { WeatherName.Rain, WeatherName.Thunder, WeatherName.Sun };
    public string GetWeather => _weatherForecast[_weatherForecast.Count - 1].ToString();

    [Header("Thunder Settings")]
    private const float MIN_TIME_BETWEEN_THUNDER = 10f;
    private const float MAX_TIME_BETWEEN_THUNDER = 30f;
    private float _timeSinceLastThunder = 0f;
    private float _nextThunder;


    private void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogWarning("Multiple instances of TimeAndWeatherManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _nextThunder = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnected;
        }

        AudioManager.Instance.SetMusicSeason((SeasonName)CurrentSeason.Value);
        if (_weatherForecast.ElementAt(0) == WeatherName.Rain) {
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Sun);
        } else if (_weatherForecast.ElementAt(0) == WeatherName.Thunder) {
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Clouds);
        }

        OnUpdateUIDate?.Invoke(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
        OnUpdateUIWeather?.Invoke(new int[] { (int)_weatherForecast.ElementAt(0), (int)_weatherForecast.ElementAt(1), (int)_weatherForecast.ElementAt(2) }, _weatherStation);
    }

    #region Client Connect
    private void NetworkManager_OnClientConnected(ulong clientId) {
        Color.RGBToHSV(_globalLight.color, out _, out _, out float v);
        NetworkManager_OnClientConnected_ClientRpc(clientId, CurrentDay.Value, CurrentSeason.Value, _currentYear.Value, v, _currentTime.Value);
    }

    [ClientRpc]
    private void NetworkManager_OnClientConnected_ClientRpc(ulong clientId, int currentDay, int currentSeason, int currentYear, float v, float currentTime) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            _currentTime.Value = currentTime;
            CurrentDay.Value = currentDay;
            CurrentSeason.Value = currentSeason;
            _currentYear.Value = currentYear;

            OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
            OnUpdateUIDate?.Invoke(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
            OnUpdateUIWeather?.Invoke(new int[] {
                (int)_weatherForecast.ElementAt(0),
                (int)_weatherForecast.ElementAt(1),
                (int)_weatherForecast.ElementAt(2)
            }, _weatherStation);

            _globalLight.color = Color.HSVToRGB(0, 0, v);
        }
    }
    #endregion

    private void Update() {
        if (IsServer) {
            _currentTime.Value += Time.deltaTime * _timeScale;

            // Check if the current time is past the time to sleep
            if (_currentTime.Value >= TOTAL_SECONDS_IN_A_DAY) {
                _currentTime.Value = 0f;
                _nextDayAvailable = true;
            }

            if (_currentTime.Value >= TIME_TO_SLEEP && _nextDayAvailable) {
                StartNextDay();
            }

            // Thunder logic
            UpdateThunder();
        }

        UpdateUI();
        UpdateLightColor();
        InvokeTimeAgents();
    }

    private void UpdateUI() {
        int currentMinutes = (int)GetMinutes();
        if (currentMinutes % MINUTES_TO_UPDATE_CLOCK == 0) {
            OnUpdateUITime?.Invoke((int)GetHours(), currentMinutes);
        }
    }

    private void InvokeTimeAgents() {
        int currentMinutes = (int)GetMinutes();
        if (currentMinutes % MINUTES_TO_INVOKE_TIMEAGENTS == 0) {
            InvokeTimeMinuteAgents();
        }
    }


    private void UpdateThunder() {
        _timeSinceLastThunder += Time.deltaTime;
        if (_timeSinceLastThunder >= _nextThunder && _weatherForecast[0] == WeatherName.Thunder) {
            _timeSinceLastThunder = 0f;
            _nextThunder = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
            PlayThunderSoundClientRpc();
        }
    }

    [ClientRpc]
    private void PlayThunderSoundClientRpc() {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.Thunder, transform.position);
    }


    #region Time
    private void InvokeTimeMinuteAgents() {
        TotalTimeAgentInvokesThisDay++;
        // Call all TimeAgents in the game
        for (int i = 0; i < _timeAgents.Count; i++) {
            _timeAgents[i].InvokeMinute();
        }
    }

    private void UpdateLightColor() {
        float curvePosition = _nightTimeCurve.Evaluate(GetHours());
        Color newColor = Color.Lerp(_dayLightColor, _nightLightColor, curvePosition);
        _globalLight.color = newColor;
    }

    private float GetHours() {
        return _currentTime.Value / 3600f;
    }

    private float GetMinutes() {
        return _currentTime.Value % 3600f / 60f;
    }

    public void SubscribeTimeAgent(TimeAgent timeAgent) {
        _timeAgents.Add(timeAgent);
    }

    public void UnsubscribeTimeAgent(TimeAgent timeAgent) {
        _timeAgents.Remove(timeAgent);
    }

    private void StartNextDay() {
        InvokeTimeAgentsIfNeeded();

        ResetDayAndAdvanceTime();

        CheckAndAdvanceSeasonAndYear();

        UpdateUIAndInvokeEvents();
    }


    private void InvokeTimeAgentsIfNeeded() {
        InvokeTimeAgentsIfNeededClientRpc(TIMEAGENT_INVOKES_IN_A_DAY - TotalTimeAgentInvokesThisDay);
        TotalTimeAgentInvokesThisDay = 0;
    }

    [ClientRpc]
    private void InvokeTimeAgentsIfNeededClientRpc(int remainingInvokes) {
        for (int i = 0; i < remainingInvokes; i++) {
            InvokeTimeMinuteAgents();
        }
    }

    private void ResetDayAndAdvanceTime() {
        _currentTime.Value = TIME_TO_WAKE_UP;
        CurrentDay.Value++;
        ResetDayAndAdvanceTimeClientRpc(CurrentDay.Value);
        _nextDayAvailable = false;
    }

    [ClientRpc]
    private void ResetDayAndAdvanceTimeClientRpc(int currentDay) {
        CurrentDay.Value = currentDay;
    }

    private void CheckAndAdvanceSeasonAndYear() {
        if (CurrentDay.Value >= DAYS_PER_SEASON) {
            CurrentDay.Value = 0;
            CurrentSeason.Value++;
            OnNextSeasonStarted?.Invoke(CurrentSeason.Value);

            if (CurrentSeason.Value >= SEASONS_PER_YEAR) {
                CurrentSeason.Value = 0;
                _currentYear.Value++;
            }
        }

        AudioManager.Instance.SetMusicSeason((SeasonName)CurrentSeason.Value);
        CheckAndAdvanceSeasonClientRpc(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
    }

    [ClientRpc]
    private void CheckAndAdvanceSeasonClientRpc(int currentDay, int currentSeason, int currentYear) {
        CurrentDay.Value = currentDay;
        CurrentSeason.Value = currentSeason;
        _currentYear.Value = currentYear;
    }

    private void UpdateUIAndInvokeEvents() {
        OnNextDayStarted?.Invoke();
        GetWeatherForcast();
        ApplyWeather();
        UpdateUIAndInvokeEventsClientRpc(GetTodaysGlobalLightColor());
    }

    [ClientRpc]
    private void UpdateUIAndInvokeEventsClientRpc(float dayLightColor) {
        OnUpdateUIDate?.Invoke(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
        OnUpdateUIWeather?.Invoke(new int[] {
            (int)_weatherForecast.ElementAt(0),
            (int)_weatherForecast.ElementAt(1),
            (int)_weatherForecast.ElementAt(2)
        }, _weatherStation);
        _globalLight.color = Color.HSVToRGB(0, 0, dayLightColor);
    }
    #endregion

    #region Weather
    public void GetWeatherForcast() {
        _weatherForecast.RemoveAt(0);
        _weatherForecast.Add(DetermineNextWeather());
    }

    private WeatherName DetermineNextWeather() {
        int probability = UnityEngine.Random.Range(0, 100);
        if (probability < _weatherProbability[0]) {
            return WeatherName.Sun;
        } else if (probability < _weatherProbability[1]) {
            return UnityEngine.Random.Range(0, 2) == 0 ? WeatherName.Clouds : WeatherName.Wind;
        } else if (probability < _weatherProbability[2]) {
            return CurrentSeason.Value == (int)SeasonName.Winter ? WeatherName.Snow : WeatherName.Rain;
        } else {
            return WeatherName.Thunder;
        }
    }

    private void ApplyWeather() {
        var todayWeather = _weatherForecast[0];

        if (todayWeather == WeatherName.Rain) {
            OnChangeRainIntensity?.Invoke(LIGHT_RAIN_INTENSITY);
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Rain);
        } else if (todayWeather == WeatherName.Thunder) {
            OnChangeRainIntensity?.Invoke(HEAVY_RAIN_INTENSITY);
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Thunder);
        } else {
            OnChangeRainIntensity?.Invoke(0);
        }
    }

    private float GetTodaysGlobalLightColor() {
        float _color;
        switch (_weatherForecast.ElementAt(0)) {
            case WeatherName.Marriage:
            case WeatherName.Event:
            case WeatherName.Sun:
                _color = 1;
                break;
            case WeatherName.Clouds:
            case WeatherName.Wind:
                _color = _daytimeColors[0];
                break;
            case WeatherName.Snow:
            case WeatherName.Rain:
                _color = _daytimeColors[1];
                break;
            case WeatherName.Thunder:
                _color = _daytimeColors[2];
                break;
            default:
                Debug.LogError("Global light color cannot be set. No viable weather set.");
                return 1;
        }

        return _color;
    }
    #endregion

    #region CheatConsole
    public void CheatStartNextDay() {
        StartNextDay();
    }
    public void CheatSetTime(int hours, int minutes) {
        if (IsServer) {
            _currentTime.Value = hours * 3600 + minutes * 60;
        } else {
            CheatSetTimeServerRpc(hours, minutes);
        }
        OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
    }

    [ServerRpc]
    private void CheatSetTimeServerRpc(int hours, int minutes) {
        _currentTime.Value = hours * 3600 + minutes * 60;
    }


    public void CheatSetDay(int day) {
        CurrentDay.Value = day;
        OnUpdateUIDate?.Invoke(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
    }

    public void CheatSetSeason(int season) {
        CurrentSeason.Value = season;
        OnUpdateUIDate?.Invoke(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
    }

    public void CheatSetYear(int year) {
        _currentYear.Value = year;
        OnUpdateUIDate?.Invoke(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
    }

    public void CheatSetDate(int day, int season, int year) {
        CurrentDay.Value = day;
        CurrentSeason.Value = season;
        _currentYear.Value = year;
        OnUpdateUIDate?.Invoke(CurrentDay.Value, CurrentSeason.Value, _currentYear.Value);
    }

    public void CheatSetWeather(int weather) {
        _weatherForecast[0] = (WeatherName)weather;
        ApplyWeather();
        UpdateUIAndInvokeEventsClientRpc(GetTodaysGlobalLightColor());
    }
    #endregion

    #region Save & Load
    public void SaveData(GameData data) {
        data.CurrentDay = CurrentDay.Value;
        data.CurrentSeason = CurrentSeason.Value;
        data.CurrentYear = _currentYear.Value;
        data.WeatherForecast = _weatherForecast.ToArray().Select(w => (int)w).ToArray();
    }

    public void LoadData(GameData data) {
        if (IsServer) {
            CurrentDay.Value = data.CurrentDay;
            CurrentSeason.Value = data.CurrentSeason;
            _currentYear.Value = data.CurrentYear;
            _weatherForecast = data.WeatherForecast.Select(i => (WeatherName)i).ToList();
        }
    }
    #endregion
}
