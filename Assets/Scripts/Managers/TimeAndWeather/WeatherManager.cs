using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
public class WeatherManager : NetworkBehaviour, IDataPersistance {
    public enum WeatherName { Sun, Cloudy, Wind, Rain, Thunder, Snow, Event, Wedding, None }

    public static WeatherManager Instance { get; private set; }

    public event Action<int[], int> OnUpdateUIWeather;
    public event Action<int> OnChangeRainIntensity;
    public event Action OnThunderStrike;

    [Header("Day and Night Settings")]
    [SerializeField] Color _nightLightColor = Color.black;
    [SerializeField] Color _dayLightColor = Color.white;
    [SerializeField] AnimationCurve _nightTimeCurve;
    [SerializeField] Light2D _globalLight;

    [Header("Weather Settings")]
    [SerializeField] int _weatherStation = 0;

    // Base probabilities for weather: Sun, Clouds/Wind, Snow/Rain, Thunder
    readonly float[] _baseWeatherProbability = { 0.4f, 0.2f, 0.25f, 0.15f };
    readonly float[] _currentWeatherProbability = new float[4];

    // Seasonal adjustments
    const float SUMMER_SUN_BOOST = 0.2f;
    const float FALL_WIND_BOOST = 0.2f;
    const float WINTER_SNOW_BOOST = 0.3f;

    const float CLOUDS_WIND_LIGHT = 0.9f;
    const float RAIN_SNOW_LIGHT = 0.8f;
    const float THUNDER_LIGHT = 0.5f;

    NetworkList<int> _weatherForecast;
    public WeatherName CurrentWeather => ((WeatherName)_weatherForecast[0]);

    const float MIN_TIME_BETWEEN_THUNDER = 10f;
    const float MAX_TIME_BETWEEN_THUNDER = 30f;
    float _timeSinceLastThunder = 0f;
    float _nextThunderTime;

    const int LIGHT_RAIN_INTENSITY = 60;
    const int HEAVY_RAIN_INTENSITY = 100;

    AudioManager _audioManager;
    FMODEvents _fmodEvents;
    TimeManager _timeManager;


    void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogError("Multiple instances of WeatherManager detected.");
            return;
        }
        Instance = this;

        _weatherForecast = new NetworkList<int>(new List<int> {
            (int)WeatherName.Rain,
            (int)WeatherName.Thunder,
            (int)WeatherName.Sun
        });

        InitializeThunderTimer();
    }

    void Start() {
        _timeManager = TimeManager.Instance;
        InitializeAudio();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            _timeManager.OnNextDayStarted += HandleNextDayStarted;
            _timeManager.OnNextSeasonStarted += HandleNextSeasonStarted;
            InitializeWeatherProbabilities();
        }
        
        UpdateUIWeather();
    }

    void Update() {
        if (IsServer) {
            UpdateThunder();
        }
        UpdateLightColor();
    }

    void InitializeThunderTimer() => _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);

    void InitializeAudio() {
        _audioManager = AudioManager.Instance;
        _fmodEvents = FMODEvents.Instance;

        _audioManager.InitializeAmbience(_fmodEvents.WeatherAmbience);
        _audioManager.InitializeMusic(_fmodEvents.SeasonTheme);

        var currentSeason = (TimeManager.SeasonName)_timeManager.CurrentDate.Value.Season;
        _audioManager.SetMusicSeason(currentSeason);
        _audioManager.SetAmbienceWeather((WeatherName)_weatherForecast[0]);
    }

    void InitializeWeatherProbabilities() {
        var currentSeason = (TimeManager.SeasonName)_timeManager.CurrentDate.Value.Season;
        Array.Copy(_baseWeatherProbability, _currentWeatherProbability, _baseWeatherProbability.Length);

        // Seasonal adjustments
        switch (currentSeason) {
            case TimeManager.SeasonName.Summer:
                AdjustProbabilities(SUMMER_SUN_BOOST);
                break;
            case TimeManager.SeasonName.Autumn:
                AdjustProbabilitiesForAutumn(FALL_WIND_BOOST);
                break;
            case TimeManager.SeasonName.Winter:
                AdjustProbabilitiesForWinter(WINTER_SNOW_BOOST);
                break;
            default:
                break;
        }
    }

    void AdjustProbabilities(float boost) {
        _currentWeatherProbability[0] += boost;
        _currentWeatherProbability[1] -= boost / 3f;
        _currentWeatherProbability[2] -= boost / 3f;
        _currentWeatherProbability[3] -= boost / 3f;
    }

    void AdjustProbabilitiesForAutumn(float boost) {
        _currentWeatherProbability[2] += boost;
        _currentWeatherProbability[0] -= boost / 3f;
        _currentWeatherProbability[1] -= boost / 3f;
        _currentWeatherProbability[3] -= boost / 3f;
    }

    void AdjustProbabilitiesForWinter(float boost) {
        _currentWeatherProbability[2] += boost;
        _currentWeatherProbability[0] -= boost / 3f;
        _currentWeatherProbability[1] -= boost / 3f;
        _currentWeatherProbability[3] -= boost / 3f;
    }

    void HandleNextDayStarted() {
        GetWeatherForecast();
        ApplyWeather();
        UpdateUIWeather();
    }

    void HandleNextSeasonStarted(int newSeason) {
        var season = (TimeManager.SeasonName)newSeason;
        _audioManager.SetMusicSeason(season);
        InitializeWeatherProbabilities();
        ApplyWeather();
        UpdateUIWeather();
    }

    void UpdateThunder() {
        _timeSinceLastThunder += Time.deltaTime;
        var currentWeather = (WeatherName)_weatherForecast[0];

        if (_timeSinceLastThunder >= _nextThunderTime && currentWeather == WeatherName.Thunder) {
            _timeSinceLastThunder = 0f;
            _nextThunderTime = UnityEngine.Random.Range(MIN_TIME_BETWEEN_THUNDER, MAX_TIME_BETWEEN_THUNDER);
            OnThunderStrike?.Invoke();
            PlayThunderSoundClientRpc();
        }
    }

    [ClientRpc]
    void PlayThunderSoundClientRpc() => _audioManager.PlayOneShot(_fmodEvents.Thunder, transform.position);
    
    public void GetWeatherForecast() {
        if (_weatherForecast.Count > 0) {
            _weatherForecast.RemoveAt(0);
        }
        _weatherForecast.Add((int)DetermineNextWeather());
    }

    WeatherName DetermineNextWeather() {
        float probability = UnityEngine.Random.value;
        float cumulative = 0f;
        for (int i = 0; i < _currentWeatherProbability.Length; i++) {
            cumulative += _currentWeatherProbability[i];
            if (probability < cumulative) {
                return i switch {
                    0 => WeatherName.Sun,
                    1 => (UnityEngine.Random.value < 0.5f ? WeatherName.Cloudy : WeatherName.Wind),
                    2 => DetermineRainOrSnow(),
                    3 => WeatherName.Thunder,
                    _ => WeatherName.Sun
                };
            }
        }

        Debug.LogError("Weather fallback reached!");
        return WeatherName.Sun;
    }

    WeatherName DetermineRainOrSnow() {
        var currentSeason = (TimeManager.SeasonName)_timeManager.CurrentDate.Value.Season;
        return currentSeason == TimeManager.SeasonName.Winter ? WeatherName.Snow : WeatherName.Rain;
    }

    void ApplyWeather() {
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

    float GetTodaysGlobalLightIntensity() {
        var w = (WeatherName)_weatherForecast[0];
        return w switch {
            WeatherName.Cloudy or WeatherName.Wind => CLOUDS_WIND_LIGHT,
            WeatherName.Snow or WeatherName.Rain => RAIN_SNOW_LIGHT,
            WeatherName.Thunder => THUNDER_LIGHT,
            _ => 1f
        };
    }

    void UpdateUIWeather() {
        int[] weatherIndices = new int[_weatherForecast.Count];
        for (int i = 0; i < _weatherForecast.Count; i++) {
            weatherIndices[i] = _weatherForecast[i];
        }

        OnUpdateUIWeather?.Invoke(weatherIndices, _weatherStation);
        _globalLight.color = Color.HSVToRGB(0f, 0f, GetTodaysGlobalLightIntensity());
    }

    void UpdateLightColor() {
        if (_timeManager == null) return;
        float curvePos = _nightTimeCurve.Evaluate(_timeManager.GetHours());
        Color newColor = Color.Lerp(_dayLightColor, _nightLightColor, curvePos);
        _globalLight.color = newColor;
    }

    public bool RainThunderSnow() => CurrentWeather == WeatherName.Rain ||
                                      CurrentWeather == WeatherName.Thunder ||
                                      CurrentWeather == WeatherName.Snow;

    public void CheatSetWeather(int weather) {
        if (IsServer) {
            SetWeather((WeatherName)weather);
        } else {
            CheatSetWeatherServerRpc(weather);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void CheatSetWeatherServerRpc(int weather) => SetWeather((WeatherName)weather);

    void SetWeather(WeatherName weather) {
        if (_weatherForecast.Count > 0) {
            _weatherForecast[0] = (int)weather;
            ApplyWeather();
            UpdateUIWeather();
        }
    }

    public void SaveData(GameData data) {
        if (_weatherForecast == null || _weatherForecast.Count == 0) {
            _weatherForecast = new NetworkList<int>(new List<int> {
                (int)WeatherName.Rain,
                (int)WeatherName.Thunder,
                (int)WeatherName.Sun
            });
        }

        data.WeatherForecast = new int[_weatherForecast.Count];
        for (int i = 0; i < _weatherForecast.Count; i++) {
            data.WeatherForecast[i] = _weatherForecast[i];
        }
    }

    public void LoadData(GameData data) {
        if (!IsServer || data.WeatherForecast == null) return;
        _weatherForecast.Clear();
        foreach (var wInt in data.WeatherForecast) {
            if (Enum.IsDefined(typeof(WeatherName), wInt)) {
                _weatherForecast.Add(wInt);
            } else {
                Debug.LogWarning($"Invalid weather index {wInt} in saved data. Skipping.");
            }
        }
        ApplyWeather();
        UpdateUIWeather();
    }

    private new void OnDestroy() {
        if (IsServer && _timeManager != null) {
            _timeManager.OnNextDayStarted -= HandleNextDayStarted;
            _timeManager.OnNextSeasonStarted -= HandleNextSeasonStarted;
        }
        base.OnDestroy();
    }
}
