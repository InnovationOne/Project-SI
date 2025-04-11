using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "IntroStepImage", menuName = "Intro/Step/Image")]
public class IntroStepImage : IntroStepBase {
    [Tooltip("Image prefab to instantiate.")]
    [SerializeField] private Image _imagePrefab;

    [Tooltip("Time to show the image.")]
    [SerializeField] private float _displayTime = 4f;

    public override IEnumerator RunStep(GameObject context) {
        var image = Instantiate(_imagePrefab, context.transform);

        image.canvasRenderer.SetAlpha(0f);
        image.gameObject.SetActive(true);
        image.CrossFadeAlpha(1f, 1f, false);

        float timer = 0f;
        while (timer < _displayTime) {
            if (CanSkip && InputManager.Instance.SkipPressed()) break;
            timer += Time.deltaTime;
            yield return null;
        }

        image.CrossFadeAlpha(0f, 1f, false);
        yield return new WaitForSeconds(1f);

        Destroy(image.gameObject);
    }
}