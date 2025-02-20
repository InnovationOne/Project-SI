using System.Collections;
using TMPro;
using UnityEngine;

public class FishCatchUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _catchText;

    private const float CATCH_TEXT_SHOW_TIME = 5f;
    private const float UI_FADE_DURATION = 0.2f;
    private Coroutine _displayRoutine;

    void Start() {
        gameObject.SetActive(false);
    }

    public void ShowFishCatchUI(string text) {
        // Stop any current routine so messages don't overlap.
        if (_displayRoutine != null) {
            StopCoroutine(_displayRoutine);
        }
        // Activate the UI and set the new message.
        gameObject.SetActive(true);
        _catchText.text = text;
        _catchText.canvasRenderer.SetAlpha(0f);

        // Start the fade in/out routine.
        _displayRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine() {
        // Fade in the text.
        _catchText.CrossFadeAlpha(1f, UI_FADE_DURATION, false);
        yield return new WaitForSeconds(CATCH_TEXT_SHOW_TIME);

        // Fade out the text.
        _catchText.CrossFadeAlpha(0f, UI_FADE_DURATION, false);
        yield return new WaitForSeconds(UI_FADE_DURATION);

        // Deactivate the UI.
        gameObject.SetActive(false);
        _displayRoutine = null;
    }
}
