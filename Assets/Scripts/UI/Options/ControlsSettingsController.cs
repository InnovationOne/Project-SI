using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlsSettingsController : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private Transform _rebindListContent;
    [SerializeField] private GameObject _actionRebindPrefab;
    [SerializeField] private Button _resetToDefaultButton;
    [SerializeField] private Toggle _controllerVibrationToggle;

    private PlayerInputActions _playerInputActions;

    private void Start() {
        _playerInputActions = InputManager.Instance.PlayerInputActions;
        PopulateRebindList();
        _resetToDefaultButton.onClick.AddListener(ResetToDefaults);
        _controllerVibrationToggle.isOn = PlayerPrefs.GetInt("ControllerVibration", 1) == 1;
        _controllerVibrationToggle.onValueChanged.AddListener(ToggleControllerVibration);
    }

    private void PopulateRebindList() {
        foreach (var action in _playerInputActions) {
            if (action.bindings.Count == 0) continue;

            for (int i = 0; i < action.bindings.Count; i++) {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;

                var go = Instantiate(_actionRebindPrefab, _rebindListContent);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                texts[0].text = action.name;
                texts[1].text = action.GetBindingDisplayString(i);

                var rebindButton = go.GetComponentInChildren<Button>();
                int bindingIndex = i;
                rebindButton.onClick.AddListener(() => StartRebinding(action, bindingIndex, texts[1]));
            }
        }
    }

    private void StartRebinding(InputAction action, int bindingIndex, TMP_Text bindingText) {
        action.Disable();
        bindingText.text = "Press key...";

        action.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(operation => {
                bindingText.text = action.GetBindingDisplayString(bindingIndex);
                operation.Dispose();
                action.Enable();
            })
            .Start();
    }

    private void ResetToDefaults() {
        _playerInputActions.asset.RemoveAllBindingOverrides();

        foreach (Transform child in _rebindListContent) {
            var texts = child.GetComponentsInChildren<TMP_Text>();
            var action = _playerInputActions.asset.FindAction(texts[0].text);
            if (action != null) {
                int bindingIndex = -1;
                for (int i = 0; i < action.bindings.Count; i++) {
                    var b = action.bindings[i];
                    if (!b.isComposite && !b.isPartOfComposite) {
                        bindingIndex = i;
                        break;
                    }
                }

                if (bindingIndex >= 0) {
                    texts[1].text = action.GetBindingDisplayString(bindingIndex);
                }
            }
        }
    }

    private void ToggleControllerVibration(bool enabled) {
        PlayerPrefs.SetInt("ControllerVibration", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
