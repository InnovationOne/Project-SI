using TMPro;
using UnityEngine;

public class FixText : MonoBehaviour {
    private TextMeshProUGUI _text;
    private string _lastText;
    private bool _movedX;
    private bool _movedY;

    private void Awake() {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void Start() {
        UpdatePosition();
    }

    private void Update() {
        if (_lastText != _text.text) {
            _lastText = _text.text;

            UpdatePosition();
        }
    }

    private void UpdatePosition() {
        Vector3 textPosition = _text.rectTransform.position;

        if (_movedX) {
            textPosition.x -= 0.5f / Screen.dpi;
            _movedX = false;
        }

        if (_movedY) {
            textPosition.y -= 0.5f / Screen.dpi;
            _movedY = false;
        }

        float xPixel = Mathf.Round(textPosition.x * Screen.dpi) / Screen.dpi;
        float yPixel = Mathf.Round(textPosition.y * Screen.dpi) / Screen.dpi;

        if (Mathf.Abs(xPixel - textPosition.x) < 0.25f) {
            textPosition.x += 0.5f / Screen.dpi;
            _movedX = true;
        }

        if (Mathf.Abs(yPixel - textPosition.y) < 0.25f) {
            textPosition.y += 0.5f / Screen.dpi;
            _movedY = true;
        }

        _text.rectTransform.position = textPosition;
    }
}
