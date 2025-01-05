using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class QuestPoint : MonoBehaviour {
    [Header("Quest")]
    [SerializeField] private QuestInfoSO _questInfoForPoint;

    [Header("Config")]
    [SerializeField] private bool _startPoint = true;
    [SerializeField] private bool _finishPoint = true;

    private bool _playerInRange = false;
    private string _questId;
    private QuestState _currentQuestState;
    private QuestIcon _questIcon;

    private void Awake() {
        _questId = _questInfoForPoint.Id;
        _questIcon = GetComponentInChildren<QuestIcon>();
    }

    private void Start() {
        GameManager.Instance.EventsManager.QuestEvents.OnQuestStateChange += EventsManager_OnQuestStateChange;
        GameManager.Instance.InputManager.OnInteractAction += InputManager_OnInteractAction;
    }

    private void OnDestroy() {
        GameManager.Instance.EventsManager.QuestEvents.OnQuestStateChange -= EventsManager_OnQuestStateChange;
        GameManager.Instance.InputManager.OnInteractAction -= InputManager_OnInteractAction;
    }

    private void InputManager_OnInteractAction() {
        if (!_playerInRange) {
            return;
        }

        if (_currentQuestState.Equals(QuestState.CAN_START) && _startPoint) {
            GameManager.Instance.EventsManager.QuestEvents.StartQuest(_questId);
        } else if (_currentQuestState.Equals(QuestState.CAN_FINISH) && _finishPoint) {
            GameManager.Instance.EventsManager.QuestEvents.FinishQuest(_questId);
        }
    }

    private void EventsManager_OnQuestStateChange(Quest quest) {
        if (quest.QuestInfoSO.Id.Equals(_questId)) {
            _currentQuestState = quest.QuestState;
            _questIcon.SetState(_currentQuestState, _startPoint, _finishPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<PlayerController>(out var player)) {
            _playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.TryGetComponent<PlayerController>(out var player)) {
            _playerInRange = false;
        }
    }
}
