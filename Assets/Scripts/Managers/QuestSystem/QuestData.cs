using System.Collections.Generic;

[System.Serializable]
public class QuestData {
    public string QuestId;
    public QuestState QuestState;
    public int QuestStepIndex;
    public QuestStepState[] QuestStepStates;

    public QuestData(string questId, QuestState questState, int questStepIndex, QuestStepState[] questStepStates) {
        QuestId = questId;
        QuestState = questState;
        QuestStepIndex = questStepIndex;
        QuestStepStates = questStepStates;
    }
}


[System.Serializable]
public class ToSaveQuestData {
    public List<QuestData> ToSaveData;

    public ToSaveQuestData() {
        ToSaveData = new List<QuestData>();
    }
}
