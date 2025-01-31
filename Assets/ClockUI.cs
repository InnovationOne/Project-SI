using Coffee.UIExtensions;
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

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

    void Start() {
        GameManager.Instance.TimeManager.OnUpdateUITime += HandleTimeUpdate;
        GameManager.Instance.TimeManager.OnUpdateUIDate += HandleDateUpdate;
        GameManager.Instance.WeatherManager.OnUpdateUIWeather += HandleWeatherUpdate;
        GameManager.Instance.FinanceManager.OnFarmMoneyChanged += UpdateFarmMoney;
        GameManager.Instance.FinanceManager.OnTownMoneyChanged += UpdateTownMoney;
        GameManager.Instance.PauseGameManager.OnShowLocalPauseGame += TogglePauseMenu;
    }

    void OnDestroy() {
        GameManager.Instance.TimeManager.OnUpdateUITime -= HandleTimeUpdate;
        GameManager.Instance.TimeManager.OnUpdateUIDate -= HandleDateUpdate;
        GameManager.Instance.WeatherManager.OnUpdateUIWeather -= HandleWeatherUpdate;
        GameManager.Instance.FinanceManager.OnFarmMoneyChanged -= UpdateFarmMoney;
        GameManager.Instance.FinanceManager.OnTownMoneyChanged -= UpdateTownMoney;
        GameManager.Instance.PauseGameManager.OnShowLocalPauseGame -= TogglePauseMenu;
    }

    void HandleTimeUpdate(int currentHour, int currentMinute) {
        // Display time in HH:MM format
        _timeText.text = $"{currentHour:00}:{currentMinute:00}";

        // Update clock hands based on hour and minute values
        _bigHand.sprite = _bigHandSprites[currentHour % 12];
        _smallHand.sprite = _smallHandSprites[(currentMinute / 5) % 12];
    }

    void HandleDateUpdate(int currentDay, int currentSeason, int currentYear, int oldSeason) {
        // Get day name and suffix from TimeManager
        int dayIndex = currentDay % TimeManager.DAYS_PER_WEEK;
        var dayName = (TimeManager.ShortDayName)dayIndex;

        // Use a safe cast for date suffix
        TimeManager.DateSuffix dateSuffix = dayIndex >= Enum.GetNames(typeof(TimeManager.DateSuffix)).Length
            ? TimeManager.DateSuffix.th
            : (TimeManager.DateSuffix)dayIndex;

        // Update date text
        _dateText.text = $"{dayName},\n{currentDay + 1}{dateSuffix}";

        // Update season image
        if (currentSeason >= 0 && currentSeason < _seasonSprites.Length) {
            _seasonImage.sprite = _seasonSprites[currentSeason];
        }

        // Disable and stop the currently active season’s particle system
        UIParticle oldParticle = _seasonParticles[oldSeason];
        oldParticle.Stop();
        oldParticle.enabled = false;

        // Enable and play the new season’s particle system
        if (currentSeason >= 0 && currentSeason < _seasonParticles.Length) {
            UIParticle newParticle = _seasonParticles[currentSeason];
            newParticle.enabled = true;
            newParticle.Play();
        }
    }

    void TogglePauseMenu() {
        // Toggle active state of this UI
        gameObject.SetActive(!gameObject.activeSelf);
    }

    void HandleWeatherUpdate(int[] weather, int weatherStation) {
        // Update forecast images safely by index
        if (weather.Length >= 3) {
            _weatherForecastImages[0].sprite = _weatherIconsColor[weather[0]];
            _weatherForecastImages[1].sprite = _weatherIcons[weather[1]];
            _weatherForecastImages[2].sprite = _weatherIcons[weather[2]];
        }

        // Adjust future forecast icons based on station
        switch (weatherStation) {
            case 0:
                _weatherForecastImages[1].sprite = _weatherIcons[^1];
                _weatherForecastImages[2].sprite = _weatherIcons[^1];
                break;
            case 1:
                _weatherForecastImages[2].sprite = _weatherIcons[^1];
                break;
            case 2:
                // No additional changes needed
                break;
            default:
                Debug.LogError($"Invalid weather station index: {weatherStation}");
                break;
        }
    }

    void UpdateFarmMoney(int farmMoney) {
        // Constrain money to non-negative
        farmMoney = Mathf.Max(0, farmMoney);

        // Convert to string once
        string moneyString = farmMoney.ToString();

        // Calculate leading zero count
        int zeroCount = NUM_DIGITS - moneyString.Length;
        zeroCount = Mathf.Max(0, zeroCount);

        // Build final text with color-coded zeros
        var sb = new StringBuilder(NUM_DIGITS + moneyString.Length + 20);

        if (farmMoney == 0) {
            if (zeroCount > 1) {
                sb.Append(COLOR_START_TAG);
                sb.Append(new string('0', zeroCount - 1));
                sb.Append(COLOR_END_TAG);
            } else if (zeroCount == 1) {
                sb.Append(COLOR_START_TAG).Append('0').Append(COLOR_END_TAG);
            }
            sb.Append('0');
        } else {
            if (zeroCount > 0) {
                sb.Append(COLOR_START_TAG);
                sb.Append(new string('0', zeroCount));
                sb.Append(COLOR_END_TAG);
            }
            sb.Append(moneyString);
        }

        // Apply to UI
        _moneyText.text = sb.ToString();
    }


    void UpdateTownMoney(int townMoney) {
        // Future extension for town money display
    }
}
