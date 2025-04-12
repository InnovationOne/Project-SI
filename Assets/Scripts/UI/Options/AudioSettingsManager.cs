using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FMODUnity;

public class AudioSettingsManager : MonoBehaviour {
    [Header("Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _menuSlider;
    [SerializeField] private Slider _envSlider;

    [Header("Output Mode")]
    [SerializeField] private TMP_Dropdown _outputDropdown;

    [Header("Test")]
    [SerializeField] private Button _testButton;

    private void Start() {
        LoadAll();

        _masterSlider.onValueChanged.AddListener(v => SetVolume("Volume_Master", v));
        _musicSlider.onValueChanged.AddListener(v => SetVolume("Volume_Music", v));
        _sfxSlider.onValueChanged.AddListener(v => SetVolume("Volume_SFX", v));
        _menuSlider.onValueChanged.AddListener(v => SetVolume("Volume_Menu", v));
        _envSlider.onValueChanged.AddListener(v => SetVolume("Volume_Environment", v));

        _outputDropdown.onValueChanged.AddListener(SetOutputMode);
        _testButton.onClick.AddListener(PlayTest);
    }

    private void LoadAll() {
        _masterSlider.value = PlayerPrefs.GetFloat("Volume_Master", 0.5f);
        _musicSlider.value = PlayerPrefs.GetFloat("Volume_Music", 0.5f);
        _sfxSlider.value = PlayerPrefs.GetFloat("Volume_SFX", 0.5f);
        _menuSlider.value = PlayerPrefs.GetFloat("Volume_Menu", 0.5f);
        _envSlider.value = PlayerPrefs.GetFloat("Volume_Environment", 0.5f);
        _outputDropdown.value = PlayerPrefs.GetInt("AudioOutputMode", 0); // 0 = Stereo, 1 = Mono
    }

    private void SetVolume(string key, float value) {
        PlayerPrefs.SetFloat(key, value);
    }

    private void SetOutputMode(int modeIndex) {
        PlayerPrefs.SetInt("AudioOutputMode", modeIndex);
        Debug.Log("Audio mode set to: " + (modeIndex == 0 ? "Stereo" : "Mono"));
        // TODO: Implement routing logic if required (e.g. via AudioMixer snapshot switching)
    }

    private void PlayTest() {
        if (FMODEvents.Instance != null) {
            RuntimeManager.PlayOneShot(FMODEvents.Instance.ItemPickup);
        }
    }
}
