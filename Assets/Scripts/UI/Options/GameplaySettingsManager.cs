using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplaySettingsManager : MonoBehaviour
{
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

    private void Start() {
        LoadSettings();

        _textLanguageDropdown.onValueChanged.AddListener(i => {
            PlayerPrefs.SetString("Language_Text", _textLanguageDropdown.options[i].text);
        });
        _audioLanguageDropdown.onValueChanged.AddListener(i => {
            PlayerPrefs.SetString("Language_Audio", _audioLanguageDropdown.options[i].text);
        });

        _showNpcNamesToggle.onValueChanged.AddListener(val => PlayerPrefs.SetInt("ShowNPCNames", val ? 1 : 0));
        _showTooltipsToggle.onValueChanged.AddListener(val => PlayerPrefs.SetInt("ShowTooltips", val ? 1 : 0));
        _showWaypointsToggle.onValueChanged.AddListener(val => PlayerPrefs.SetInt("ShowWaypoints", val ? 1 : 0));
        _skipIntroToggle.onValueChanged.AddListener(val => PlayerPrefs.SetInt("SkipIntro", val ? 1 : 0));

        _dialogueSpeedSlider.onValueChanged.AddListener(val => {
            PlayerPrefs.SetFloat("DialogueSpeed", val);
            PlayerPrefs.Save();
            UpdateDialogueSpeedText(val);
        });
    }

    private void LoadSettings() {
        // Sprache
        string textLang = PlayerPrefs.GetString("Language_Text", "English");
        string audioLang = PlayerPrefs.GetString("Language_Audio", "English");
        _textLanguageDropdown.value = _textLanguageDropdown.options.FindIndex(o => o.text == textLang);
        _audioLanguageDropdown.value = _audioLanguageDropdown.options.FindIndex(o => o.text == audioLang);

        // Toggles
        _showNpcNamesToggle.isOn = PlayerPrefs.GetInt("ShowNPCNames", 1) == 1;
        _showTooltipsToggle.isOn = PlayerPrefs.GetInt("ShowTooltips", 1) == 1;
        _showWaypointsToggle.isOn = PlayerPrefs.GetInt("ShowWaypoints", 1) == 1;
        _skipIntroToggle.isOn = PlayerPrefs.GetInt("SkipIntro", 0) == 1;

        // Dialoggeschwindigkeit
        float speed = PlayerPrefs.GetFloat("DialogueSpeed", 0.04f);
        _dialogueSpeedSlider.value = speed;
        UpdateDialogueSpeedText(speed);
    }

    private void UpdateDialogueSpeedText(float value) {
        if (_dialogueSpeedValueText != null) {
            _dialogueSpeedValueText.text = $"{value:0.000} sec/char";
        }
    }
}
