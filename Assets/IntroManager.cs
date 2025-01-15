using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour {
    [SerializeField] private bool _skipIntro;
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

    private const float STARTUP_TIME = 1f;
    private const float MINIMUM_TIME = 4f;
    private const float FADE_DURATION = 1f;

    private void Awake() {
        if (_skipIntro) {
            _background.gameObject.SetActive(false);
            this.enabled = false;
            return;
        }

        // Initialize game objects
        _background.gameObject.SetActive(true);
        _alphaHeaderTextMeshPro.gameObject.SetActive(false);
        _alphaBodyTextMeshPro.gameObject.SetActive(false);
        _photosensitivityHeaderTextMeshPro.gameObject.SetActive(false);
        _photosensitivityBodyTextMeshPro.gameObject.SetActive(false);
        _octiwareLogo.gameObject.SetActive(false);
        _fmodLogo.gameObject.SetActive(false);
    }


    private void Start() {
        GameManager.Instance.AudioManager.InitializeMusic(GameManager.Instance.FMODEvents.TitleTheme);

        _alphaHeaderTextMeshPro.text = _alphaHeaderText;
        _alphaBodyTextMeshPro.text = _alphaBodyText;
        _photosensitivityHeaderTextMeshPro.text = _photosensitivityHeaderText;
        _photosensitivityBodyTextMeshPro.text = _photosensitivityBodyText;
        StartCoroutine(FadeSequence());
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

        this.enabled = false;
    }

    private IEnumerator ShowElement(params Graphic[] graphics) {
        float remainingTime = MINIMUM_TIME;

        // Activate graphics and set initial alpha to 0
        foreach (var graphic in graphics) {
            graphic.gameObject.SetActive(true);
            graphic.canvasRenderer.SetAlpha(0f);
            graphic.CrossFadeAlpha(1f, FADE_DURATION, false);
        }

        // Wait for fade-in to complete
        yield return new WaitForSeconds(FADE_DURATION);

        // Wait for minimum time or until user clicks
        while (remainingTime > 0) {
            if (Input.GetMouseButtonDown(0)) {
                break;
            }
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        // Fade out
        foreach (var graphic in graphics) {
            graphic.CrossFadeAlpha(0f, FADE_DURATION, false);
        }

        // Wait for fade-out to complete
        yield return new WaitForSeconds(FADE_DURATION);

        // Deactivate graphics
        foreach (var graphic in graphics) {
            graphic.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideElement(Graphic graphic) {
        // Fade out
        graphic.CrossFadeAlpha(0f, FADE_DURATION, false);

        // Wait for fade-out to complete
        yield return new WaitForSeconds(FADE_DURATION);

        // Deactivate graphics
        graphic.gameObject.SetActive(false);
    }
}