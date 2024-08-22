using TMPro;
using UnityEngine;

public class FixText : MonoBehaviour {
    private TextMeshProUGUI _text;
    private string _lastText;
    private bool _movedX;
    private bool _movedY;
    private float _dpi;


    private void Awake() {
        _text = GetComponent<TextMeshProUGUI>();
        _dpi = Screen.dpi;
    }

    private void Start() {
        _lastText = _text.text;
        UpdatePosition();
    }

    /// <summary>
    /// This method is called every frame to check if the text has changed and update the position accordingly.
    /// </summary>
    private void Update() {
        if (_lastText != _text.text) {
            _lastText = _text.text;

            UpdatePosition();
        }
    }

    /// <summary>
    /// Updates the position of the text element.
    /// </summary>
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

        textPosition.x = AdjustAxisPosition(textPosition.x, ref _movedX);
        textPosition.y = AdjustAxisPosition(textPosition.y, ref _movedY);

        _text.rectTransform.position = textPosition;
    }

    /// <summary>
    /// Adjusts the axis position based on the DPI (dots per inch) value.
    /// </summary>
    /// <param name="axisPosition">The original axis position.</param>
    /// <param name="movedAxis">A reference boolean indicating if the axis was moved.</param>
    /// <returns>The adjusted axis position.</returns>
    private float AdjustAxisPosition(float axisPosition, ref bool movedAxis) {
        float pixelPos = Mathf.Round(axisPosition * _dpi) / _dpi;
        if (Mathf.Abs(pixelPos - axisPosition) < 0.25f) {
            axisPosition += 0.5f / _dpi;
            movedAxis = true;
        }
        return axisPosition;
    }
}
