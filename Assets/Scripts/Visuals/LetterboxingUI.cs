using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LetterboxingUI : MonoBehaviour {
    [SerializeField] Image _topLetterbox;
    [SerializeField] Image _bottomLetterbox;

    // Duration for the slide animation; adjust to get the desired “movie‐like” speed.
    [SerializeField] float animationDuration = 0.5f;

    void Start() {
        // Start with the letterboxes hidden (inactive)
        _topLetterbox.gameObject.SetActive(false);
        _bottomLetterbox.gameObject.SetActive(false);
    }

    public void ShowLetterboxes() {
        // Activate the letterbox GameObjects
        _topLetterbox.gameObject.SetActive(true);
        _bottomLetterbox.gameObject.SetActive(true);

        // Get the RectTransforms
        RectTransform topRect = _topLetterbox.rectTransform;
        RectTransform bottomRect = _bottomLetterbox.rectTransform;

        // Set starting positions offscreen:
        // For the top letterbox, anchored at (0,0) is the final position.
        // Starting offscreen above means a Y offset of +46.
        topRect.anchoredPosition = new Vector2(0, 46);
        // For the bottom letterbox, start offscreen below (Y = -46)
        bottomRect.anchoredPosition = new Vector2(0, -46);

        // Animate both to their onscreen positions (0,0)
        StartCoroutine(AnimateRectTransform(topRect, topRect.anchoredPosition, Vector2.zero, animationDuration));
        StartCoroutine(AnimateRectTransform(bottomRect, bottomRect.anchoredPosition, Vector2.zero, animationDuration));
    }

    public void HideLetterboxes() {
        // Get the RectTransforms
        RectTransform topRect = _topLetterbox.rectTransform;
        RectTransform bottomRect = _bottomLetterbox.rectTransform;

        // Animate them back offscreen:
        // Top letterbox moves back to (0,46) (above)
        StartCoroutine(AnimateRectTransform(topRect, topRect.anchoredPosition, new Vector2(0, 46), animationDuration, () => {
            _topLetterbox.gameObject.SetActive(false);
        }));
        // Bottom letterbox moves to (0,-46) (below)
        StartCoroutine(AnimateRectTransform(bottomRect, bottomRect.anchoredPosition, new Vector2(0, -46), animationDuration, () => {
            _bottomLetterbox.gameObject.SetActive(false);
        }));
    }

    // Coroutine to animate the RectTransform's anchoredPosition
    private IEnumerator AnimateRectTransform(RectTransform rect, Vector2 startPos, Vector2 endPos, float duration, System.Action onComplete = null) {
        float elapsed = 0f;
        while (elapsed < duration) {
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the final position is set
        rect.anchoredPosition = endPos;
        onComplete?.Invoke();
    }
}
