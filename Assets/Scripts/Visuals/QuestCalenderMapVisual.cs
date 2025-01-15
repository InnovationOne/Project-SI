using UnityEngine;
using UnityEngine.UI;

public enum QCMSubPanels {
    Quest,
    Calender,
    Map,
    none,
}

public class QuestCalenderMapVisual : MonoBehaviour {
    public static QuestCalenderMapVisual Instance { get; private set; }

    [SerializeField] private Button _questButton;
    [SerializeField] private Button _calenderButton;
    [SerializeField] private Button _mapButton;

    [SerializeField] private GameObject[] qcmSubPanels;
    [SerializeField] private Image[] qcmSubPanelsHighlight;

    private const float TIME_BETWEEN_FLASHES = 1f;
    private float _currentTime = 0f;
    private QCMSubPanels _lastOpenPanel = QCMSubPanels.none;
    private bool _newUnseenQuest = false;
    

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of QuestAndCalenderAndMapVisual in the scene!");
            return;
        }
        Instance = this;

        _questButton.onClick.AddListener(() => {
            SetSubPanel(QCMSubPanels.Quest);

            });
        _calenderButton.onClick.AddListener(() => SetSubPanel(QCMSubPanels.Calender));
        _mapButton.onClick.AddListener(() => SetSubPanel(QCMSubPanels.Map));

        foreach (var panelButtonHighlight in qcmSubPanelsHighlight) {
            panelButtonHighlight.gameObject.SetActive(false);
        }
    }

    private void Start() {
        //QuestManager.Instance.OnNewQuestAdded += QuestManager_OnNewQuestAdded;
        GameManager.Instance.PauseGameManager.OnShowUIForPause += PauseGameManager_OnShowUIForPause;
        GameManager.Instance.PauseGameManager.OnHideUIForPause += PauseGameManager_OnHideUIForPause;
    }

    private void Update() {
        // Flash quest highlight
        if (_newUnseenQuest) {
            _currentTime += Time.deltaTime;

            if (_currentTime >= TIME_BETWEEN_FLASHES) {
                qcmSubPanelsHighlight[(int)QCMSubPanels.Quest].gameObject.SetActive(!qcmSubPanelsHighlight[(int)QCMSubPanels.Quest].gameObject.activeSelf);
                _currentTime = 0f;
            }
        }
    }

    private void QuestManager_OnNewQuestAdded() {
        if (!_newUnseenQuest && _lastOpenPanel != QCMSubPanels.Quest) {
            _newUnseenQuest = true;
        }
    }

    private void PauseGameManager_OnShowUIForPause() {
        gameObject.SetActive(true);
    }

    private void PauseGameManager_OnHideUIForPause() {
        gameObject.SetActive(false);
    }

    // Sets a sub panel
    public void SetSubPanel(QCMSubPanels qcmSubPanel) {
        if (qcmSubPanel == _lastOpenPanel) {
            // Current panel is opened => Deactivate the current panel
            DeactivateOldPanel();
            _lastOpenPanel = QCMSubPanels.none;
        } else if (_lastOpenPanel == QCMSubPanels.none) {
            // Last panel was none => Activate new panel
            ActivateNewPanel(qcmSubPanel);
        } else {
            // Last open panel is another panel => Deactivate old panal and activate new panel
            DeactivateOldPanel();
            ActivateNewPanel(qcmSubPanel);
        }
    }

    private void ActivateNewPanel(QCMSubPanels qcmSubPanel) {
        if (_newUnseenQuest && qcmSubPanel == QCMSubPanels.Quest) {
            _newUnseenQuest = false;
        }

        qcmSubPanels[(int)qcmSubPanel].SetActive(true);
        qcmSubPanelsHighlight[(int)qcmSubPanel].gameObject.SetActive(true);
        _lastOpenPanel = qcmSubPanel;
    }

    private void DeactivateOldPanel() {
        qcmSubPanels[(int)_lastOpenPanel].SetActive(false);
        qcmSubPanelsHighlight[(int)_lastOpenPanel].gameObject.SetActive(false);

        // Enable the toolbelt selection after closing the quest panel
        if (_lastOpenPanel == QCMSubPanels.Quest) {
            PlayerController.LocalInstance.PlayerToolbeltController.LockToolbelt(false);
        }
    }

    private void OnDestroy() {
        //QuestManager.Instance.OnNewQuestAdded -= QuestManager_OnNewQuestAdded;
        GameManager.Instance.PauseGameManager.OnShowUIForPause -= PauseGameManager_OnShowUIForPause;
        GameManager.Instance.PauseGameManager.OnHideUIForPause -= PauseGameManager_OnHideUIForPause;
    }
}
