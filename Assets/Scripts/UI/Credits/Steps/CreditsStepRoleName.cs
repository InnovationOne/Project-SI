using System.Collections;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "CreditsStepRoleName", menuName = "MainMenu/Credits/Step/Role+Name")]
public class CreditsStepRoleName : CreditsStepBase {
    [SerializeField] private TextMeshProUGUI _textPrefab;
    [SerializeField] private CreditsEntry _entrie;
    [SerializeField] private float _offsetPixels = 100f;

    private float _lastCalculatedDelay = 0f;
    public override float GetStepDelay() => _lastCalculatedDelay;

    public override IEnumerator RunStep(GameObject context) {
        var text = Instantiate(_textPrefab, context.transform);

        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;

        text.text = $"{_entrie.Role}\n{_entrie.Name}";

        var rect = text.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -Screen.height - _offsetSpawnPosition);

        float effectiveSpeed = _scrollSpeed * (Screen.height / _refHeight);
        float visualHeight = rect.rect.height * rect.lossyScale.y;
        _lastCalculatedDelay = (visualHeight + _offsetPixels) / effectiveSpeed;

        context.GetComponent<MonoBehaviour>().StartCoroutine(ScrollAndDestroy(rect, text, effectiveSpeed));
        yield break;
    }

    private IEnumerator ScrollAndDestroy(RectTransform rect, TMP_Text text, float speed) {
        yield return null;

        while (rect.anchoredPosition.y < Screen.height + _offsetPixels) {
            if (CanSkip && InputManager.Instance.CreditsSkipPressed()) break;
            float s = InputManager.Instance.CreditsFastForwardPressed() ? speed * 3f : speed;
            rect.anchoredPosition += new Vector2(0, s * Time.deltaTime);
            yield return null;
        }

        Destroy(text.gameObject);
    }
}
