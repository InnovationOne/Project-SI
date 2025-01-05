using UnityEngine;

/// <summary>
/// Controls the exit door. In deluxe version: time and weather-controlled.
/// In large versions: time-controlled.
/// </summary>
public class DoorController : MonoBehaviour {
    [SerializeField] private bool _isWeatherControlled = false;
    [SerializeField] private bool _isTimeControlled = false;
    [SerializeField] private Collider2D _doorCollider;

    private const float OPEN_HOUR = 6f;
    private const float CLOSE_HOUR = 18f;

    private bool _isOpen = false;
    private TimeManager _timeManager;
    private WeatherManager _weatherManager;

    private void Start() {
        _timeManager = GameManager.Instance.TimeManager;
        _weatherManager = GameManager.Instance.WeatherManager;
    }

    public void OpenDoor() {
        _isOpen = true;
        if (_doorCollider != null) _doorCollider.enabled = false;
        Debug.Log("Door opened");
    }

    public void CloseDoor() {
        _isOpen = false;
        if (_doorCollider != null) _doorCollider.enabled = true;
        Debug.Log("Door closed");
    }

    private void Update() { 
        if (_isTimeControlled) {
            float hour = _timeManager.GetHours();
            if (hour >= OPEN_HOUR && hour <= CLOSE_HOUR  && !_isOpen) {
                if (_isWeatherControlled) {
                    if (!_weatherManager.RainThunderSnow()) {
                        OpenDoor();
                    } else {
                        CloseDoor();
                    }
                } else {
                    OpenDoor();
                }
            }

            if (hour >= CLOSE_HOUR && _isOpen) {
                CloseDoor();
            }
        }
    }
}
