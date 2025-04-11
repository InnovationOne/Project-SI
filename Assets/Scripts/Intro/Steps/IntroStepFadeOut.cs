using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "IntroStepFadeOut", menuName = "Intro/Step/FadeOut")]
public class IntroStepFadeOut : IntroStepBase {
    [Tooltip("UI element that is to be hidden.")]
    [SerializeField] private Image _imagePrefab;

    private Image _image;

    public override IEnumerator RunStep(GameObject context) {
        _image = Instantiate(_imagePrefab, context.transform);
        yield return null;
    }

    /// <summary>
    /// Call this from a UnityEvent (e.g. IntroStepCallback) to start the fade out.
    /// </summary>
    public void TriggerFade() {
        if (_image == null) return;
        _image.StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy() {
        _image.CrossFadeAlpha(0f, 1f, false);
        yield return new WaitForSeconds(1f);
        Destroy(_image.gameObject);
    }
}