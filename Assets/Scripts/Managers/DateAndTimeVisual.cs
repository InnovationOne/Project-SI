using TMPro;
using UnityEngine;

public class DateAndTimeVisual : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _dateText;
    [SerializeField] private TextMeshProUGUI _timeText;


    private void Start() {
        TimeAndWeatherManager.Instance.OnUpdateUITime += TimeAndWeatherManager_OnUpdateTimeText;
        TimeAndWeatherManager.Instance.OnUpdateUIDate += TimeAndWeatherManager_OnUpdateDateText;
        PauseGameManager.Instance.OnShowLocalPauseGame += PauseMenuController_OnTogglePauseMenu;
    }

    private void TimeAndWeatherManager_OnUpdateTimeText(int currentHour, int currentMinute) {
        _timeText.text = $"{currentHour:00}:{currentMinute:00}";
    }

    private void TimeAndWeatherManager_OnUpdateDateText(int currentDay, int currentSeason, int currentYear) {
        TimeAndWeatherManager.ShortDayName dayName = (TimeAndWeatherManager.ShortDayName)(currentDay % TimeAndWeatherManager.DAYS_PER_WEEK);
        TimeAndWeatherManager.SeasonName seasonName = (TimeAndWeatherManager.SeasonName)(currentSeason);

        _dateText.text = 
            $"{dayName} {currentDay + 1}. {seasonName}\n" +
            $"Year {currentYear + 1}";
    }

    // Hide the panel
    private void PauseMenuController_OnTogglePauseMenu() {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
