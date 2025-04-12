using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsSettingsController : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private TMP_Dropdown fpsLimitDropdown;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Toggle screenshakeToggle;

    private readonly Resolution[] supportedResolutions = new Resolution[] {
        new() { width = 640, height = 360 },
        new() { width = 1280, height = 720 },
        new() { width = 1920, height = 1080 },
        new() { width = 2560, height = 1440 },
        new() { width = 3200, height = 1800 },
        new() { width = 3840, height = 2160 }
    };

    private const string ResolutionPrefKey = "ResolutionIndex";
    private const string WindowModePrefKey = "WindowMode";
    private const string VSyncPrefKey = "VSync";
    private const string FPSLimitPrefKey = "FPSLimit";
    private const string ScreenshakePrefKey = "Screenshake";

    private void Start() {
        InitializeResolutionOptions();
        InitializeWindowModeOptions();
        InitializeVSyncToggle();
        InitializeFPSLimitOptions();
        InitializeScreenshakeToggle();
    }

    private void InitializeResolutionOptions() {
        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        int currentResolutionIndex = PlayerPrefs.GetInt(ResolutionPrefKey, -1);

        for (int i = 0; i < supportedResolutions.Length; i++) {
            string option = $"{supportedResolutions[i].width} x {supportedResolutions[i].height}";
            options.Add(option);

            if (Screen.currentResolution.width == supportedResolutions[i].width &&
                Screen.currentResolution.height == supportedResolutions[i].height) {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(index => {
            PlayerPrefs.SetInt(ResolutionPrefKey, index);
            ApplyGraphicsSettings();
        });
    }

    private void InitializeWindowModeOptions() {
        windowModeDropdown.ClearOptions();
        var options = new List<string> { 
            "Fullscreen", 
            "Windowed", 
            "Borderless" };
        windowModeDropdown.AddOptions(options);

        int currentMode = PlayerPrefs.GetInt(WindowModePrefKey, (int)Screen.fullScreenMode);
        windowModeDropdown.value = currentMode;
        windowModeDropdown.RefreshShownValue();

        windowModeDropdown.onValueChanged.AddListener(index => {
            PlayerPrefs.SetInt(WindowModePrefKey, index);
            ApplyGraphicsSettings();
        });
    }

    private void InitializeVSyncToggle() {
        vSyncToggle.isOn = PlayerPrefs.GetInt(VSyncPrefKey, QualitySettings.vSyncCount) > 0;
        vSyncToggle.onValueChanged.AddListener(isOn => {
            PlayerPrefs.SetInt(VSyncPrefKey, isOn ? 1 : 0);
            ApplyGraphicsSettings();
        });
    }

    private void InitializeFPSLimitOptions() {
        fpsLimitDropdown.ClearOptions();
        var options = new List<string> { 
            "30", 
            "60", 
            "120", 
            "Unlimited" };
        fpsLimitDropdown.AddOptions(options);

        int currentFPS = PlayerPrefs.GetInt(FPSLimitPrefKey, Application.targetFrameRate);
        int index = options.IndexOf(currentFPS.ToString());
        fpsLimitDropdown.value = index >= 0 ? index : options.Count - 1;
        fpsLimitDropdown.RefreshShownValue();

        fpsLimitDropdown.onValueChanged.AddListener(index => {
            PlayerPrefs.SetInt(FPSLimitPrefKey, index);
            ApplyGraphicsSettings();
        });
    }

    private void InitializeScreenshakeToggle() {
        screenshakeToggle.isOn = PlayerPrefs.GetInt(ScreenshakePrefKey, 1) == 1;
        screenshakeToggle.onValueChanged.AddListener(isOn => {
            PlayerPrefs.SetInt(ScreenshakePrefKey, isOn ? 1 : 0);
            ApplyGraphicsSettings();
        });
    }

    private void ApplyGraphicsSettings() {
        int resolutionIndex = PlayerPrefs.GetInt(ResolutionPrefKey, 0);
        Resolution selectedResolution = supportedResolutions[resolutionIndex];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreenMode);

        FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt(WindowModePrefKey, (int)Screen.fullScreenMode);
        Screen.fullScreenMode = mode;

        QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncPrefKey, QualitySettings.vSyncCount) > 0 ? 1 : 0;

        int fpsLimitIndex = PlayerPrefs.GetInt(FPSLimitPrefKey, 3);
        int fpsLimit = fpsLimitIndex < 3 ? int.Parse(fpsLimitDropdown.options[fpsLimitIndex].text) : -1;
        Application.targetFrameRate = fpsLimit;

        // TODO Screenshake-Einstellung kann hier auf das Spiel angewendet werden.

        PlayerPrefs.Save();
    }
}
