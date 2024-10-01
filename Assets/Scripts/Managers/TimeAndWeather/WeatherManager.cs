using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// Manages in-game weather, handling synchronization across the network and updating related systems.
/// Utilizes Singleton pattern for global access.
/// </summary>
public class WeatherManager : NetworkBehaviour, IDataPersistance {
    // Enums for weather representation
    public enum WeatherName { Sun, Clouds, Wind, Rain, Thunder, Snow, Event, Marriage }

    // Singleton Instance
    public static WeatherManager Instance { get; private set; }

    // Events
    public event Action<int[], int> OnUpdateUIWeather;
    public event Action<int> OnChangeRainIntensity;
    public event Action OnThunderStrike;

    [Header("Day and Night Settings")]
    [SerializeField] private Color _nightLightColor = Color.black;
    [SerializeField] private Color _dayLightColor = Color.white;
    [SerializeField] private AnimationCurve _nightTimeCurve;
    [SerializeField] private Light2D _globalLight;

    [Header("Weather Settings")]
    [SerializeField] private int _weatherStation = 0;

    // Weather Probability based on current season
    private float[] _baseWeatherProbability = { 0.4f, 0.2f, 0.25f, 0.15f }; // Sun, Clouds/Wind, Snow/Rain, Thunder
    private float[] _currentWeatherProbability = new float[4];

    // Adjustments per season
    private readonly float _summerSunBoost = 0.2f;
    private readonly float _fallWindBoost = 0.2f;
    private readonly float _winterSnowBoost = 0.3f;

    private readonly float _cloudsWindLight = 0.9f;
    private readonly float _rainSnowLight = 0.8f;
    private readonly float _thunderLight = 0.5f;

    // Networked Weather Forecast List
    private NetworkList<int> _weatherForecast;

    // Current Weather Property
    public string CurrentWeather => _weatherForecast[^1].ToString();

    [Header("Thunder Settings")]
    private const float MIN_TIME_BETWEEN_THUNDER = 10f;
    private const float MAX_TIME_BETWEEN_THUNDER = 30f;
    private float _timeSinceLastThunder = 0f;
    private float _nextThunderTime;

    // Rain Intensity Constants
    private const int LIGHT_RAIN_INTENSITY = 60;
    private const int HEAVY_RAIN_INTENSITY = 100;

    // Cached References
    private AudioManager _audioManager;
    private FMODEvents _fmodEvents;
    private TimeManager _timeManager;

    #region Unity Lifecycle Methods

    private void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogWarning("Multiple instances of WeatherManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeThunderTimer();
    }

    /// <summary>
    /// Initializes network-related callbacks upon spawning.
    /// </summary>
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        _weatherForecast = new NetworkList<int>(new List<int> { (int)WeatherName.Rain, (int)WeatherName.Thunder, (int)WeatherName.Sun });

        if (IsServer) {
            _timeManager = TimeManager.Instance;
            _timeManager.OnNextDayStarted += HandleNextDayStarted;
            _timeManager.OnNextSeasonStarted += HandleNextSeasonStarted;
            InitializeWeatherProbabilities();
        }

        InitializeAudio();
        UpdateUIWeather();
    }

    private void Update() {
        if (IsServer) {
            UpdateThunder();
        }

        UpdateLightColor();
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Initializes the thunder timer with a random interval.
    /// </summary>
    private void InitializeThunderTimer() {
        _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
    }

    /// <summary>
    /// Initializes audio settings based on current weather and season.
    /// </summary>
    private void InitializeAudio() {
        _audioManager = AudioManager.Instance;
        _fmodEvents = FMODEvents.Instance;

        _audioManager.InitializeAmbience(_fmodEvents.WeatherAmbience);
        _audioManager.InitializeMusic(_fmodEvents.SeasonTheme);

        // Set Music based on Current Season
        var currentSeason = (TimeManager.SeasonName)_timeManager.CurrentDate.Value.Season;
        _audioManager.SetMusicSeason(currentSeason);

        // Set Ambience based on Initial Weather
        _audioManager.SetAmbienceWeather((WeatherName)_weatherForecast[0]);
    }

    /// <summary>
    /// Initializes weather probabilities based on the current season.
    /// </summary>
    private void InitializeWeatherProbabilities() {
        var currentSeason = (TimeManager.SeasonName)_timeManager.CurrentDate.Value.Season;

        // Reset to base probabilities
        Array.Copy(_baseWeatherProbability, _currentWeatherProbability, _baseWeatherProbability.Length);

        // Adjust based on season
        switch (currentSeason) {
            case TimeManager.SeasonName.Spring:
                // No adjustment
                break;
            case TimeManager.SeasonName.Summer:
                _currentWeatherProbability[0] += _summerSunBoost; // More sun
                _currentWeatherProbability[1] -= _summerSunBoost / 3; // Less clouds/wind
                _currentWeatherProbability[2] -= _summerSunBoost / 3; // Less snow/rain
                _currentWeatherProbability[3] -= _summerSunBoost / 3; // Less thunder
                break;
            case TimeManager.SeasonName.Autumn:
                _currentWeatherProbability[2] += _fallWindBoost; // More wind
                _currentWeatherProbability[0] -= _fallWindBoost / 3; // Less sun
                _currentWeatherProbability[1] -= _fallWindBoost / 3; // Less snow/rain
                _currentWeatherProbability[3] -= _fallWindBoost / 3; // Less thunder
                break;
            case TimeManager.SeasonName.Winter:
                _currentWeatherProbability[2] += _winterSnowBoost; // More snow/rain
                _currentWeatherProbability[0] -= _winterSnowBoost / 3; // Less sun
                _currentWeatherProbability[1] -= _winterSnowBoost / 3; // clouds/wind
                _currentWeatherProbability[3] -= _winterSnowBoost / 3; // Less thunder
                break;
            default:
                break;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the event when a new day starts.
    /// </summary>
    private void HandleNextDayStarted() {
        GetWeatherForecast();
        ApplyWeather();
        UpdateUIWeather();
    }

    /// <summary>
    /// Handles the event when a new season starts.
    /// </summary>
    /// <param name="newSeason">The new season index.</param>
    private void HandleNextSeasonStarted(int newSeason) {
        var season = (TimeManager.SeasonName)newSeason;
        _audioManager.SetMusicSeason((TimeManager.SeasonName)newSeason);
        InitializeWeatherProbabilities();
        ApplyWeather();
        UpdateUIWeather();
    }

    #endregion

    #region Thunder Management

    /// <summary>
    /// Updates thunder-related logic on the server.
    /// </summary>
    private void UpdateThunder() {
        _timeSinceLastThunder += Time.deltaTime;
        var currentWeather = (WeatherName)_weatherForecast[0];

        if (_timeSinceLastThunder >= _nextThunderTime && currentWeather == WeatherName.Thunder) {
            _timeSinceLastThunder = 0f;
            _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
            OnThunderStrike?.Invoke();
            PlayThunderSoundClientRpc();
        }
    }

    /// <summary>
    /// Plays the thunder sound effect on all clients.
    /// </summary>
    [ClientRpc]
    private void PlayThunderSoundClientRpc() {
        _audioManager.PlayOneShot(_fmodEvents.Thunder, transform.position);
    }

    #endregion

    #region Weather Logic

    /// <summary>
    /// Retrieves and updates the weather forecast.
    /// </summary>
    public void GetWeatherForecast() {
        if (_weatherForecast.Count > 0) {
            _weatherForecast.RemoveAt(0);
        }
        _weatherForecast.Add((int)DetermineNextWeather());
    }

    /// <summary>
    /// Determines the next weather based on probabilities and current season.
    /// </summary>
    /// <returns>The next WeatherName.</returns>
    private WeatherName DetermineNextWeather() {
        float probability = UnityEngine.Random.value;
        float cumulative = 0f;

        for (int i = 0; i < _currentWeatherProbability.Length; i++) {
            cumulative += _currentWeatherProbability[i];
            if (probability < cumulative) {
                switch (i) {
                    case 0:
                        return WeatherName.Sun;
                    case 1:
                        // Decide between Clouds and Wind
                        return UnityEngine.Random.value < 0.5f ? WeatherName.Clouds : WeatherName.Wind;
                    case 2:
                        // Decide between Rain and Snow based on season
                        var currentSeason = (TimeManager.SeasonName)_timeManager.CurrentDate.Value.Season;
                        if (currentSeason == TimeManager.SeasonName.Winter) {
                            return WeatherName.Snow;
                        } else {
                            return WeatherName.Rain;
                        }
                    case 3:
                        return WeatherName.Thunder;
                    default:
                        return WeatherName.Sun;
                }
            }
        }

        Debug.LogError("DetermineNextWeather fallback reached!");
        return WeatherName.Sun;
    }

    /// <summary>
    /// Applies the current weather settings.
    /// </summary>
    private void ApplyWeather() {
        var todayWeather = (WeatherName)_weatherForecast[0];

        switch (todayWeather) {
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
                _audioManager.SetAmbienceWeather(todayWeather);
                break;
        }
    }

    /// <summary>
    /// Calculates today's global light intensity based on weather.
    /// </summary>
    /// <returns>The global light intensity as a float.</returns>
    private float GetTodaysGlobalLightIntensity() {
        var todayWeather = (WeatherName)_weatherForecast[0];

        return todayWeather switch {
            WeatherName.Clouds or WeatherName.Wind => _cloudsWindLight,
            WeatherName.Snow or WeatherName.Rain => _rainSnowLight,
            WeatherName.Thunder => _thunderLight,
            _ => 1f
        };
    }

    /// <summary>
    /// Updates the UI and global light color based on the weather forecast.
    /// </summary>
    private void UpdateUIWeather() {
        int[] weatherIndices = new int[_weatherForecast.Count];
        for (int i = 0; i < _weatherForecast.Count; i++) {
            weatherIndices[i] = _weatherForecast[i];
        }

        OnUpdateUIWeather?.Invoke(weatherIndices, _weatherStation);
        _globalLight.color = Color.HSVToRGB(0f, 0f, GetTodaysGlobalLightIntensity());
    }

    /// <summary>
    /// Smoothly transitions the global light color based on the time of day.
    /// </summary>
    private void UpdateLightColor() {
        if (_timeManager == null) {
            return;
        }

        float curvePosition = _nightTimeCurve.Evaluate(_timeManager.GetHours());
        Color newColor = Color.Lerp(_dayLightColor, _nightLightColor, curvePosition);
        _globalLight.color = newColor;
    }

    #endregion

    #region Cheat Console

    /// <summary>
    /// Sets the weather via cheat command.
    /// </summary>
    /// <param name="weather">The weather index to set.</param>
    public void CheatSetWeather(int weather) {
        if (IsServer) {
            SetWeather((WeatherName)weather);
        } else {
            CheatSetWeatherServerRpc(weather);
        }
    }

    /// <summary>
    /// Server RPC to set the weather.
    /// </summary>
    /// <param name="weather">The weather index to set.</param>
    [ServerRpc(RequireOwnership = false)]
    private void CheatSetWeatherServerRpc(int weather) {
        SetWeather((WeatherName)weather);
    }

    /// <summary>
    /// Sets the weather and updates relevant systems.
    /// </summary>
    /// <param name="weather">The WeatherName to set.</param>
    private void SetWeather(WeatherName weather) {
        if (_weatherForecast.Count > 0) {
            _weatherForecast[0] = (int)weather;
            ApplyWeather();
            UpdateUIWeather();
        }
    }

    #endregion

    #region Save & Load

    /// <summary>
    /// Saves the current weather forecast data.
    /// </summary>
    /// <param name="data">GameData object to save into.</param>
    public void SaveData(GameData data) {
        data.WeatherForecast = new int[_weatherForecast.Count];
        for (int i = 0; i < _weatherForecast.Count; i++) {
            data.WeatherForecast[i] = _weatherForecast[i];
        }
    }

    /// <summary>
    /// Loads the saved weather forecast data.
    /// </summary>
    /// <param name="data">GameData object to load from.</param>
    public void LoadData(GameData data) {
        if (IsServer && data.WeatherForecast != null) {
            _weatherForecast.Clear();
            foreach (var weatherInt in data.WeatherForecast) {
                // Validate weather index before adding
                if (Enum.IsDefined(typeof(WeatherName), weatherInt)) {
                    _weatherForecast.Add(weatherInt);
                } else {
                    Debug.LogWarning($"Invalid weather index {weatherInt} found in saved data. Skipping.");
                }
            }

            ApplyWeather();
            UpdateUIWeather();
        }
    }

    #endregion

    /// <summary>
    /// Cleans up event subscriptions when destroyed.
    /// </summary>
    private void OnDestroy() {
        base.OnDestroy();

        if (IsServer && _timeManager != null) {
            _timeManager.OnNextDayStarted -= HandleNextDayStarted;
            _timeManager.OnNextSeasonStarted -= HandleNextSeasonStarted;
        }
    }
}
