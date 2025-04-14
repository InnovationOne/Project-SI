using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Manages gameplay settings (languages, UI toggles, dialogue speed) and
/// provides an event interface for modded systems to subscribe to setting changes.
/// </summary>
public class GameplaySettingsController : MonoBehaviour {
    // Inspector-assigned UI elements
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown _textLanguageDropdown;
    [SerializeField] private TMP_Dropdown _audioLanguageDropdown;

    [Header("Toggles")]
    [SerializeField] private Toggle _showNpcNamesToggle;
    [SerializeField] private Toggle _showTooltipsToggle;
    [SerializeField] private Toggle _showWaypointsToggle;
    [SerializeField] private Toggle _skipIntroToggle;

    [Header("Slider")]
    [SerializeField] private Slider _dialogueSpeedSlider;
    [SerializeField] private TextMeshProUGUI _dialogueSpeedValueText;

    // Cached list of available locales
    private List<Locale> _availableLocales;

    // Event to notify external systems (mods) when a settings change occurs.
    public event System.Action OnSettingsChanged;

    private void Start() {
        // Begin localization initialization and load saved settings.
        StartCoroutine(InitializeLocalization());
        LoadSettings();
        SetupToggleListeners();
        SetupSliderListener();
    }

    /// <summary>
    /// Waits for localization to be fully initialized, then sets up language dropdowns.
    /// </summary>
    private IEnumerator InitializeLocalization() {
        yield return LocalizationSettings.InitializationOperation;
        _availableLocales = new List<Locale>(LocalizationSettings.AvailableLocales.Locales);
        SetupTextLanguageDropdown();
        SetupAudioLanguageDropdown();
    }

    /// <summary>
    /// Registers toggle events to save settings when their values change.
    /// </summary>
    private void SetupToggleListeners() {
        _showNpcNamesToggle.onValueChanged.AddListener(value => SaveToggleSetting("ShowNPCNames", value));
        _showTooltipsToggle.onValueChanged.AddListener(value => SaveToggleSetting("ShowTooltips", value));
        _showWaypointsToggle.onValueChanged.AddListener(value => SaveToggleSetting("ShowWaypoints", value));
        _skipIntroToggle.onValueChanged.AddListener(value => SaveToggleSetting("SkipIntro", value));
    }

    /// <summary>
    /// Registers the dialogue speed slider event to save changes and update the UI.
    /// </summary>
    private void SetupSliderListener() {
        _dialogueSpeedSlider.onValueChanged.AddListener(OnDialogueSpeedChanged);
    }

    /// <summary>
    /// Sets up the text language dropdown, loading saved preference if available.
    /// </summary>
    private void SetupTextLanguageDropdown() {
        _textLanguageDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;

        // Retrieve saved locale code; fallback to current selected locale.
        string savedLocaleCode = PlayerPrefs.GetString("Language_Text", LocalizationSettings.SelectedLocale.Identifier.Code);

        for (int i = 0; i < _availableLocales.Count; i++) {
            Locale locale = _availableLocales[i];
            options.Add(new TMP_Dropdown.OptionData(GetDisplayName(locale)));
            if (locale.Identifier.Code == savedLocaleCode) {
                selectedIndex = i;
            }
        }

        _textLanguageDropdown.AddOptions(options);
        _textLanguageDropdown.value = selectedIndex;
        _textLanguageDropdown.onValueChanged.AddListener(OnTextLanguageChanged);
    }

    /// <summary>
    /// Sets up the audio language dropdown with a static list of languages.
    /// </summary>
    private void SetupAudioLanguageDropdown() {
        _audioLanguageDropdown.ClearOptions();
        // Static list for supported voice languages; extend with additional languages as needed.
        var options = new List<TMP_Dropdown.OptionData>
        {
            new("English")
        };

        _audioLanguageDropdown.AddOptions(options);

        // Load saved audio language; default to "English" if not found.
        string savedVoiceLang = PlayerPrefs.GetString("Language_Audio", "English");
        int selectedIndex = options.FindIndex(o => o.text == savedVoiceLang);
        if (selectedIndex < 0) {
            selectedIndex = 0;
        }
        _audioLanguageDropdown.value = selectedIndex;
        _audioLanguageDropdown.onValueChanged.AddListener(OnAudioLanguageChanged);
    }

    /// <summary>
    /// Gets a display-friendly name for the locale using CultureInfo.
    /// Returns the native name if available, otherwise falls back to LocaleName or code.
    /// </summary>
    private string GetDisplayName(Locale locale) {
        try {
            return CultureInfo.GetCultureInfo(locale.Identifier.Code).NativeName;
        } catch {
            return !string.IsNullOrEmpty(locale.LocaleName) ? locale.LocaleName : locale.Identifier.Code;
        }
    }

    /// <summary>
    /// Called when the text language dropdown selection changes.
    /// Updates the current locale and saves the selection.
    /// </summary>
    private void OnTextLanguageChanged(int index) {
        if (index >= 0 && index < _availableLocales.Count) {
            Locale selectedLocale = _availableLocales[index];
            LocalizationSettings.SelectedLocale = selectedLocale;
            PlayerPrefs.SetString("Language_Text", selectedLocale.Identifier.Code);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Called when the audio language dropdown selection changes.
    /// Saves the selected voice language setting.
    /// </summary>
    private void OnAudioLanguageChanged(int index) {
        TMP_Dropdown.OptionData option = _audioLanguageDropdown.options[index];
        PlayerPrefs.SetString("Language_Audio", option.text);
        PlayerPrefs.Save();
        // Additional integration (e.g., with AudioManager) can be added here.
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Saves the state of a toggle control to PlayerPrefs.
    /// </summary>
    private void SaveToggleSetting(string key, bool value) {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Called when the dialogue speed slider value changes.
    /// Saves the new dialogue speed and updates its UI text.
    /// </summary>
    private void OnDialogueSpeedChanged(float value) {
        PlayerPrefs.SetFloat("DialogueSpeed", value);
        PlayerPrefs.Save();
        UpdateDialogueSpeedText(value);
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Loads saved settings from PlayerPrefs and applies them to the UI.
    /// </summary>
    private void LoadSettings() {
        _showNpcNamesToggle.isOn = PlayerPrefs.GetInt("ShowNPCNames", 0) == 1;
        _showTooltipsToggle.isOn = PlayerPrefs.GetInt("ShowTooltips", 0) == 1;
        _showWaypointsToggle.isOn = PlayerPrefs.GetInt("ShowWaypoints", 0) == 1;
        _skipIntroToggle.isOn = PlayerPrefs.GetInt("SkipIntro", 0) == 1;

        float speed = PlayerPrefs.GetFloat("DialogueSpeed", 0.04f);
        _dialogueSpeedSlider.value = speed;
        UpdateDialogueSpeedText(speed);
    }

    /// <summary>
    /// Updates the dialogue speed display to show the current value.
    /// </summary>
    private void UpdateDialogueSpeedText(float value) {
        if (_dialogueSpeedValueText != null) {
            _dialogueSpeedValueText.text = $"{value:0.000} sec/char";
        }
    }
}
