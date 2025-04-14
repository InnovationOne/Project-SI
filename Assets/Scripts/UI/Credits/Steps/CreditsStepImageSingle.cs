using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CreditsStepImageSingle", menuName = "MainMenu/Credits/Step/ImageSingle")]
public class CreditsStepImageSingle : CreditsStepBase {
    [SerializeField] private Image _imagePrefab;
    [SerializeField] private float _offsetPixels = 100f;

    private float _lastCalculatedDelay = 0f;
    public override float GetStepDelay() => _lastCalculatedDelay;

    public override IEnumerator RunStep(GameObject context) {
        var image = Instantiate(_imagePrefab, context.transform);

        var rect = image.GetComponent<RectTransform>();
        float effectiveSpeed = _scrollSpeed * (Screen.height / _refHeight);

        float visualHeight = rect.rect.height * rect.lossyScale.y;
        _lastCalculatedDelay = (visualHeight + _offsetPixels) / effectiveSpeed;

        rect.anchoredPosition = new Vector2(0, -Screen.height - _offsetSpawnPosition);
        context.GetComponent<MonoBehaviour>().StartCoroutine(ScrollAndDestroy(rect, image, effectiveSpeed));
        yield break;
    }

    private IEnumerator ScrollAndDestroy(RectTransform rect, Image image, float speed) {
        yield return null;

        while (rect.anchoredPosition.y < Screen.height + _offsetPixels) {
            if (CanSkip && InputManager.Instance.CreditsSkipPressed()) break;
            float s = InputManager.Instance.CreditsFastForwardPressed() ? speed * 3f : speed;
            rect.anchoredPosition += new Vector2(0, s * Time.deltaTime);
            yield return null;
        }

        Destroy(image.gameObject);
    }
}