using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CreditsStepFadeOut", menuName = "MainMenu/Credits/Step/FadeOut")]
public class CreditsStepFadeOut : CreditsStepBase {
    [SerializeField] private Image _imagePrefab;
    private Image _image;

    public override IEnumerator RunStep(GameObject context) {
        _image = Instantiate(_imagePrefab, context.transform);
        _image.canvasRenderer.SetAlpha(0f);
        _image.CrossFadeAlpha(1f, 1f, false);
        yield return new WaitForSeconds(1f);
    }

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
