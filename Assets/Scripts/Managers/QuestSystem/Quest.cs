using UnityEngine;

public class Quest {
    public QuestInfoSO QuestInfoSO;
    public QuestState QuestState;

    private int _currentQuestStepIndex;
    private QuestStepState[] _questStepStates;

    public Quest(QuestInfoSO questInfoSO) {
        QuestInfoSO = questInfoSO;
        QuestState = QuestState.REQUIREMENTS_NOT_MET;
        _currentQuestStepIndex = 0;
        _questStepStates = new QuestStepState[questInfoSO.QuestStepPrefabs.Length];
        for (int i = 0; i < _questStepStates.Length; i++) {
            _questStepStates[i] = new QuestStepState();
        }
    }

    public Quest(QuestInfoSO questInfoSO, QuestState questState, int currentQuestStepIndex, QuestStepState[] questStepStates) {
        QuestInfoSO = questInfoSO;
        QuestState = questState;
        _currentQuestStepIndex = currentQuestStepIndex;
        _questStepStates = questStepStates;

        if (_questStepStates.Length != this.QuestInfoSO.QuestStepPrefabs.Length) {
            Debug.LogWarning("Quest Step Prefabs and Quest Step State are of different lengths. "
                + "This indicates something with the QuestInfo and the saved data is now out of sync. "
                + "Reset syour data - as this might cause issues. QuestId: " + QuestInfoSO.Id);
        }
    }

    public void MoveToNextStep() {
        _currentQuestStepIndex++;
    }

    public bool CurrentStepExists() {
        return _currentQuestStepIndex < QuestInfoSO.QuestStepPrefabs.Length;
    }

    public void InstantiateCurrentQuestStep(Transform parentTransform) {
        GameObject questStepPrefab = GetCurrentQuestStepPrefab();
        if (questStepPrefab != null) {
            QuestStep questStep = Object.Instantiate(questStepPrefab, parentTransform).GetComponent<QuestStep>();
            questStep.InitializeQuestStep(QuestInfoSO.Id, _currentQuestStepIndex, _questStepStates[_currentQuestStepIndex].State);
        }
    }

    public void StoreQuestStepState(QuestStepState questStepState, int stepIndex) {
        if (stepIndex < _questStepStates.Length) {
            _questStepStates[stepIndex] = questStepState;
        } else {
            Debug.LogWarning("Tried to store quest step state, but stepIndex was out of range: QuestId=" + QuestInfoSO.Id + ", StepIndex=" + stepIndex);
        }
    }

    public QuestData GetQuestData() {
        return new QuestData(QuestInfoSO.Id, QuestState, _currentQuestStepIndex, _questStepStates);
    }

    private GameObject GetCurrentQuestStepPrefab() {
        GameObject questStepPrefab = null;
        if (CurrentStepExists()) {
            questStepPrefab = QuestInfoSO.QuestStepPrefabs[_currentQuestStepIndex];
        } else {
            Debug.LogWarning("Tried to get quest step prefab, but stepIndex was out of range indicating that there's no current step: QuestId=" + QuestInfoSO.Id + ", stepIndex=" + _currentQuestStepIndex);
        }
        return questStepPrefab;
    }
}
