using UnityEngine;
using UnityEngine.UI;

public class TestingCharacterSelectUI : MonoBehaviour {
    [SerializeField] private Button _readyButton;

    private void Awake() {
        _readyButton.onClick.AddListener(() => {
            CharacterSelectReadyManager.Instance.SetPlayerReady();
            });
    }
}
