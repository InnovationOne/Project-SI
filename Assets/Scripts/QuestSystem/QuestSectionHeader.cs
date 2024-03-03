using System.Text;
using TMPro;
using UnityEngine;

public class QuestSectionHeader : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _questSectionHeaderText;


    public void SetQuestSectionHeader(string text) {
        StringBuilder headerStringBuilder = new();

        for (int i = 0; i < text.Length; i++) {
            if (char.IsUpper(text[i]) && i != 0) {
                headerStringBuilder.Append(" ");
            }

            headerStringBuilder.Append(text[i]);
        }

        _questSectionHeaderText.text = headerStringBuilder.ToString();
    }
}
