using System.Collections;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CreditsStepImageDual", menuName = "MainMenu/Credits/Step/ImageDual")]
public class CreditsStepImageDual : CreditsStepBase {
    [SerializeField] private Image _leftImagePrefab;
    [SerializeField] private Image _rightImagePrefab;
    [SerializeField] private float _offsetPixels = 100f;
    private readonly float _horizontalOffset = 300f;

    private float _lastCalculatedDelay = 0f;
    public override float GetStepDelay() => _lastCalculatedDelay;

    public override IEnumerator RunStep(GameObject context) {
        float effectiveSpeed = _scrollSpeed * (Screen.height / _refHeight);

        var left = Instantiate(_leftImagePrefab, context.transform);
        var right = Instantiate(_rightImagePrefab, context.transform);

        var lRect = left.GetComponent<RectTransform>();
        var rRect = right.GetComponent<RectTransform>();

        float startY = -Screen.height - _offsetSpawnPosition;
        lRect.anchoredPosition = new Vector2(-_horizontalOffset, startY);
        rRect.anchoredPosition = new Vector2(+_horizontalOffset, startY);

        float visualHeight = Mathf.Max(
            lRect.rect.height * lRect.lossyScale.y,
            rRect.rect.height * rRect.lossyScale.y
        );

        _lastCalculatedDelay = (visualHeight + _offsetPixels) / effectiveSpeed;

        context.GetComponent<MonoBehaviour>().StartCoroutine(ScrollAndDestroy(lRect, rRect, left, right, effectiveSpeed));
        yield break;
    }

    private IEnumerator ScrollAndDestroy(RectTransform lRect, RectTransform rRect, Image left, Image right, float speed) {
        yield return null;

        while (lRect.anchoredPosition.y < Screen.height + _offsetPixels &&
               rRect.anchoredPosition.y < Screen.height + _offsetPixels) {
            if (CanSkip && InputManager.Instance.CreditsSkipPressed()) break;

            float s = InputManager.Instance.CreditsFastForwardPressed() ? speed * 3f : speed;
            lRect.anchoredPosition += new Vector2(0, s * Time.deltaTime);
            rRect.anchoredPosition += new Vector2(0, s * Time.deltaTime);
            yield return null;
        }

        Destroy(left.gameObject);
        Destroy(right.gameObject);
    }
}
