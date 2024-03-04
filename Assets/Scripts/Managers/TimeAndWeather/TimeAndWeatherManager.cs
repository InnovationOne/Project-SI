using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using Unity.Netcode;

// This script manages the time and weather
public class TimeAndWeatherManager : NetworkBehaviour, IDataPersistance {
    public enum ShortDayName {
        Mon, Tue, Wed, Thu, Fri, Sat, Sun,
    }

    public enum WeatherName {
        Sun, Clouds, Wind, Rain, Thunder, Snow, Event, Marriage,
    }

    public enum SeasonName {
        Spring, Summer, Autumn, Winter,
    }

    public static TimeAndWeatherManager Instance { get; private set; }

    public event Action OnNextDayStarted;
    public event Action<int> OnNextSeasonStarted;
    public event Action<int, int> OnUpdateUITime;
    public event Action<int, int, int> OnUpdateUIDate;
    public event Action<int[], int> OnUpdateUIWeather;
    public event Action<int> OnChangeRainIntensity;


    [Header("Day and night time curve")]
    [SerializeField] private Color _nightLightColor;
    [SerializeField] private Color _dayLightColor = Color.white;
    [SerializeField] private AnimationCurve _nightTimeCurve;
    [SerializeField] private Light2D _globalLight;


    [Header("Time constants")]
    [SerializeField] private float _timeScale = 60f; // Time scale for the ingame time (e.g. 60 means 1 minute ingame is equal to 1 second in real life)

    private const int TOTAL_SECONDS_IN_A_DAY = 86400;
    public const int DAYS_PER_WEEK = 7;
    public const int DAYS_PER_SEASON = 28;
    private const int SEASONS_PER_YEAR = 4;
    private const int TIME_TO_WAKE_UP = 21600;
    private const int TIME_TO_SLEEP = 7200;
    private const int MINUTES_TO_UPDATE_CLOCK = 10;


    [Header("Current time and date")]
    private float _currentTime = 21600f;
    public int CurrentDay { get; private set; } = 0;
    public int CurrentSeason { get; private set; } = 0;
    private int _currentYear = 0;
    private bool _nextDayAvailable = false;
    private bool _updatedTime = false;


    [Header("Time agents")]
    private const int MINUTES_TO_INVOKE_TIMEAGENTS = 10;
    private const int TIMEAGENT_INVOKES_IN_A_DAY = 144;
    private int _totalTimeAgentInvokesThisDay = 0;
    private bool _updatedTimeAgent = false;
    private List<TimeAgent> _timeAgents;


    [Header("Weather settings")]
    private const int LIGHT_RAN_INTENSITY = 60;
    private const int HEAVY_RAIN_INTENSITY = 100;
    [SerializeField] private int _weatherStation = 0;
    private readonly int[] _weatherProbability = { 40, 60, 85 };
    private readonly float[] _daytimeColors = { 0.9f, 0.8f, 0.5f };
    private WeatherName[] _weatherForecast = { WeatherName.Rain, WeatherName.Thunder, WeatherName.Sun };

    [Header("Thunder Settings")]
    private const float MIN_TIME_BETWEEN_THUNDER = 10f;
    private const float MAX_TIME_BETWEEN_THUNDER = 30f;
    private float _timeSinceLastThunder = 0f;
    private float _nextThunder;


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Day Time Manager in the scene.");
        } else {
            Instance = this;
        }

        _timeAgents = new List<TimeAgent>();
        _nextThunder = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnected;
        }

        AudioManager.Instance.SetMusicSeason((SeasonName)CurrentSeason);
        if (_weatherForecast[0] == WeatherName.Rain) {
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Sun);
        } else if (_weatherForecast[0] == WeatherName.Thunder) {
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Clouds);
        }

        OnUpdateUIDate?.Invoke(CurrentDay, CurrentSeason, _currentYear);
        OnUpdateUIWeather?.Invoke(new int[] { (int)_weatherForecast[0], (int)_weatherForecast[1], (int)_weatherForecast[2] }, _weatherStation);
    }

    #region Client Connect
    private void NetworkManager_OnClientConnected(ulong clientId) {
        Color.RGBToHSV(_globalLight.color, out _, out _, out float v);
        NetworkManager_OnClientConnected_ClientRpc(clientId, CurrentDay, CurrentSeason, _currentYear, v, _currentTime);
    }

    [ClientRpc]
    private void NetworkManager_OnClientConnected_ClientRpc(ulong clientId, int currentDay, int currentSeason, int currentYear, float v, float currentTime) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            _currentTime = currentTime;
            CurrentDay = currentDay;
            CurrentSeason = currentSeason;
            _currentYear = currentYear;

            OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
            OnUpdateUIDate?.Invoke(CurrentDay, CurrentSeason, _currentYear);
            OnUpdateUIWeather?.Invoke(new int[] { (int)_weatherForecast[0], (int)_weatherForecast[1], (int)_weatherForecast[2] }, _weatherStation);

            _globalLight.color = Color.HSVToRGB(0, 0, v);
        }
    }
    #endregion

    private void Update() {
        // Debug
        if (Input.GetKeyDown(KeyCode.L)) {
            StartNextDay();
        }


        // Update the time
        _currentTime += Time.deltaTime * _timeScale;
        // Check if the current time is past the time to sleep
        if (_currentTime >= TOTAL_SECONDS_IN_A_DAY) {
            _currentTime = 0f;
            _nextDayAvailable = true;
        }

        if (_currentTime >= TIME_TO_SLEEP && _nextDayAvailable) {
            // If the next day is available, start the next day
            if (IsServer) {
                //### Play sleep animation
                //### Respawn at House
                StartNextDay();
            }
        }

        // Update the time
        if (((int)GetMinutes()) % MINUTES_TO_UPDATE_CLOCK == 0 && !_updatedTime) {
            _updatedTime = true;
            OnUpdateUITime?.Invoke((int)GetHours(), (int)GetMinutes());
        } else if (((int)GetMinutes()) % MINUTES_TO_UPDATE_CLOCK != 0 && _updatedTime) {
            _updatedTime = false;
        }

        // Call Time Agents
        if (((int)GetMinutes()) % MINUTES_TO_INVOKE_TIMEAGENTS == 0 && !_updatedTimeAgent) {
            _updatedTimeAgent = true;
            InvokeTimeMinuteAgents();
        } else if (((int)GetMinutes()) % MINUTES_TO_INVOKE_TIMEAGENTS != 0 && _updatedTimeAgent) {
            _updatedTimeAgent = false;
        }

        // Thunder
        _timeSinceLastThunder += Time.deltaTime;
        if (_timeSinceLastThunder >= _nextThunder && _weatherForecast[0] == WeatherName.Thunder) {
            _timeSinceLastThunder = 0f;
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.Thunder, transform.position);
            _nextThunder = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
        }

        UpdateLightColor();
    }

    #region Time
    private void InvokeTimeMinuteAgents() {
        _totalTimeAgentInvokesThisDay++;
        // Call all TimeAgents in the game
        for (int i = 0; i < _timeAgents.Count; i++) {
            _timeAgents[i].InvokeMinute();
        }
    }

    private void UpdateLightColor() {
        // Calculate the position on the curve based on the current hour
        float curvePosition = _nightTimeCurve.Evaluate(GetHours());

        // Calculate the color by interpolating between the day and night colors based on the curve position
        Color newColor = Color.Lerp(_dayLightColor, _nightLightColor, curvePosition);

        // Update the global light color
        _globalLight.color = newColor;
    }

    private float GetHours() {
        return _currentTime / 3600f;
    }

    private float GetMinutes() {
        return _currentTime % 3600f / 60f;
    }

    public void SubscribeTimeAgent(TimeAgent timeAgent) {
        _timeAgents.Add(timeAgent);
    }

    public void UnsubscribeTimeAgent(TimeAgent timeAgent) {
        _timeAgents.Remove(timeAgent);
    }

    public void StartNextDay() {
        InvokeTimeAgentsIfNeeded();

        ResetDayAndAdvanceTime();

        CheckAndAdvanceSeasonAndYear();

        UpdateUIAndInvokeEvents();
    }


    private void InvokeTimeAgentsIfNeeded() {
        InvokeTimeAgentsIfNeededClientRpc(TIMEAGENT_INVOKES_IN_A_DAY - _totalTimeAgentInvokesThisDay);
        _totalTimeAgentInvokesThisDay = 0;
    }

    [ClientRpc]
    private void InvokeTimeAgentsIfNeededClientRpc(int remainingInvokes) {
        for (int i = 0; i < remainingInvokes; i++) {
            InvokeTimeMinuteAgents();
        }
    }

    private void ResetDayAndAdvanceTime() {
        _currentTime = TIME_TO_WAKE_UP;
        CurrentDay++;
        ResetDayAndAdvanceTimeClientRpc(CurrentDay);
        _nextDayAvailable = false;
    }

    [ClientRpc]
    private void ResetDayAndAdvanceTimeClientRpc(int currentDay) {
        CurrentDay = currentDay;
    }

    private void CheckAndAdvanceSeasonAndYear() {
        if (CurrentDay >= DAYS_PER_SEASON) {
            CurrentDay = 0;
            CurrentSeason++;
            OnNextSeasonStarted?.Invoke(CurrentSeason);

            if (CurrentSeason >= SEASONS_PER_YEAR) {
                CurrentSeason = 0;
                _currentYear++;
            }
        }

        AudioManager.Instance.SetMusicSeason((SeasonName)CurrentSeason);
        CheckAndAdvanceSeasonClientRpc(CurrentDay, CurrentSeason, _currentYear);
    }

    [ClientRpc]
    private void CheckAndAdvanceSeasonClientRpc(int currentDay, int currentSeason, int currentYear) {
        CurrentDay = currentDay;
        CurrentSeason = currentSeason;
        _currentYear = currentYear;
    }

    private void UpdateUIAndInvokeEvents() {
        GetWeatherForcast();
        UpdateUIAndInvokeEventsClientRpc(GetTodaysGlobalLightColor());

        OnNextDayStarted?.Invoke();
    }

    [ClientRpc]
    private void UpdateUIAndInvokeEventsClientRpc(float dayLightColor) {
        OnUpdateUIDate?.Invoke(CurrentDay, CurrentSeason, _currentYear);
        OnUpdateUIWeather?.Invoke(new int[] { (int)_weatherForecast[0], (int)_weatherForecast[1], (int)_weatherForecast[2] }, _weatherStation);
        _globalLight.color = Color.HSVToRGB(0, 0, dayLightColor);
    }
    #endregion

    #region Weather
    public void GetWeatherForcast() {
        _weatherForecast[0] = _weatherForecast[1];
        _weatherForecast[1] = _weatherForecast[2];

        int probability = UnityEngine.Random.Range(0, 100);
        if (probability < _weatherProbability[0]) {
            // 40% chance for sun
            _weatherForecast[2] = WeatherName.Sun;
        } else if (probability < _weatherProbability[1]) {
            // 20% chance for clouds or wind
            _weatherForecast[2] = UnityEngine.Random.Range(0, 2) == 0 ? WeatherName.Clouds : WeatherName.Wind;
        } else if (probability < _weatherProbability[2]) {
            // 25% chance for rain or snow
            _weatherForecast[2] = CurrentSeason == 3 ? WeatherName.Snow : WeatherName.Rain;
        } else {
            // 15% chance for thunder
            _weatherForecast[2] = WeatherName.Thunder;
        }

        if (_weatherForecast[0] == WeatherName.Rain) {
            OnChangeRainIntensity?.Invoke(LIGHT_RAN_INTENSITY);
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Sun);
        } else if (_weatherForecast[0] == WeatherName.Thunder) {
            OnChangeRainIntensity?.Invoke(HEAVY_RAIN_INTENSITY);
            AudioManager.Instance.SetAmbienceWeather(WeatherName.Thunder);
        } else {
            OnChangeRainIntensity?.Invoke(0);
        }
    }

    private float GetTodaysGlobalLightColor() {
        float _color;
        switch (_weatherForecast[0]) {
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


    #region Save & Load
    public void SaveData(GameData data) {
        data.CurrentDay = CurrentDay;
        data.CurrentSeason = CurrentSeason;
        data.CurrentYear = _currentYear;
        data.WeatherForecast = new int[] { (int)_weatherForecast[0], (int)_weatherForecast[1], (int)_weatherForecast[2] };
    }

    public void LoadData(GameData data) {
        CurrentDay = data.CurrentDay;
        CurrentSeason = data.CurrentSeason;
        _currentYear = data.CurrentYear;
        _weatherForecast = new WeatherName[] { (WeatherName)data.WeatherForecast[0], (WeatherName)data.WeatherForecast[1], (WeatherName)data.WeatherForecast[2] };
    }
    #endregion
}
