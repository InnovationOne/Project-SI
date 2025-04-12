using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using static WeatherManager;
using static TimeManager;
using static SwitchMusicTrigger;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    private readonly Dictionary<string, Bus> _busMap = new();
    private readonly Dictionary<EventReference, EventInstance> _loopingSoundInstances = new();
    private readonly List<EventInstance> _eventInstances = new();

    private EventInstance _music;
    private EventInstance _ambience;

    private const string MasterBus = "bus:/";
    private const string MusicBus = "bus:/Music";
    private const string AmbienceBus = "bus:/Ambience";
    private const string SFXBus = "bus:/SFX";
    private const string MenuBus = "bus:/UI";
    private const string VoiceBus = "bus:/Voice";

    private const string ParamSeason = "Seasons";
    private const string ParamBossFight = "BossFight";
    private const string ParamBossPhase = "BossPhase";
    private const string ParamTimeOfDay = "TimeOfDay";
    private const string ParamWeather = "Weather";
    private const string ParamWindIntensity = "Wind_Intensity";
    private const string ParamGround = "Ground";


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of AudioManager in the scene!");
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _busMap["Volume_Master"] = RuntimeManager.GetBus(MasterBus);
        _busMap["Volume_Music"] = RuntimeManager.GetBus(MusicBus);
        _busMap["Volume_Ambience"] = RuntimeManager.GetBus(AmbienceBus);
        _busMap["Volume_SFX"] = RuntimeManager.GetBus(SFXBus);
        _busMap["Volume_Menu"] = RuntimeManager.GetBus(MenuBus);
        _busMap["Volume_Voice"] = RuntimeManager.GetBus(VoiceBus);
    }

    private void Update() {
        foreach (var (key, bus) in _busMap) {
            bus.setVolume(PlayerPrefs.GetFloat(key, 0.5f));
        }
    }

    #region Music

    public void InitializeMusic(EventReference reference) {
        StopEvent(ref _music);
        _music = Create(reference);
        _music.start();
    }

    public void PlayMusic(EventReference musicEvent) {
        StopEvent(ref _music, crossfade: true);
        _music = Create(musicEvent);
        _music.start();
    }

    public void SetMusicSeason(SeasonName season) {
        if (_music.isValid()) _music.setParameterByName(ParamSeason, (float)season);
    }

    public void SetBossFight(BossFight boss) {
        if (_music.isValid()) _music.setParameterByName(ParamBossFight, (float)boss);
    }

    public void SetBossFightPhase(BossFightPhase phase) {
        if (_music.isValid()) _music.setParameterByName(ParamBossPhase, (float)phase);
    }

    #endregion

    #region Ambience

    public void InitializeAmbience(EventReference reference) {
        StopEvent(ref _ambience);
        _ambience = Create(reference);
        _ambience.start();
    }

    public void PlayAmbience(EventReference ambienceEvent) {
        GameManager.Instance.WeatherManager.IsWeatherPlaying = ambienceEvent.Equals(GameManager.Instance.FMODEvents.Weather);
        StopEvent(ref _ambience);
        _ambience = Create(ambienceEvent);
        _ambience.start();
    }

    public void SetAmbienceTimeOfDay(TimeOfDay time) {
        if (!_ambience.isValid()) return;
        int value = time switch {
            TimeOfDay.Morning => 0,
            TimeOfDay.Evening => 1,
            TimeOfDay.Night => 2,
            _ => 0
        };
        _ambience.setParameterByName(ParamTimeOfDay, value);
    }

    public void SetAmbienceWeather(WeatherName weather) {
        if (!_ambience.isValid()) return;
        int value = weather switch {
            WeatherName.Rain => 1,
            WeatherName.Thunder => 2,
            WeatherName.Wind => 3,
            _ => 0
        };
        _ambience.setParameterByName(ParamWeather, value);
    }

    #endregion

    #region Utiliy

    public void PlayOneShot(EventReference sound, Vector3 position) => RuntimeManager.PlayOneShot(sound, position);

    public void PlayLoopingSound(EventReference sound, Vector3 position) {
        if (_loopingSoundInstances.ContainsKey(sound)) return;
        var instance = RuntimeManager.CreateInstance(sound);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        instance.start();
        _loopingSoundInstances[sound] = instance;
    }

    public void StopLooping(EventReference sound) {
        if (_loopingSoundInstances.TryGetValue(sound, out var instance)) {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
            _loopingSoundInstances.Remove(sound);
        }
    }

    public EventInstance Create(EventReference reference) {
        var instance = RuntimeManager.CreateInstance(reference);
        _eventInstances.Add(instance);
        return instance;
    }

    public void StopMusic() => StopEvent(ref _music);

    public void StopSound(EventReference sound) {
        if (_loopingSoundInstances.TryGetValue(sound, out EventInstance instance)) {
            StopEvent(ref instance, true);
            _loopingSoundInstances.Remove(sound);
        }
    }

    private void StopEvent(ref EventInstance instance, bool crossfade = false) {
        if (!instance.isValid()) return;
        instance.stop(crossfade ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.release();
        instance = default;
    }

    private void OnDestroy() {
        foreach (var evt in _eventInstances) if (evt.isValid()) evt.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        foreach (var evt in _loopingSoundInstances.Values) evt.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        _eventInstances.Clear();
        _loopingSoundInstances.Clear();
    }

    #endregion
}
