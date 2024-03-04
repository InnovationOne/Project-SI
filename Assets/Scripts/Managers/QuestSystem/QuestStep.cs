using UnityEngine;

public abstract class QuestStep : MonoBehaviour {
    private bool _isFinished = false;
    private string _questID;
    private int _stepIndex;

    public void InitializeQuestStep(string questId, int stepIndex, string questStepState) {
        _questID = questId;
        _stepIndex = stepIndex;
        if (questStepState != null && questStepState != string.Empty) {
            SetQuestStepState(questStepState);
        }
    }

    protected void FinishQuestStep() {
        if (!_isFinished) {
            _isFinished = true;

            EventsManager.Instance.QuestEvents.AdvanceQuest(_questID);

            Destroy(gameObject);
        }
    }

    protected void ChangeState(string newState) {
        EventsManager.Instance.QuestEvents.QuestStepStateChange(_questID, _stepIndex, new QuestStepState(newState));
    }

    protected abstract void SetQuestStepState(string state);
}
