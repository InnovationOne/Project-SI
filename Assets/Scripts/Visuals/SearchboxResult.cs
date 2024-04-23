using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchboxResult : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _itemName;
    [SerializeField] private Button _button;

    private int _itemID;


    private void Awake() {
        //_button.onClick.AddListener(() => GetComponentInParent<WikiPanel>().ResultPressed(_itemID));
    }

    internal void SetSearchboxResultButton(int itemID, string itemName) {
        _itemID = itemID;
        _itemName.text = itemName;
    }
}
