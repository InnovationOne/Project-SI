using System.Collections;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "IntroStepText", menuName = "MainMenu/Intro/Step/Text")]
public class IntroStepText : IntroStepBase {
    [Tooltip("The header text prefab (TMP_Text).")]
    [SerializeField] private TextMeshProUGUI _headerPrefab;

    [Tooltip("The body text prefab (TMP_Text).")]
    [SerializeField] private TextMeshProUGUI _bodyPrefab;

    [TextArea]
    [Tooltip("The text displayed in the header.")]
    [SerializeField] private string _headerText;

    [TextArea]
    [Tooltip("The text displayed in the body.")]
    [SerializeField] private string _bodyText;

    [Tooltip("Time to show the texts before fading out.")]
    [SerializeField] private float _displayTime = 4f;

    public override IEnumerator RunStep(GameObject context) {
        var header = Instantiate(_headerPrefab, context.transform);
        var body = Instantiate(_bodyPrefab, context.transform);

        header.text = _headerText;
        body.text = _bodyText;

        header.canvasRenderer.SetAlpha(0f);
        body.canvasRenderer.SetAlpha(0f);

        header.gameObject.SetActive(true);
        body.gameObject.SetActive(true);

        header.CrossFadeAlpha(1f, 1f, false);
        body.CrossFadeAlpha(1f, 1f, false);

        float timer = 0f;
        while (timer < _displayTime) {
            if (CanSkip && InputManager.Instance.IntroSkipPressed()) break;
            timer += Time.deltaTime;
            yield return null;
        }

        header.CrossFadeAlpha(0f, 1f, false);
        body.CrossFadeAlpha(0f, 1f, false);
        yield return new WaitForSeconds(1f);

        Destroy(header.gameObject);
        Destroy(body.gameObject);
    }
}