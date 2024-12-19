using System;
using TMPro;
using UnityEngine;

// Ensures text always aligns cleanly on screen pixels for clarity on various DPI screens.
public class FixText : MonoBehaviour {
    [SerializeField] TextMeshProUGUI _textComponent = null!;

    string _lastText = string.Empty;
    bool _movedX;
    bool _movedY;
    float _dpi;

    void Awake() {
        if (_textComponent == null) {
            _textComponent = GetComponent<TextMeshProUGUI>();
        }
        _dpi = Screen.dpi == 0 ? 96f : Screen.dpi;
    }

    void Start() {
        _lastText = _textComponent.text;
        UpdatePosition();
    }

    void Update() {
        if (!_lastText.Equals(_textComponent.text, StringComparison.Ordinal)) {
            _lastText = _textComponent.text;
            UpdatePosition();
        }
    }

    void UpdatePosition() {
        Vector3 pos = _textComponent.rectTransform.position;

        if (_movedX) {
            pos.x -= 0.5f / _dpi;
            _movedX = false;
        }

        if (_movedY) {
            pos.y -= 0.5f / _dpi;
            _movedY = false;
        }

        pos.x = AdjustAxisPosition(pos.x, ref _movedX);
        pos.y = AdjustAxisPosition(pos.y, ref _movedY);

        _textComponent.rectTransform.position = pos;
    }

    float AdjustAxisPosition(float axisPosition, ref bool movedAxis) {
        float pixelPos = Mathf.Round(axisPosition * _dpi) / _dpi;
        if (Mathf.Abs(pixelPos - axisPosition) < 0.25f) {
            axisPosition += 0.5f / _dpi;
            movedAxis = true;
        }
        return axisPosition;
    }
}
