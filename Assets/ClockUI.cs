using Coffee.UIExtensions;
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TimeManager;
using static WeatherManager;

public class ClockUI : MonoBehaviour {
    [Header("Date and Time")]
    [SerializeField] TextMeshProUGUI _dateText;
    [SerializeField] TextMeshProUGUI _timeText;
    [SerializeField] Image _bigHand;
    [SerializeField] Sprite[] _bigHandSprites;
    [SerializeField] Image _smallHand;
    [SerializeField] Sprite[] _smallHandSprites;

    [Header("Weather")]
    [SerializeField] Image[] _weatherForecastImages;
    [SerializeField] Sprite[] _weatherIconsColor;
    [SerializeField] Sprite[] _weatherIcons;

    [Header("Season")]
    [SerializeField] Image _seasonImage;
    [SerializeField] Sprite[] _seasonSprites;
    [SerializeField] UIParticle[] _seasonParticles;

    [Header("Money")]
    [SerializeField] TextMeshProUGUI _moneyText;

    const int NUM_DIGITS = 8;
    const string COLOR_START_TAG = "<color=#80775c>";
    const string COLOR_END_TAG = "</color>";

    // Subscribe to game events.
    void Start() {
        GameManager.Instance.TimeManager.OnUpdateUITime += HandleTimeUpdate;
        GameManager.Instance.TimeManager.OnUpdateUIDate += HandleDateUpdate;
        GameManager.Instance.WeatherManager.OnUpdateUIWeather += HandleWeatherUpdate;
        GameManager.Instance.FinanceManager.OnFarmMoneyChanged += UpdateFarmMoney;
        GameManager.Instance.FinanceManager.OnTownMoneyChanged += UpdateTownMoney;
        GameManager.Instance.PauseGameManager.OnShowLocalPauseGame += TogglePauseMenu;
    }

    // Unsubscribe from events.
    void OnDestroy() {
        GameManager.Instance.TimeManager.OnUpdateUITime -= HandleTimeUpdate;
        GameManager.Instance.TimeManager.OnUpdateUIDate -= HandleDateUpdate;
        GameManager.Instance.WeatherManager.OnUpdateUIWeather -= HandleWeatherUpdate;
        GameManager.Instance.FinanceManager.OnFarmMoneyChanged -= UpdateFarmMoney;
        GameManager.Instance.FinanceManager.OnTownMoneyChanged -= UpdateTownMoney;
        GameManager.Instance.PauseGameManager.OnShowLocalPauseGame -= TogglePauseMenu;
    }

    // Updates the displayed time and clock hands.
    void HandleTimeUpdate(int currentHour, int currentMinute) {
        _timeText.text = $"{currentHour:00}:{currentMinute:00}";
        _bigHand.sprite = _bigHandSprites[currentHour % 12];
        _smallHand.sprite = _smallHandSprites[(currentMinute / 5) % 12];
    }

    void HandleDateUpdate(int currentDay, int currentSeason, int currentYear, int oldSeason) {
        int dayIndex = currentDay % DAYS_PER_WEEK;
        var dayName = (ShortDayName)dayIndex;
        DateSuffix dateSuffix = dayIndex >= Enum.GetNames(typeof(DateSuffix)).Length
            ? DateSuffix.th
            : (DateSuffix)dayIndex;
        _dateText.text = $"{dayName},\n{currentDay + 1}{dateSuffix}";

        if (currentSeason >= 0 && currentSeason < _seasonSprites.Length) {
            _seasonImage.sprite = _seasonSprites[currentSeason];
        }

        _seasonParticles[oldSeason].Stop();
        _seasonParticles[oldSeason].enabled = false;

        if (currentSeason >= 0 && currentSeason < _seasonParticles.Length) {
            _seasonParticles[currentSeason].enabled = true;
            _seasonParticles[currentSeason].Play();
        }
    }

    // Toggles the UI (e.g., pause menu) visibility.
    void TogglePauseMenu() {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    // Updates the weather forecast display.
    void HandleWeatherUpdate(int[] weather) {
        if (weather == null || weather.Length == 0) return;
        for (int i = 0; i < _weatherForecastImages.Length && i < weather.Length; i++) {
            if (weather[i] == (int)WeatherName.None) {
                _weatherForecastImages[i].sprite = _weatherIcons[^1]; // Question mark icon.
            } else {
                _weatherForecastImages[i].sprite = (i == 0)
                    ? _weatherIconsColor[weather[i]]
                    : _weatherIcons[weather[i]];
            }
        }
    }

    // Updates the farm money display with leading zeros and colored placeholders.
    void UpdateFarmMoney(int farmMoney) {
        farmMoney = Mathf.Max(0, farmMoney);
        string moneyString = farmMoney.ToString();
        int zeroCount = Mathf.Max(0, NUM_DIGITS - moneyString.Length);
        var sb = new StringBuilder(NUM_DIGITS + moneyString.Length + 20);

        if (farmMoney == 0) {
            if (zeroCount > 1) {
                sb.Append(COLOR_START_TAG).Append(new string('0', zeroCount - 1)).Append(COLOR_END_TAG);
            } else if (zeroCount == 1) {
                sb.Append(COLOR_START_TAG).Append('0').Append(COLOR_END_TAG);
            }
            sb.Append('0');
        } else {
            if (zeroCount > 0) {
                sb.Append(COLOR_START_TAG).Append(new string('0', zeroCount)).Append(COLOR_END_TAG);
            }
            sb.Append(moneyString);
        }
        _moneyText.text = sb.ToString();
    }

    // Placeholder for future town money display updates.
    void UpdateTownMoney(int townMoney) {
        // Future implementation.
    }
}
