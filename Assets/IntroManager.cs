using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour {
    [SerializeField] private Image _bg;
    [SerializeField] private TextMeshProUGUI _photosensitibityHeaderTextMeshPro;
    [TextArea]
    [SerializeField] private string _photosensitibityHeaderText;
    [SerializeField] private TextMeshProUGUI _photosensitibityBodyTextMeshPro;
    [TextArea]
    [SerializeField] private string _photosensitibityBodyText;

    [SerializeField] private Image _octiwareLogo;
    [SerializeField] private Image _fmodLogo;

    private int _caseId;
    private const float MINIMUM_TIME = 5f;
    private float _currentTime = MINIMUM_TIME;

    private const float FADE_DURATION = 1f;

    private void Awake() {
        _bg.gameObject.SetActive(true);
        _photosensitibityHeaderTextMeshPro.gameObject.SetActive(false);
        _photosensitibityBodyTextMeshPro.gameObject.SetActive(false);
        _octiwareLogo.gameObject.SetActive(false);
        _fmodLogo.gameObject.SetActive(false);
    }

    private void Start() {
        _photosensitibityHeaderTextMeshPro.text = _photosensitibityHeaderText;
        _photosensitibityBodyTextMeshPro.text = _photosensitibityBodyText;

        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence() {
        yield return new WaitForSeconds(MINIMUM_TIME);

        // Photosensitibity Warning
        FadeIn(_photosensitibityHeaderTextMeshPro);
        FadeIn(_photosensitibityBodyTextMeshPro);
        yield return new WaitForSeconds(MINIMUM_TIME);
        FadeOut(_photosensitibityHeaderTextMeshPro);
        FadeOut(_photosensitibityBodyTextMeshPro);
        yield return new WaitForSeconds(FADE_DURATION + 1);
        _photosensitibityHeaderTextMeshPro.gameObject.SetActive(false);
        _photosensitibityBodyTextMeshPro.gameObject.SetActive(false);

        // Octiware Logo
        FadeIn(_octiwareLogo);
        yield return new WaitForSeconds(MINIMUM_TIME);
        FadeOut(_octiwareLogo);
        yield return new WaitForSeconds(FADE_DURATION + 1);
        _octiwareLogo.gameObject.SetActive(false);

        // FMod Logo
        FadeIn(_fmodLogo);
        yield return new WaitForSeconds(MINIMUM_TIME);
        FadeOut(_fmodLogo);
        yield return new WaitForSeconds(FADE_DURATION + 1);
        _fmodLogo.gameObject.SetActive(false);

        // Background
        FadeOut(_bg);
        yield return new WaitForSeconds(FADE_DURATION + 1);
        _bg.gameObject.SetActive(false);

        Destroy(this);
    }

    private void FadeIn(Image image) {
        image.gameObject.SetActive(true);
        StartCoroutine(FadeImage(image, 0f, 1f));
    }

    private void FadeOut(Image image) {
        StartCoroutine(FadeImage(image, 1f, 0f));
    }

    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha) {
        Color currentColor = image.color;
        float elapsedTime = 0f;

        // Set initial alpha
        currentColor.a = startAlpha;
        image.color = currentColor;

        // Gradually change the alpha
        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / FADE_DURATION);
            currentColor.a = newAlpha;
            image.color = currentColor;
            yield return null;
        }

        // Ensure the final alpha value is set
        currentColor.a = endAlpha;
        image.color = currentColor;
    }

    public void FadeIn(TextMeshProUGUI text) {
        text.gameObject.SetActive(true);
        StartCoroutine(FadeText(text, 0, 1));
    }

    public void FadeOut(TextMeshProUGUI text) {
        StartCoroutine(FadeText(text, 1, 0));
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float startAlpha, float endAlpha) {
        Color color = text.color;
        float time = 0;

        while (time < FADE_DURATION) {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / FADE_DURATION);
            text.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        // Ensure final alpha value is set
        text.color = new Color(color.r, color.g, color.b, endAlpha);
    }
}
