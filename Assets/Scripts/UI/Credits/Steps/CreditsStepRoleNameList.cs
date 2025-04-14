using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "CreditsStepRoleNameList", menuName = "MainMenu/Credits/Step/RoleNameList")]
public class CreditsStepRoleNameList : CreditsStepBase {
    [SerializeField] private GameObject _entryPrefab;
    [SerializeField] private List<CreditsEntry> _entries = new();
    [SerializeField] private float _offsetPixels = 100f;
    private readonly float _entrySpacing = 16f;
    private readonly float _offsetSpawnPosition = 50f;

    private float _lastCalculatedDelay = 0f;
    public override float GetStepDelay() => _lastCalculatedDelay;

    public override IEnumerator RunStep(GameObject context) {
        float scale = Screen.height / _refHeight;
        float effectiveSpeed = _scrollSpeed * scale;
        float spacing = _entrySpacing * scale;
        float totalHeight = (_entries.Count * spacing);

        _lastCalculatedDelay = (totalHeight + _offsetPixels) / effectiveSpeed;

        for (int i = 0; i < _entries.Count; i++) {
            var entry = _entries[i];
            var go = Instantiate(_entryPrefab, context.transform);

            var texts = go.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length < 2) continue;

            texts[0].text = entry.Role;
            texts[0].alignment = TextAlignmentOptions.Right;
            texts[1].text = entry.Name;
            texts[1].alignment = TextAlignmentOptions.Left;

            texts[0].fontSize = texts[1].fontSize = 11;

            var rect = go.GetComponent<RectTransform>();
            float yOffset = -Screen.height - _offsetSpawnPosition - i * spacing;
            rect.anchoredPosition = new Vector2(0, yOffset);

            context.GetComponent<MonoBehaviour>().StartCoroutine(ScrollAndDestroy(rect, go, effectiveSpeed));
        }

        yield break;
    }

    private IEnumerator ScrollAndDestroy(RectTransform rect, GameObject go, float speed) {
        yield return null;

        while (rect.anchoredPosition.y < Screen.height + _offsetPixels) {
            if (CanSkip && InputManager.Instance.CreditsSkipPressed()) break;

            float s = InputManager.Instance.CreditsFastForwardPressed() ? speed * 3f : speed;
            rect.anchoredPosition += new Vector2(0, s * Time.deltaTime);
            yield return null;
        }

        Destroy(go);
    }
}
