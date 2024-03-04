using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class QuestManager : NetworkBehaviour, IDataPersistance {
    [Header("Config")]
    [SerializeField] private bool _loadQuestState = true;

    private Dictionary<string, Quest> _questMap;
    private List<QuestData> _questDatas;

    private void Awake() {
        _questDatas = new List<QuestData>();
        _questMap = new Dictionary<string, Quest>();
    }

    public override void OnNetworkSpawn() {
        _questMap = CreateQuestMap();

        foreach (Quest quest in _questMap.Values) {
            if (quest.QuestState == QuestState.IN_PROGRESS) {
                quest.InstantiateCurrentQuestStep(transform);
            }

            EventsManager.Instance.QuestEvents.QuestStateChange(quest);
        }
    }

    private void Start() {
        EventsManager.Instance.QuestEvents.OnStartQuest += StartQuest;
        EventsManager.Instance.QuestEvents.OnAdvanceQuest += AdvanceQuest;
        EventsManager.Instance.QuestEvents.OnFinishQuest += FinishQuest;

        EventsManager.Instance.QuestEvents.OnQuestStepStateChange += QuestStepStateChange;
    }

    private void OnDestroy() {
        EventsManager.Instance.QuestEvents.OnStartQuest -= StartQuest;
        EventsManager.Instance.QuestEvents.OnAdvanceQuest -= AdvanceQuest;
        EventsManager.Instance.QuestEvents.OnFinishQuest -= FinishQuest;

        EventsManager.Instance.QuestEvents.OnQuestStepStateChange -= QuestStepStateChange;
    }

    private void Update() {
        foreach (Quest quest in _questMap.Values) {
            if (quest.QuestState == QuestState.REQUIREMENTS_NOT_MET && CheckRequirementsMet(quest)) {
                ChangeQuestState(quest.QuestInfoSO.Id, QuestState.CAN_START);
            }
        }
    }

    private void ChangeQuestState(string id, QuestState state) {
        Quest quest = GetQuestById(id);
        quest.QuestState = state;
        EventsManager.Instance.QuestEvents.QuestStateChange(quest);
    }

    private bool CheckRequirementsMet(Quest quest) {
        bool meetsRequirements = true;

        // Check quest prerequisites
        foreach (QuestInfoSO prerequisiteQuestInfo in quest.QuestInfoSO.QuestPrerequisires) {
            if (GetQuestById(prerequisiteQuestInfo.Id).QuestState != QuestState.FINISHED) {
                meetsRequirements = false;
                break;
            }
        }

        return meetsRequirements;
    }

    private void StartQuest(string id) {
        Quest quest = GetQuestById(id);
        quest.InstantiateCurrentQuestStep(transform);
        ChangeQuestState(id, QuestState.IN_PROGRESS);
    }

    private void AdvanceQuest(string id) {
        Quest quest = GetQuestById(id);

        // move to next step
        quest.MoveToNextStep();

        // if there are more steps, instantiate the next one
        if (quest.CurrentStepExists()) {
            quest.InstantiateCurrentQuestStep(transform);
        } else {
            // else there are no more steps, finish the quest
            ChangeQuestState(id, QuestState.CAN_FINISH);
        }
    }

    private void FinishQuest(string id) {
        Quest quest = GetQuestById(id);

        ClaimRewards(quest);
        ChangeQuestState(id, QuestState.FINISHED);
    }

    private void ClaimRewards(Quest quest) {
        //###TODO### Add rewards to player
        Debug.Log("Rewards claimed for quest");
    }

    private void QuestStepStateChange(string id, int stepIndex, QuestStepState questStepState) {
        Quest quest = GetQuestById(id);
        quest.StoreQuestStepState(questStepState, stepIndex);
        ChangeQuestState(id, quest.QuestState);
    }

    private Dictionary<string, Quest> CreateQuestMap() {
        // Loads all QuestInfoSO under the Assets/Resources/Quests folder
        QuestInfoSO[] allQuests = Resources.LoadAll<QuestInfoSO>("Quests");

        // Create the quest map
        var idToQuestMap = new Dictionary<string, Quest>();
        foreach (QuestInfoSO questInfoSO in allQuests) {
            if (idToQuestMap.ContainsKey(questInfoSO.Id)) {
                Debug.LogError("Duplicate ID found when creating quest map: " + questInfoSO.Id);
            }

            idToQuestMap.Add(questInfoSO.Id, LoadQuest(questInfoSO));
        }

        return idToQuestMap;
    }

    private Quest GetQuestById(string id) {
        Quest quest = _questMap[id];
        if (quest == null) {
            Debug.LogError("Quest not found in the Quest Map: " + id);
        }

        return quest;
    }

    public void SaveData(GameData data) {
        // Create a new instance of ToSaveCropData to store the data from the crops container
        var toSaveQuestData = new ToSaveQuestData();
        foreach (Quest quest in _questMap.Values) {
            try {
                QuestData questData = quest.GetQuestData();
                toSaveQuestData.ToSaveData.Add(questData);
            } catch (System.Exception e) {
                Debug.LogError("Failed to save quest with id " + quest.QuestInfoSO.Id + ": " + e);
            }
        }
        data.QuestData = JsonUtility.ToJson(toSaveQuestData);
    }

    public void LoadData(GameData data) {
        // Check if an found item container exists
        if (string.IsNullOrEmpty(data.QuestData)) {
            return;
        }

        _questDatas = new List<QuestData>();
        var toLoadQuestDatas = JsonUtility.FromJson<ToSaveQuestData>(data.QuestData);
        foreach (QuestData questData in toLoadQuestDatas.ToSaveData) {
            _questDatas.Add(questData);
        }
    }

    private Quest LoadQuest(QuestInfoSO questInfoSO) {
        if (_questDatas.Count == 0) {
            return new Quest(questInfoSO);
        }

        Quest quest = null;
        try {
            foreach (QuestData questData in _questDatas) {
                if (questData.QuestId == questInfoSO.Id) {
                    quest = new Quest(questInfoSO, questData.QuestState, questData.QuestStepIndex, questData.QuestStepStates);
                    _questDatas.Remove(questData);
                    break;
                } else {
                    quest = new Quest(questInfoSO);
                }
            }
        } catch (System.Exception e) {
            Debug.LogError("Failed to load quest with id " + questInfoSO.Id + ": " + e);
        }

        return quest;
    }
}
