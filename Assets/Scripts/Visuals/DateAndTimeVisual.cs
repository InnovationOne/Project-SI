using TMPro;
using UnityEngine;

public class DateAndTimeVisual : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _dateText;
    [SerializeField] private TextMeshProUGUI _timeText;


    private void Start() {
        TimeManager.Instance.OnUpdateUITime += TimeAndWeatherManager_OnUpdateTimeText;
        TimeManager.Instance.OnUpdateUIDate += TimeAndWeatherManager_OnUpdateDateText;
        PauseGameManager.Instance.OnShowLocalPauseGame += PauseMenuController_OnTogglePauseMenu;
    }

    private void TimeAndWeatherManager_OnUpdateTimeText(int currentHour, int currentMinute) {
        _timeText.text = $"{currentHour:00}:{currentMinute:00}";
    }

    private void TimeAndWeatherManager_OnUpdateDateText(int currentDay, int currentSeason, int currentYear) {
        TimeManager.ShortDayName dayName = (TimeManager.ShortDayName)(currentDay % TimeManager.DAYS_PER_WEEK);
        TimeManager.SeasonName seasonName = (TimeManager.SeasonName)(currentSeason);

        _dateText.text = 
            $"{dayName} {currentDay + 1}. {seasonName}\n" +
            $"Year {currentYear + 1}";
    }

    // Hide the panel
    private void PauseMenuController_OnTogglePauseMenu() {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
