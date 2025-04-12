using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "IntroStepDualImage", menuName = "MainMenu/Intro/Step/DualImage")]
public class IntroStepDualImage : IntroStepBase {
    [Tooltip("Left Image")]
    [SerializeField] private Image _leftImagePrefab;

    [Tooltip("Right Image")]
    [SerializeField] private Image _rightImagePrefab;

    [Tooltip("Duration of the display in seconds.")]
    [SerializeField] private float _displayTime = 4f;

    public override IEnumerator RunStep(GameObject context) {
        var _leftImage = Instantiate(_leftImagePrefab, context.transform);
        var _rightImage = Instantiate(_rightImagePrefab, context.transform);

        _leftImage.canvasRenderer.SetAlpha(0f);
        _rightImage.canvasRenderer.SetAlpha(0f);
        _leftImage.gameObject.SetActive(true);
        _rightImage.gameObject.SetActive(true);
        _leftImage.CrossFadeAlpha(1f, 1f, false);
        _rightImage.CrossFadeAlpha(1f, 1f, false);

        float timer = 0f;
        while (timer < _displayTime) {
            if (CanSkip && InputManager.Instance.IntroSkipPressed()) break;
            timer += Time.deltaTime;
            yield return null;
        }

        _leftImage.CrossFadeAlpha(0f, 1f, false);
        _rightImage.CrossFadeAlpha(0f, 1f, false);
        yield return new WaitForSeconds(1f);

        Destroy(_leftImage.gameObject);
        Destroy(_rightImage.gameObject);
    }
}