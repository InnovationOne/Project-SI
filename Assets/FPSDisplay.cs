using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour {
    private float _deltaTime = 0.0f;
    private TextMeshProUGUI _text;

    private void Awake() {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void Update() {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

        int fps = Mathf.RoundToInt(1.0f / _deltaTime);
        string fpsText = string.Format($"FPS: {fps}");

        _text.text = fpsText;
    }
}

