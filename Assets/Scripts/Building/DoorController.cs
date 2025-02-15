using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable {
    [SerializeField] private bool _isWeatherControlled = false;
    [SerializeField] private bool _isTimeControlled = false;
    [SerializeField] private Collider2D _doorCollider;

    private const float OPEN_HOUR = 6f;
    private const float CLOSE_HOUR = 18f;

    private bool _isOpen = false;
    private TimeManager _timeManager;
    private WeatherManager _weatherManager;

    public float MaxDistanceToPlayer => throw new System.NotImplementedException();

    private void Start() {
        _timeManager = GameManager.Instance.TimeManager;
        _weatherManager = GameManager.Instance.WeatherManager;
    }

    public void Interact(PlayerController player) {
        if (_isOpen) {
            Close();
        } else {
            Open();
        }
    }

    public void Open() {
        _isOpen = true;
        if (_doorCollider != null) _doorCollider.enabled = false;
        Debug.Log("Door opened");
    }

    public void Close() {
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
                        Open();
                    } else {
                        Close();
                    }
                } else {
                    Open();
                }
            }

            if (hour >= CLOSE_HOUR && _isOpen) {
                Close();
            }
        }
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }
    public void InitializePreLoad(int itemId) { }
}
