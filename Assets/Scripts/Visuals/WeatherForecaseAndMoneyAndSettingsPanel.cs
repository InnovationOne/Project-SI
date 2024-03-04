using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeatherForecaseAndMoneyAndSettingsPanel : MonoBehaviour {
    public static WeatherForecaseAndMoneyAndSettingsPanel Instance { get; private set; }

    [Header("Weather sprites")]
    [SerializeField] private Sprite[] _weatherSprites;
    [SerializeField] private Sprite _questionmark;

    [Header("Weather components")]
    [SerializeField] private TextMeshProUGUI[] _weatherForcastText;
    [SerializeField] private Image[] _weatherForcastImages;

    [Header("Money components")]
    [SerializeField] private TextMeshProUGUI _moneyText;
    private const int COUNT_OF_MONEY_NUMBERS = 8;

    [Header("Settings components")]
    [SerializeField] private Button _settingsButton;


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one WeatherForecaseAndMoneyAndSettings in the scene.");
        } else {
            Instance = this;
        }

        _settingsButton.onClick.AddListener(() => PauseGameManager.Instance.InputManager_TogglePauseGame());
    }

    private void Start() {
        TimeAndWeatherManager.Instance.OnUpdateUIWeather += TimeAndWeatherManager_OnUpdateWeatherImage;
        FinanceManager.Instance.OnUpdateChanged += FinanceManager_OnUpdateMoney;
        PauseGameManager.Instance.OnShowLocalPauseGame += PauseMenuController_OnTogglePauseMenu;
    }

    private void PauseMenuController_OnTogglePauseMenu() {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void TimeAndWeatherManager_OnUpdateWeatherImage(int[] weather, int weatherstation) {
        //### When upgrading the weather system, this method needs to be updated ###

        // Set the weather text and sprite
        int currentDay = TimeAndWeatherManager.Instance.CurrentDay % TimeAndWeatherManager.DAYS_PER_SEASON;
        for (int i = 0; i < _weatherForcastText.Length; i++) {
            currentDay++;
            _weatherForcastText[i].text = currentDay + ".";
            _weatherForcastImages[i].sprite = _weatherSprites[weather[i]];
        }

        switch (weatherstation) {
            case 0:
                _weatherForcastImages[1].sprite = _questionmark;
                _weatherForcastImages[2].sprite = _questionmark;
                break;
            case 1:
                _weatherForcastImages[2].sprite = _questionmark;
                break;
            case 2:
                break;
            default:
                Debug.LogError("Weatherstation is set to an impossible value");
                break;
        }
    }

    private void FinanceManager_OnUpdateMoney(int moneyOfFarm) {
        int zeroCount = COUNT_OF_MONEY_NUMBERS - moneyOfFarm.ToString().Length;

        // Build the formatted string
        StringBuilder moneyTextStringBuilder = new();
        moneyTextStringBuilder.Append("<color=#80775c>");
        for (int i = 0; i < zeroCount; i++)
            moneyTextStringBuilder.Append('0');
        moneyTextStringBuilder.Append("</color>");
        moneyTextStringBuilder.Append(moneyOfFarm.ToString());

        _moneyText.text = moneyTextStringBuilder.ToString();
    }
}
