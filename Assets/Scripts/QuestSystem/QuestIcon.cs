using UnityEngine;

public class QuestIcon : MonoBehaviour {
    [Header("Icons")]
    [SerializeField] private GameObject _canStartIcon;
    [SerializeField] private GameObject _canFinishIcon;

    public void SetState(QuestState newState, bool startPoint, bool finishPoint) {
        _canStartIcon.SetActive(false);
        _canFinishIcon.SetActive(false);

        switch (newState) {
            case QuestState.CAN_START:
                if (startPoint) {
                    _canStartIcon.SetActive(true);
                }
                break;
            case QuestState.CAN_FINISH:
                if (finishPoint) {
                    _canFinishIcon.SetActive(true);
                }
                break;
            case QuestState.REQUIREMENTS_NOT_MET:
            case QuestState.IN_PROGRESS:
            case QuestState.FINISHED:
                break;
            default:
                Debug.LogWarning("Quest State not recognized by switch statement for quest icon: " + newState);
                break;
        }
    }
}
