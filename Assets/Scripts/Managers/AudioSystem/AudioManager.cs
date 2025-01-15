using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour {
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
    private EventInstance _ambience;
    private EventInstance _music;

    private void Awake() {
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
        if (_ambience.isValid()) {
            _ambience.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _ambience.release();
        }

        _ambience = CreateEventInstance(eventReference);
        _ambience.start();
    }

    public void InitializeMusic(EventReference eventReference) {
        if (_music.isValid()) {
            _music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _music.release();
        }

        _music = CreateEventInstance(eventReference);
        _music.start();
    }

    public void SetMusicSeason(TimeManager.SeasonName seasonName) {
        if (_music.isValid()) {
            _music.setParameterByName(MUSIC_PARAMETER_NAME, (float)seasonName);
        }
    }

    public void SetAmbienceWeather(WeatherManager.WeatherName weatherName) {
        if (_ambience.isValid()) {
            _ambience.setParameterByName(AMBIENCE_PARAMETER_NAME, (float)weatherName);
        }
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

    public void StopMusic() {
        if (_music.isValid()) {
            _music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _music.release();
            _music = default;
        }
    }

    private void CleanUp() {
        foreach (var eventInstance in _eventInstances) {
            if (eventInstance.isValid()) {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
        }

        _eventInstances.Clear();
    }

    private void OnDestroy() {
        CleanUp();
    }
}
