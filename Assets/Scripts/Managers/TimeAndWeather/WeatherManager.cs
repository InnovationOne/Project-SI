using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using static TimeManager;

/// <summary>
/// Manages weather conditions, forecasts, and associated audio/visual effects in the game.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class WeatherManager : NetworkBehaviour, IDataPersistance {
    public enum WeatherName { Sun, Cloudy, Wind, Rain, Thunder, Snow, Event, Wedding, None }

    // Event to update UI weather forecast (sends an array of weather indices).
    public event Action<int[]> OnUpdateUIWeather;
    // Event to update UI rain intensity.
    public event Action<int> OnChangeRainIntensity;
    // Event to notify clients of a thunder strike.
    public event Action OnThunderStrike;

    #region -------------------- Inspector_Fields --------------------

    [Header("Data Loading/Saving")]
    [SerializeField] bool _loadData = true;
    [SerializeField] bool _saveData = true;

    [Header("Weather Settings")]
    [SerializeField] int _weatherStation = 0;

    // Constants for thunder timing.
    const float MIN_TIME_BETWEEN_THUNDER = 10f;
    const float MAX_TIME_BETWEEN_THUNDER = 30f;

    #endregion -------------------- Inspector_Fields --------------------

    // Base and current weather probabilities (indices: 0=Sun, 1=Clouds/Wind, 2=Rain/Snow, 3=Thunder).
    readonly float[] _baseWeatherProbability = { 0.4f, 0.2f, 0.25f, 0.15f };
    readonly float[] _currentWeatherProbability = new float[4];

    // Seasonal adjustments.
    const float SUMMER_SUN_BOOST = 0.2f;
    const float FALL_WIND_BOOST = 0.2f;
    const float WINTER_SNOW_BOOST = 0.3f;

    // Light reduction factors.
    const float CLOUDS_WIND_LIGHT = 0.9f;
    const float RAIN_SNOW_LIGHT = 0.8f;
    const float THUNDER_LIGHT = 0.5f;

    // Weather forecast (first element is the current weather).
    NetworkList<int> _weatherForecast = new(new List<int> { (int)WeatherName.Thunder, (int)WeatherName.Thunder, (int)WeatherName.Rain },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Gets the current weather.
    public WeatherName CurrentWeather => _weatherForecast.Count > 0 ? (WeatherName)_weatherForecast[0] : WeatherName.Sun;

    // Thunder timing logic.
    public bool IsWeatherPlaying;
    float _timeSinceLastThunder = 0f;
    float _nextThunderTime;

    // Rain intensity constants.
    const int LIGHT_RAIN_INTENSITY = 60;
    const int HEAVY_RAIN_INTENSITY = 100;

    // External dependencies retrieved from GameManager.
    TimeManager _timeManager;
    AudioManager _audioManager;
    FMODEvents _fmodEvents;

    // Caches the last updated time-of-day for ambience adjustments.
    private TimeOfDay _lastAmbienceUpdate;

    #region -------------------- Unity Lifecycle --------------------

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        _timeManager = GameManager.Instance.TimeManager;
        _audioManager = GameManager.Instance.AudioManager;
        _fmodEvents = GameManager.Instance.FMODEvents;

        if (IsServer) {
            InitializeThunderTimer();
            _timeManager.OnNextDayStarted += HandleNextDayStarted;
            _timeManager.OnNextSeasonStarted += HandleNextSeasonStarted;
            InitializeWeatherProbabilities();
        }

        _weatherForecast.OnListChanged += OnWeatherForecastChanged;
        InitializeAudio();
        UpdateUIWeather();
    }

    void Update() {
        if (IsServer) {
            UpdateThunder();
        }
        CheckForTimeOfDayUpdate();
    }

    public override void OnNetworkDespawn() {
        if (IsServer) {
            _timeManager.OnNextDayStarted -= HandleNextDayStarted;
            _timeManager.OnNextSeasonStarted -= HandleNextSeasonStarted;
        }
        _weatherForecast.OnListChanged -= OnWeatherForecastChanged;
        base.OnNetworkDespawn();
    }

    #endregion -------------------- Unity Lifecycle --------------------

    #region -------------------- Weather Forecast & Application --------------------

    // Called when the forecast list changes
    void OnWeatherForecastChanged(NetworkListEvent<int> changeEvent) {
        ApplyWeather();
        UpdateUIWeather();
    }

    // Handles a new day by advancing the forecast.
    void HandleNextDayStarted() {
        AdvanceWeatherForecast();
        ApplyWeather();
        UpdateUIWeather();
    }

    // Handles a new season by updating audio and weather probabilities.
    void HandleNextSeasonStarted(int newSeason) {
        var season = (SeasonName)newSeason;
        _audioManager.SetMusicSeason(season);
        InitializeWeatherProbabilities();
        ApplyWeather();
        UpdateUIWeather();
    }

    // Advances the forecast by removing the oldest weather and adding a new prediction.
    void AdvanceWeatherForecast() {
        if (_weatherForecast.Count > 0) {
            _weatherForecast.RemoveAt(0);
        }
        _weatherForecast.Add((int)DetermineNextWeather());
    }

    // Chooses the next weather based on current probabilities.
    WeatherName DetermineNextWeather() {
        float prob = UnityEngine.Random.value;
        float cumul = 0f;
        for (int i = 0; i < _currentWeatherProbability.Length; i++) {
            cumul += _currentWeatherProbability[i];
            if (prob < cumul) {
                return i switch {
                    0 => WeatherName.Sun,
                    1 => (UnityEngine.Random.value < 0.5f) ? WeatherName.Cloudy : WeatherName.Wind,
                    2 => DetermineRainOrSnow(),
                    3 => WeatherName.Thunder,
                    _ => WeatherName.Sun
                };
            }
        }
        return WeatherName.Sun; // Fallback.
    }

    // Returns Rain or Snow based on the current season.
    WeatherName DetermineRainOrSnow() {
        var season = (SeasonName)_timeManager.CurrentDate.Season;
        return (season == SeasonName.Winter) ? WeatherName.Snow : WeatherName.Rain;
    }

    // Initializes probabilities and applies seasonal adjustments.
    void InitializeWeatherProbabilities() {
        Array.Copy(_baseWeatherProbability, _currentWeatherProbability, _baseWeatherProbability.Length);
        var currentSeason = (SeasonName)_timeManager.CurrentDate.Season;
        switch (currentSeason) {
            case SeasonName.Summer:
                AdjustProbabilities(SUMMER_SUN_BOOST);
                break;
            case SeasonName.Autumn:
                AdjustProbabilitiesForAutumn(FALL_WIND_BOOST);
                break;
            case SeasonName.Winter:
                AdjustProbabilitiesForWinter(WINTER_SNOW_BOOST);
                break;
        }
    }

    // Adjusts probabilities (e.g., for a sunny summer).
    void AdjustProbabilities(float boost) {
        _currentWeatherProbability[0] += boost;      // Sun
        _currentWeatherProbability[1] -= boost / 3f; // Clouds/Wind
        _currentWeatherProbability[2] -= boost / 3f; // Rain/Snow
        _currentWeatherProbability[3] -= boost / 3f; // Thunder
    }

    // Adjusts probabilities for autumn (favoring rain).
    void AdjustProbabilitiesForAutumn(float boost) {
        _currentWeatherProbability[2] += boost;
        _currentWeatherProbability[0] -= boost / 3f;
        _currentWeatherProbability[1] -= boost / 3f;
        _currentWeatherProbability[3] -= boost / 3f;
    }

    // Adjusts probabilities for winter (favoring snow).
    void AdjustProbabilitiesForWinter(float boost) {
        _currentWeatherProbability[2] += boost;
        _currentWeatherProbability[0] -= boost / 3f;
        _currentWeatherProbability[1] -= boost / 3f;
        _currentWeatherProbability[3] -= boost / 3f;
    }

    // Applies weather effects like rain intensity and audio ambience.
    void ApplyWeather() {
        var w = CurrentWeather;
        switch (w) {
            case WeatherName.Rain:
                OnChangeRainIntensity?.Invoke(LIGHT_RAIN_INTENSITY);
                _audioManager.SetAmbienceWeather(WeatherName.Rain);
                break;
            case WeatherName.Thunder:
                OnChangeRainIntensity?.Invoke(HEAVY_RAIN_INTENSITY);
                _audioManager.SetAmbienceWeather(WeatherName.Thunder);
                break;
            default:
                OnChangeRainIntensity?.Invoke(0);
                _audioManager.SetAmbienceWeather(w);
                break;
        }
    }

    // Updates the UI with the current weather forecast.
    void UpdateUIWeather() {
        int count = _weatherForecast.Count;
        int[] weatherIndices = new int[count];
        for (int i = 0; i < count; i++) {
            weatherIndices[i] = i > _weatherStation ? (int)WeatherName.None : _weatherForecast[i];
        }
        OnUpdateUIWeather?.Invoke(weatherIndices);
    }

    #endregion -------------------- Weather Forecast & Application --------------------

    #region -------------------- Audio & Thunder --------------------

    // Sets up initial audio settings.
    void InitializeAudio() {
        _audioManager.InitializeAmbience(_fmodEvents.Weather);
        _audioManager.InitializeMusic(_fmodEvents.Seasons);
        _audioManager.SetMusicSeason((SeasonName)_timeManager.CurrentDate.Season);
        _audioManager.SetAmbienceTimeOfDay(_timeManager.CurrentTimeOfDay);
        _audioManager.SetAmbienceWeather(CurrentWeather);
    }

    // Initializes the thunder timer with a random interval.
    void InitializeThunderTimer() {
        _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
    }

    // Updates thunder logic and triggers a thunder strike when the timer elapses.
    void UpdateThunder() {
        _timeSinceLastThunder += Time.deltaTime;
        if (CurrentWeather == WeatherName.Thunder && IsWeatherPlaying && _timeSinceLastThunder >= _nextThunderTime) {
            _timeSinceLastThunder = 0f;
            _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
            OnThunderStrike?.Invoke();
            PlayThunderSoundClientRpc();
        }
    }

    // Plays the thunder sound on all clients.
    [ClientRpc]
    void PlayThunderSoundClientRpc() {
        _audioManager.PlayOneShot(_fmodEvents.Thunder, transform.position);
    }

    // Returns a light factor to reduce scene brightness based on current weather.
    public float GetWeatherLightFactor() {
        return CurrentWeather switch {
            WeatherName.Cloudy or WeatherName.Wind => CLOUDS_WIND_LIGHT,
            WeatherName.Rain or WeatherName.Snow => RAIN_SNOW_LIGHT,
            WeatherName.Thunder => THUNDER_LIGHT,
            _ => 1f,
        };
    }

    // Checks for changes in the time of day to update ambience.
    void CheckForTimeOfDayUpdate() {
        if (_timeManager == null || _audioManager == null) return;
        var currentTimeOfDay = _timeManager.CurrentTimeOfDay;
        if (currentTimeOfDay != _lastAmbienceUpdate) {
            _lastAmbienceUpdate = currentTimeOfDay;
            _audioManager.SetAmbienceTimeOfDay(_lastAmbienceUpdate);
        }
    }

    #endregion -------------------- Audio & Thunder --------------------

    #region -------------------- Public API & Cheat Functions --------------------

    // Returns true if the current weather involves rain, thunder, or snow.
    public bool RainThunderSnow() =>
        CurrentWeather == WeatherName.Rain ||
        CurrentWeather == WeatherName.Thunder ||
        CurrentWeather == WeatherName.Snow;

    // Cheat: Set the current weather (only works on the server).
    [ServerRpc(RequireOwnership = false)]
    public void CheatSetWeatherServerRpc(int weather) {
        if (_weatherForecast.Count > 0 && Enum.IsDefined(typeof(WeatherName), weather)) {
            _weatherForecast[0] = weather;
            ApplyWeather();
            UpdateUIWeather();
        }
    }

    // Cheat: Replace the entire weather forecast.
    [ServerRpc(RequireOwnership = false)]
    public void CheatSetForecastServerRpc(int[] forecast) {
        _weatherForecast.Clear();
        foreach (int weather in forecast) {
            if (Enum.IsDefined(typeof(WeatherName), weather)) _weatherForecast.Add(weather);
            else Debug.LogWarning($"Cheat input: Invalid weather value {weather}. Skipped.");
        }
        ApplyWeather();
        UpdateUIWeather();
    }

    // Cheat: Force a thunder strike on the next update.
    [ServerRpc(RequireOwnership = false)]
    public void CheatTriggerThunderServerRpc() {
        if (CurrentWeather == WeatherName.Thunder && IsWeatherPlaying) {
            _timeSinceLastThunder = _nextThunderTime; // Forces a thunder strike on the next frame.
        }
    }

    #endregion -------------------- Public API & Cheat Functions --------------------

    #region -------------------- Data Persistence --------------------

    // Saves the current weather forecast into game data.
    public void SaveData(GameData data) {
        if (!_saveData) return;
        if (_weatherForecast == null || _weatherForecast.Count == 0) {
            _weatherForecast = new NetworkList<int>(new List<int> {
                (int)WeatherName.Sun, (int)WeatherName.Thunder, (int)WeatherName.Rain
            });
        }
        data.WeatherForecast = new int[_weatherForecast.Count];
        for (int i = 0; i < _weatherForecast.Count; i++) {
            data.WeatherForecast[i] = _weatherForecast[i];
        }
    }

    // Loads the weather forecast from saved game data.
    public void LoadData(GameData data) {
        if (!IsServer || data.WeatherForecast == null || !_loadData) return;

        _weatherForecast.Clear();
        foreach (var wInt in data.WeatherForecast) {
            if (Enum.IsDefined(typeof(WeatherName), wInt)) _weatherForecast.Add(wInt);
            else Debug.LogWarning($"Saved data: Invalid weather index {wInt}. Skipped.");
        }
        ApplyWeather();
        UpdateUIWeather();
    }

    #endregion -------------------- Data Persistence --------------------
}
