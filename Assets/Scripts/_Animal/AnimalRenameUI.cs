using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalRenameUI : MonoBehaviour {
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private Button _okButton;
    [SerializeField] private Button _cancelButton;

    private Action<string> _onSubmit;

    public void Initialize(string currentName, Action<string> onSubmit) {
        _nameInput.text = currentName;
        _onSubmit = onSubmit;
        _okButton.onClick.AddListener(OnOk);
        _cancelButton.onClick.AddListener(() => Destroy(gameObject));
    }

    private void OnOk() {
        _onSubmit?.Invoke(_nameInput.text);
        Destroy(gameObject);
    }
}
