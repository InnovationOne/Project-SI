using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using static TimeManager;

/// <summary>
/// Handles weather forecasting, thunder logic, and audio/ambience setup.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class WeatherManager : NetworkBehaviour, IDataPersistance {
    public enum WeatherName { Sun, Cloudy, Wind, Rain, Thunder, Snow, Event, Wedding, None }

    public event Action<int[], int> OnUpdateUIWeather;
    public event Action<int> OnChangeRainIntensity;
    public event Action OnThunderStrike;

    #region -------------------- Inspector_Fields --------------------

    [Header("Data Loading/Saving")]
    [SerializeField] bool _loadData = true;
    [SerializeField] bool _saveData = true;

    [Header("Weather Settings")]
    [SerializeField] int _weatherStation = 0;

    [Tooltip("Minimum real-time seconds between thunder strikes.")]
    private const float MIN_TIME_BETWEEN_THUNDER = 10f;

    [Tooltip("Maximum real-time seconds between thunder strikes.")]
    private const float MAX_TIME_BETWEEN_THUNDER = 30f;

    #endregion -------------------- Inspector_Fields --------------------

    // Base probabilities (Sun, Clouds/Wind, Rain/Snow, Thunder).
    float[] _baseWeatherProbability = { 0.4f, 0.2f, 0.25f, 0.15f };
    float[] _currentWeatherProbability = new float[4];

    // Seasonal adjustments
    const float SUMMER_SUN_BOOST = 0.2f;
    const float FALL_WIND_BOOST = 0.2f;
    const float WINTER_SNOW_BOOST = 0.3f;

    // Factors that reduce brightness in TimeManager.
    const float CLOUDS_WIND_LIGHT = 0.9f;
    const float RAIN_SNOW_LIGHT = 0.8f;
    const float THUNDER_LIGHT = 0.5f;

    // Forecast (index 0 = today's weather)
    NetworkList<int> _weatherForecast = new(
            new List<int> {
                (int)WeatherName.Sun,
                (int)WeatherName.Thunder,
                (int)WeatherName.Rain
            },
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    public WeatherName CurrentWeather => ((WeatherName)_weatherForecast[0]);

    // Thunder logic
    public bool IsWeatherPlaying;
    float _timeSinceLastThunder = 0f;
    float _nextThunderTime;

    // Rain intensities
    const int LIGHT_RAIN_INTENSITY = 60;
    const int HEAVY_RAIN_INTENSITY = 100;

    // Dependencies
    TimeManager _timeManager;
    AudioManager _audioManager;
    FMODEvents _fmodEvents;

    // Cache last known timeOfDay to decide if we need to update ambience.
    private TimeOfDay _lastAmbienceUpdate;

    #region -------------------- Forecast --------------------

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        _timeManager = GameManager.Instance.TimeManager;
        _audioManager = GameManager.Instance.AudioManager;
        _fmodEvents = GameManager.Instance.FMODEvents;

        if (IsServer) {
            InitializeThunderTimer();

            // On day or season transitions, we recalc weather.
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

        // Unsubscribe from forecast changes.
        _weatherForecast.OnListChanged -= OnWeatherForecastChanged;

        base.OnNetworkDespawn();
    }

    #endregion -------------------- Forecast --------------------

    #region -------------------- Weather_Events --------------------
    void OnWeatherForecastChanged(NetworkListEvent<int> changeEvent) {
        ApplyWeather();
        UpdateUIWeather();
    }


    void HandleNextDayStarted() {
        // Shift & generate new forecast.
        AdvanceWeatherForecast();
        ApplyWeather();
        UpdateUIWeather();
    }

    void HandleNextSeasonStarted(int newSeason) {
        var season = (SeasonName)newSeason;
        _audioManager.SetMusicSeason(season);

        InitializeWeatherProbabilities();
        ApplyWeather();
        UpdateUIWeather();
    }

    #endregion -------------------- Weather_Events --------------------

    #region -------------------- Weather_Forecast --------------------

    void AdvanceWeatherForecast() {
        if (_weatherForecast.Count > 0) {
            _weatherForecast.RemoveAt(0);
        }
        _weatherForecast.Add((int)DetermineNextWeather());
    }

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
        return WeatherName.Sun; // fallback
    }

    WeatherName DetermineRainOrSnow() {
        var season = (SeasonName)_timeManager.CurrentDate.Season;
        return (season == SeasonName.Winter) ? WeatherName.Snow : WeatherName.Rain;
    }

    #endregion -------------------- Weather_Forecast --------------------

    #region -------------------- Weather_Probabilities --------------------

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

    void AdjustProbabilities(float boost) {
        // More Sun in Summer.
        _currentWeatherProbability[0] += boost;      // Sun
        _currentWeatherProbability[1] -= boost / 3f; // Clouds/Wind
        _currentWeatherProbability[2] -= boost / 3f; // Rain/Snow
        _currentWeatherProbability[3] -= boost / 3f; // Thunder
    }

    void AdjustProbabilitiesForAutumn(float boost) {
        // More rain in autumn.
        _currentWeatherProbability[2] += boost;
        _currentWeatherProbability[0] -= boost / 3f;
        _currentWeatherProbability[1] -= boost / 3f;
        _currentWeatherProbability[3] -= boost / 3f;
    }

    void AdjustProbabilitiesForWinter(float boost) {
        // More snow in Winter.
        _currentWeatherProbability[2] += boost;
        _currentWeatherProbability[0] -= boost / 3f;
        _currentWeatherProbability[1] -= boost / 3f;
        _currentWeatherProbability[3] -= boost / 3f;
    }

    #endregion -------------------- Weather_Probabilities --------------------

    #region -------------------- Audio --------------------

    void InitializeAudio() {
        _audioManager.InitializeAmbience(_fmodEvents.Weather);
        _audioManager.InitializeMusic(_fmodEvents.Seasons);

        // On spawn, set initial season & weather.
        _audioManager.SetMusicSeason((SeasonName)_timeManager.CurrentDate.Season);
        _audioManager.SetAmbienceTimeOfDay(_timeManager.CurrentTimeOfDay);
        _audioManager.SetAmbienceWeather(CurrentWeather);
    }

    void UpdateUIWeather() {
        int[] weatherIndices = new int[_weatherForecast.Count];
        for (int i = 0; i < _weatherForecast.Count; i++) {
            weatherIndices[i] = _weatherForecast[i];
        }
        OnUpdateUIWeather?.Invoke(weatherIndices, _weatherStation);
    }

    #endregion -------------------- Audio --------------------

    #region -------------------- Apply_Weather --------------------

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

    #endregion -------------------- Apply_Weather --------------------

    #region -------------------- Thunder_Logic --------------------
    void InitializeThunderTimer() {
        _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
    }

    void UpdateThunder() {
        _timeSinceLastThunder += Time.deltaTime;
        if (CurrentWeather == WeatherName.Thunder && IsWeatherPlaying) {
            if (_timeSinceLastThunder >= _nextThunderTime) {
                _timeSinceLastThunder = 0f;
                _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
                OnThunderStrike?.Invoke();
                PlayThunderSoundClientRpc();
            }
        }
    }

    [ClientRpc]
    void PlayThunderSoundClientRpc() {
        _audioManager.PlayOneShot(_fmodEvents.Thunder, transform.position);
    }

    #endregion -------------------- Thunder_Logic --------------------

    #region -------------------- Weather_Light_Factor --------------------

    public float GetWeatherLightFactor() {
        return CurrentWeather switch {
            WeatherName.Cloudy or WeatherName.Wind => CLOUDS_WIND_LIGHT,
            WeatherName.Rain or WeatherName.Snow => RAIN_SNOW_LIGHT,
            WeatherName.Thunder => THUNDER_LIGHT,
            _ => 1f,
        };
    }

    #endregion -------------------- Weather_Light_Factor --------------------

    #region -------------------- Ambience_Support --------------------

    void CheckForTimeOfDayUpdate() {
        if (_timeManager == null || _audioManager == null) return;

        // If the time of day changed, update ambiance.
        var currentTimeOfDay = _timeManager.CurrentTimeOfDay;
        if (currentTimeOfDay != _lastAmbienceUpdate) {
            _lastAmbienceUpdate = currentTimeOfDay;
            _audioManager.SetAmbienceTimeOfDay(_lastAmbienceUpdate);
        }
    }

    #endregion -------------------- Ambience_Support --------------------

    #region -------------------- Public_API --------------------

    public bool RainThunderSnow() {
        return CurrentWeather == WeatherName.Rain
            || CurrentWeather == WeatherName.Thunder
            || CurrentWeather == WeatherName.Snow;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheatSetWeatherServerRpc(int weather) {
        if (_weatherForecast.Count > 0) {
            _weatherForecast[0] = weather;
        }
    }

    #endregion -------------------- Public_API --------------------

    #region -------------------- Data_Persistance --------------------

    public void SaveData(GameData data) {
        if (!_saveData) return;

        if (_weatherForecast == null || _weatherForecast.Count == 0) {
            // Provide fallback forecast.
            _weatherForecast = new NetworkList<int>(new List<int>
            {
                (int)WeatherName.Sun,
                (int)WeatherName.Thunder,
                (int)WeatherName.Rain
            });
        }

        // Save entire forecast.
        data.WeatherForecast = new int[_weatherForecast.Count];
        for (int i = 0; i < _weatherForecast.Count; i++) {
            data.WeatherForecast[i] = _weatherForecast[i];
        }
    }

    public void LoadData(GameData data) {
        if (!IsServer || data.WeatherForecast == null || !_loadData) return;

        // Replace forecast.
        _weatherForecast.Clear();
        foreach (var wInt in data.WeatherForecast) {
            if (Enum.IsDefined(typeof(WeatherName), wInt)) {
                _weatherForecast.Add(wInt);
            } else {
                Debug.LogWarning($"Invalid weather index {wInt} in saved data. Skipping.");
            }
        }

        // Now that the list changed, clients also get updated.
        ApplyWeather();
        UpdateUIWeather();
    }

    #endregion -------------------- Data_Persistance --------------------
}
