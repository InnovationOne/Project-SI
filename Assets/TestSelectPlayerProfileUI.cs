using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestSelectPlayerProfileUI : MonoBehaviour {
    [SerializeField] private Button _buttonPrefab;

    public void SpawnPlayerProfileButtons(int playerId) {
        var newButton = Instantiate(_buttonPrefab, transform);
        newButton.name = "Button_" + playerId.ToString();
        newButton.transform.SetParent(transform, false);
        newButton.GetComponentInChildren<TextMeshProUGUI>().text = playerId.ToString();
        newButton.onClick.AddListener(() => { Debug.Log("Button " + playerId.ToString() + " clicked!"); });
    }
}
