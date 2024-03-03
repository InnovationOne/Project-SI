using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

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

    private const string MUSIC_PARAMETER_NAME = "Seasons";
    private const string AMBIENCE_PARAMETER_NAME = "Weather";

    private List<EventInstance> _eventInstances;
    private EventInstance _weatherAmbience;
    private EventInstance _seasonsMusic;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of AudioManager in the scene!");
            return;
        }
        Instance = this;

        _eventInstances = new List<EventInstance>();

        _masterBus = RuntimeManager.GetBus("bus:/");
        _musicBus = RuntimeManager.GetBus("bus:/Music");
        _ambienceBus = RuntimeManager.GetBus("bus:/Ambience");
        _sfxBus = RuntimeManager.GetBus("bus:/SFX");
    }

    private void Start() {
        InitializeAmbience(FMODEvents.Instance.WeatherAmbience);
        InitializeMusic(FMODEvents.Instance.SeasonMusic);
    }

    private void Update() {
        _masterBus.setVolume(MasterVolume);
        _musicBus.setVolume(MusicVolume);
        _ambienceBus.setVolume(AmbienceVolume);
        _sfxBus.setVolume(SFXVolume);
    }

    private void InitializeAmbience(EventReference eventReference) {
        _weatherAmbience = CreateEventInstance(eventReference);
        _weatherAmbience.start();
    }

    public void InitializeMusic(EventReference eventReference) {
        _seasonsMusic = CreateEventInstance(eventReference);
        _seasonsMusic.start();
    }

    public void SetMusicSeason(TimeAndWeatherManager.SeasonName seasonName) {
        _seasonsMusic.setParameterByName(MUSIC_PARAMETER_NAME, (float)seasonName);
    }

    public void SetAmbienceWeather(TimeAndWeatherManager.WeatherName weatherName) {
        _weatherAmbience.setParameterByName(AMBIENCE_PARAMETER_NAME, (float)weatherName);
    }

    // Plays a sound once at the given position
    public void PlayOneShot(EventReference sound, Vector3 worldPosition) {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }

    public EventInstance CreateEventInstance(EventReference eventReference) {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        _eventInstances.Add(eventInstance);
        return eventInstance;
    }

    private void CleanUp() {
        foreach (var eventInstance in _eventInstances) {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }
    }

    private void OnDestroy() {
        CleanUp();
    }
}
