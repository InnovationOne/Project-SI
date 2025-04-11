using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "IntroStepFadeOut", menuName = "Intro/Step/FadeOut")]
public class IntroStepFadeOut : IntroStepBase {
    [Tooltip("UI element that is to be hidden.")]
    [SerializeField] private Image _image;

    public override IEnumerator RunStep(GameObject context) {
        if (_image == null) yield break;

        _image.CrossFadeAlpha(0f, 1f, false);
        yield return new WaitForSeconds(1f);
        Destroy(_image.gameObject);
    }
}