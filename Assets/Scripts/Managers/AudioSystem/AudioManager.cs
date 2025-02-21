using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using static WeatherManager;
using static TimeManager;
using static SwitchMusicTrigger;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)]
    public float MasterVolume = 1f;
    private Bus _masterBus;
    [Range(0f, 1f)]
    public float MusicVolume = 1f;
    private Bus _musicBus;
    [Range(0f, 1f)]
    public float AmbienceVolume = 1f;
    private Bus _ambienceBus;
    [Range(0f, 1f)]
    public float SFXVolume = 1f;
    private Bus _sfxBus;

    // Music
    private const string SEASONS_PARAMETER_NAME = "Seasons";
    private const string BOSS_FIGHT_PARAMETER_NAME = "BossFight";
    // Ambience
    private const string WEATHER_PARAMETER_NAME = "Weather";
    private const string TIME_OF_DAY_PARAMETER_NAME = "TimeOfDay";
    private const string AMBIENCE_WIND_INTENSITY_PARAMETER_NAME = "Wind_Intensity";

    // SFX
    private const string GROUND_PARAMETER_NAME = "Ground";

    private List<EventInstance> _eventInstances;
    private EventInstance _ambience;
    private EventInstance _music;

    private Dictionary<string, EventInstance> loopingSoundInstances = new();

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of AudioManager in the scene!");
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _eventInstances = new List<EventInstance>();

        _masterBus = RuntimeManager.GetBus("bus:/");
        _musicBus = RuntimeManager.GetBus("bus:/Music");
        _ambienceBus = RuntimeManager.GetBus("bus:/Ambience");
        _sfxBus = RuntimeManager.GetBus("bus:/SFX");
    }

    private void Update() {
        _masterBus.setVolume(MasterVolume);
        _musicBus.setVolume(MusicVolume);
        _ambienceBus.setVolume(AmbienceVolume);
        _sfxBus.setVolume(SFXVolume);
    }

    public void InitializeAmbience(EventReference eventReference) {
        StopEvent(_ambience);
        _ambience = CreateEventInstance(eventReference);
        _ambience.start();
    }

    public void InitializeMusic(EventReference eventReference) {
        StopEvent(_music);
        _music = CreateEventInstance(eventReference);
        _music.start();
    }

    public void PlayMusic(EventReference newMusicEvent) {
        StopEvent(_music, crossfade: true);
        _music = CreateEventInstance(newMusicEvent);
        _music.start();
    }

    public void SetMusicSeason(SeasonName seasonName) {
        if (_music.isValid()) {
            _music.setParameterByName(SEASONS_PARAMETER_NAME, (float)seasonName);
        }
    }

    public void SetBossFight(BossFight bossFight) {
        if (_music.isValid()) {
            _music.setParameterByName(BOSS_FIGHT_PARAMETER_NAME, (float)bossFight);
        }
    }

    public void SetBossFightPhase(BossFightPhase bossFightPhase) {
        if (_music.isValid()) {
            _music.setParameterByName("", (float)bossFightPhase);
        }
    }

    public void PlayAmbience(EventReference newAmbienceEvent) {
        GameManager.Instance.WeatherManager.IsWeatherPlaying = newAmbienceEvent.Equals(GameManager.Instance.FMODEvents.Weather);
        StopEvent(_ambience);
        InitializeAmbience(newAmbienceEvent);
    }

    public void SetAmbienceTimeOfDay(TimeOfDay timeOfDay) {
        if (_ambience.isValid()) {
            if (timeOfDay == TimeOfDay.Evening) {
                _ambience.setParameterByName(TIME_OF_DAY_PARAMETER_NAME, 1);
                return;
            } else if (timeOfDay == TimeOfDay.Night) {
                _ambience.setParameterByName(TIME_OF_DAY_PARAMETER_NAME, 2);
                return;
            } else {
                _ambience.setParameterByName(TIME_OF_DAY_PARAMETER_NAME, 0);
                return;
            }
        }
    }

    public void SetAmbienceWeather(WeatherName weatherName) {
        if (_ambience.isValid()) {
            if (weatherName == WeatherName.Rain) {
                _ambience.setParameterByName(WEATHER_PARAMETER_NAME, 1);
                return;
            } else if (weatherName == WeatherName.Thunder) {
                _ambience.setParameterByName(WEATHER_PARAMETER_NAME, 2);
                return;
            } else if (weatherName == WeatherName.Wind) {
                _ambience.setParameterByName(WEATHER_PARAMETER_NAME, 3);
                return;
            } else {
                _ambience.setParameterByName(WEATHER_PARAMETER_NAME, 0);
                return;
            }
        }
    }

    // Plays a sound once at the given position
    public void PlayOneShot(EventReference sound, Vector3 worldPosition) {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }

    public void PlayLoopingSound(EventReference sound, Vector3 position) {
        if (loopingSoundInstances.ContainsKey(sound.Path)) return;
        EventInstance instance = RuntimeManager.CreateInstance(sound.Path);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        instance.start();
        loopingSoundInstances.Add(sound.Path, instance);
    }

    public void StopSound(EventReference sound) {
        if (loopingSoundInstances.TryGetValue(sound.Path, out EventInstance instance)) {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
            loopingSoundInstances.Remove(sound.Path);
        }
    }

    public EventInstance CreateEventInstance(EventReference eventReference) {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        _eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public void StopMusic() => StopEvent(_music);

    private bool StopEvent(EventInstance eventInstance, bool crossfade = false) {
        bool isValid = eventInstance.isValid();
        if (isValid) {
            // If crossfade is true, use ALLOWFADEOUT, otherwise IMMEDIATE
            eventInstance.stop(crossfade ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
                                         : FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }
        return isValid;
    }

    private void OnDestroy() {
        if (_eventInstances == null) return;
        foreach (var eventInstance in _eventInstances) {
            StopEvent(eventInstance);
        }

        _eventInstances.Clear();
    }
}
