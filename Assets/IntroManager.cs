using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour {
    [Header("Background")]
    [SerializeField] private Image _background;

    [Header("Alpha Version")]
    [SerializeField] private TextMeshProUGUI _alphaHeaderTextMeshPro;
    [TextArea][SerializeField] private string _alphaHeaderText;
    [SerializeField] private TextMeshProUGUI _alphaBodyTextMeshPro;
    [TextArea][SerializeField] private string _alphaBodyText;

    [Header("Photosensitivity Warning")]
    [SerializeField] private TextMeshProUGUI _photosensitivityHeaderTextMeshPro;
    [TextArea][SerializeField] private string _photosensitivityHeaderText;
    [SerializeField] private TextMeshProUGUI _photosensitivityBodyTextMeshPro;
    [TextArea][SerializeField] private string _photosensitivityBodyText;

    [Header("Logos")]
    [SerializeField] private Image _octiwareLogo;
    [SerializeField] private Image _fmodLogo;

    private const float STARTUP_TIME = 2f;
    private const float MINIMUM_TIME = 5f;
    private const float FADE_DURATION = 1f;

    private float _remainingTime;

    private void Awake() {
        _background.gameObject.SetActive(true);
        _photosensitivityHeaderTextMeshPro.gameObject.SetActive(false);
        _photosensitivityBodyTextMeshPro.gameObject.SetActive(false);
        _octiwareLogo.gameObject.SetActive(false);
        _fmodLogo.gameObject.SetActive(false);
    }

    private void Start() {
        _photosensitivityHeaderTextMeshPro.text = _photosensitivityHeaderText;
        _photosensitivityBodyTextMeshPro.text = _photosensitivityBodyText;
        StartCoroutine(FadeSequence());
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            _remainingTime = 0;
        }
    }

    private IEnumerator FadeSequence() {
        yield return new WaitForSeconds(STARTUP_TIME);

        // Alpha Version
        yield return ShowElement(_alphaHeaderTextMeshPro, _alphaBodyTextMeshPro);

        // Photosensitivity Warning
        yield return ShowElement(_photosensitivityHeaderTextMeshPro, _photosensitivityBodyTextMeshPro);

        // Octiware Logo
        yield return ShowElement(_octiwareLogo);

        // FMod Logo
        yield return ShowElement(_fmodLogo);

        // Background
        yield return HideElement(_background);

        Destroy(this);
    }

    private IEnumerator ShowElement(params Graphic[] graphics) {
        // Einblenden der Elemente
        foreach (var graphic in graphics) {
            graphic.gameObject.SetActive(true);
            StartCoroutine(Fade(graphic, 0f, 1f));
        }

        // Wartezeit, oder springe direkt weiter bei Klick
        _remainingTime = MINIMUM_TIME;
        while (_remainingTime > 0) {
            _remainingTime -= Time.deltaTime;
            yield return null;
        }

        // Ausblenden der Elemente
        foreach (var graphic in graphics) {
            StartCoroutine(Fade(graphic, 1f, 0f));
        }
        yield return new WaitForSeconds(FADE_DURATION);
        foreach (var graphic in graphics) {
            graphic.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideElement(Graphic graphic) {
        graphic.gameObject.SetActive(true);
        StartCoroutine(Fade(graphic, 1f, 0f));
        yield return new WaitForSeconds(FADE_DURATION);
        graphic.gameObject.SetActive(false);
    }

    private IEnumerator Fade(Graphic graphic, float startAlpha, float endAlpha) {
        Color currentColor = graphic.color;
        float elapsedTime = 0f;

        // Set initial alpha
        currentColor.a = startAlpha;
        graphic.color = currentColor;

        // Gradually change the alpha
        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / FADE_DURATION);
            currentColor.a = newAlpha;
            graphic.color = currentColor;
            yield return null;
        }

        // Ensure the final alpha value is set
        currentColor.a = endAlpha;
        graphic.color = currentColor;
    }
}