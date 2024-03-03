using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestInfo : MonoBehaviour {
    [SerializeField] private RectTransform _questHeader;
    [SerializeField] private TextMeshProUGUI _questGiverText;
    [SerializeField] private TextMeshProUGUI _questBodyText;
    [SerializeField] private Slider _questProgressBarSlider;
    [SerializeField] private RectTransform _rectTransform;

    private const int QUEST_BODY_HIGHT_CORRECTURE = 6;
    private const int QUEST_PROGRESS_BAR_HIGHT = 1;

    /*
    public void SetQuest(QuestSO questSO) {
        _questGiverText.text = questSO.QuestGiverName;

        float questCompletetAmount = 0;

        StringBuilder questStringBuilder = new();
        questStringBuilder.Append("<u>" + questSO.QuestName + "</u>");
        
        for (int i = 0; i < questSO.ItemsToComplete.Length; i++) {
            if (questSO.ItemsAmountCompleted[i] < questSO.ItemsToComplete[i].Amount) {
                questStringBuilder.Append("\n" + questSO.QuestActionTexts[i]);
                questStringBuilder.Append("\n" + questSO.ItemsAmountCompleted[i] + " / " + questSO.ItemsToComplete[i].Amount);
                questCompletetAmount += (float)questSO.ItemsAmountCompleted[i] / (float)questSO.ItemsToComplete[i].Amount;
            } else {
                questStringBuilder.Append("\n" + "<color=#86816f><s>" + questSO.QuestActionTexts[i] + "</s></color>");
                questStringBuilder.Append("\n" + "<color=#86816f><s>" + questSO.ItemsAmountCompleted[i] + " / " + questSO.ItemsToComplete[i].Amount + "</s></color>");
                questCompletetAmount++;
            }
        }

        // Kill monsters
        // for (int i = 0; i < questSO.)

        _questBodyText.text = questStringBuilder.ToString();

        SetProgressBar(questCompletetAmount, questSO.ItemsToComplete.Length);
        SetNewSize();
    }
    */
    private void SetProgressBar(float questCompletetAmount, int questCount) {
        _questProgressBarSlider.value = _questProgressBarSlider.maxValue / questCount * questCompletetAmount;
    }

    private void SetNewSize() {
        _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, 
            _questHeader.sizeDelta.y + _questBodyText.preferredHeight + QUEST_BODY_HIGHT_CORRECTURE + QUEST_PROGRESS_BAR_HIGHT);
    }
}
