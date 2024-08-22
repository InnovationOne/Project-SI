using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public static QuestPanel Instance { get; private set; }

    [SerializeField] private Transform _content;
    [SerializeField] private QuestInfo _questInfoPrefab;
    [SerializeField] private QuestSectionHeader _questSectionHeaderPrefab;

    private Button _closeButton;
        

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of QuestPanel in the scene!");
            return;
        }
        Instance = this;

        _closeButton = GetComponentInChildren<Button>();

        _closeButton.onClick.AddListener(() => {
            QuestCalenderMapVisual.Instance.SetSubPanel(QCMSubPanels.Quest);
            PlayerToolbeltController.LocalInstance.LockToolbelt(false);
            });

        gameObject.SetActive(false);
    }
    /*
    private void Start() {
        QuestManager.Instance.OnUpdateQuestsInContentbox += QuestManager_OnUpdateQuestInContentbox;

        gameObject.SetActive(false);
    }

    // Show all quest from the container
    private void QuestManager_OnUpdateQuestInContentbox(QuestContainerSO questContainer) {
        foreach (Transform child in _content) {
            Destroy(child.gameObject);
        }
        
        int questCountOfAType = 0;
        // Set the quest headers main story, side story, ...
        foreach (QuestManager.QuestType questType in (QuestManager.QuestType[])Enum.GetValues(typeof(QuestManager.QuestType))) {
            QuestSectionHeader questSectionHeader = Instantiate(_questSectionHeaderPrefab, _content);
            questSectionHeader.SetQuestSectionHeader(questType.ToString());

            // Add the quest types under the section
            foreach (QuestSO quest in questContainer.Quests) {
                if (quest.QuestType == questType) {
                    QuestInfo questInfo = Instantiate(_questInfoPrefab, _content);
                    questInfo.SetQuest(quest);

                    questCountOfAType++;
                }
            }

            // If the type has no content delete the header
            if (questCountOfAType == 0) {
                Destroy(questSectionHeader.gameObject);
            }
            
            questCountOfAType = 0;
            
        }
    }
    */

    public void OnPointerEnter(PointerEventData eventData) {
        // Block only from local player
        PlayerToolbeltController.LocalInstance.LockToolbelt(true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        // Block only from local player
        PlayerToolbeltController.LocalInstance.LockToolbelt(false);
    }

    /*
    private void OnDestroy() {
        QuestManager.Instance.OnUpdateQuestsInContentbox -= QuestManager_OnUpdateQuestInContentbox;
    }

    */
}
