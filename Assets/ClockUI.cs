using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour {
    [Header("Date and Time")]
    [SerializeField] TextMeshProUGUI _dateText;
    [SerializeField] TextMeshProUGUI _timeText;

    [Header("Weather")]
    [SerializeField] Image[] _weatherForecastImages;
    [SerializeField] Sprite[] _weatherSpriteIconsColor;
    [SerializeField] Sprite[] _weatherSpriteIcons;

    [Header("Money")]
    [SerializeField] TextMeshProUGUI _moneyText;

    const int NUM_DIGITS = 8;

    void Start() {
        GameManager.Instance.TimeManager.OnUpdateUITime += HandleTimeUpdate;
        GameManager.Instance.TimeManager.OnUpdateUIDate += HandleDateUpdate;
        GameManager.Instance.WeatherManager.OnUpdateUIWeather += HandleWeatherUpdate;
        GameManager.Instance.FinanceManager.OnFarmMoneyChanged += UpdateFarmMoney;
        GameManager.Instance.FinanceManager.OnTownMoneyChanged += UpdateTownMoney;
        GameManager.Instance.PauseGameManager.OnShowLocalPauseGame += TogglePauseMenu;
    }

    void OnDestroy() {
        if (GameManager.Instance != null) {
            GameManager.Instance.TimeManager.OnUpdateUITime -= HandleTimeUpdate;
            GameManager.Instance.TimeManager.OnUpdateUIDate -= HandleDateUpdate;
            GameManager.Instance.WeatherManager.OnUpdateUIWeather -= HandleWeatherUpdate;
            GameManager.Instance.FinanceManager.OnFarmMoneyChanged -= UpdateFarmMoney;
            GameManager.Instance.FinanceManager.OnTownMoneyChanged -= UpdateTownMoney;
            GameManager.Instance.PauseGameManager.OnShowLocalPauseGame -= TogglePauseMenu;
        }
    }

    // Displays the current hour and minute in two UI text fields.
    private void HandleTimeUpdate(int currentHour, int currentMinute) {
        string timeString = $"{currentHour:00}:{currentMinute:00}";
        _timeText.text = timeString;
    }

    // Displays the current day, season, and year in the date UI text field.
    void HandleDateUpdate(int currentDay, int currentSeason, int currentYear) {
        int idx = currentDay % TimeManager.DAYS_PER_WEEK;
        var dayName = (TimeManager.ShortDayName)idx;
        TimeManager.DateSuffix dateSuffix;
        if (idx >= Enum.GetNames(typeof(TimeManager.DateSuffix)).Length) {
            dateSuffix = TimeManager.DateSuffix.th;
        } else {
            dateSuffix = (TimeManager.DateSuffix)idx;
        }

        // Format date
        _dateText.text = $"{dayName},\n{currentDay}{dateSuffix}";
    }

    // Toggles this UI object's visibility, primarily for the pause menu.
    void TogglePauseMenu() => gameObject.SetActive(!gameObject.activeSelf);

    // Updates weather forecast images based on provided weather data and station.
    void HandleWeatherUpdate(int[] weather, int weatherStation) {
        _weatherForecastImages[0].sprite = _weatherSpriteIconsColor[weather[0]];
        _weatherForecastImages[1].sprite = _weatherSpriteIcons[weather[1]];
        _weatherForecastImages[2].sprite = _weatherSpriteIcons[weather[2]];

        switch (weatherStation) {
            case 0:
                _weatherForecastImages[1].sprite = _weatherSpriteIcons[^1];
                _weatherForecastImages[2].sprite = _weatherSpriteIcons[^1];
                break;
            case 1:
                _weatherForecastImages[2].sprite = _weatherSpriteIcons[^1];
                break;
            case 2:
                break;
            default:
                Debug.LogError($"Invalid weather station index: {weatherStation}");
                break;
        }
    }

    // Updates the displayed farm money using a formatted string with leading zeros.
    void UpdateFarmMoney(int farmMoney) {
        int zeroCount = NUM_DIGITS - farmMoney.ToString().Length;
        StringBuilder sb = new();
        sb.Append("<color=#80775c>");
        for (int i = 0; i < zeroCount; i++) {
            sb.Append('0');
        }
        sb.Append("</color>");
        sb.Append(farmMoney.ToString());

        _moneyText.text = sb.ToString();
    }

    // Placeholder for town money updates.
    void UpdateTownMoney(int townMoney) {
        // Extend here for town money display.
        return;
    }
}
