using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private Transform _teleportTarget;
    [SerializeField] private Image _fadePanel;

    const float _fadeDuration = 1f;

    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            StartCoroutine(TeleportPlayerWithFade(other));
        }
    }

    IEnumerator TeleportPlayerWithFade(Collider2D player) {
        player.GetComponent<PlayerMovementController>().SetCanMoveAndTurn(false);
        yield return StartCoroutine(FadeToBlack());
        player.transform.position = _teleportTarget.position;
        yield return StartCoroutine(FadeFromBlack());
        player.GetComponent<PlayerMovementController>().SetCanMoveAndTurn(true);
    }

    IEnumerator FadeToBlack() {
        float timer = 0f;
        Color panelColor = _fadePanel.color;

        while (timer < _fadeDuration) {
            timer += Time.deltaTime;
            panelColor.a = Mathf.Lerp(0, 1, timer / _fadeDuration);
            _fadePanel.color = panelColor;
            yield return null;
        }

        panelColor.a = 1;
        _fadePanel.color = panelColor;
    }

    IEnumerator FadeFromBlack() {
        float timer = 0f;
        Color panelColor = _fadePanel.color;

        while (timer < _fadeDuration) {
            timer += Time.deltaTime;
            panelColor.a = Mathf.Lerp(1, 0, timer / _fadeDuration);
            _fadePanel.color = panelColor;
            yield return null;
        }

        panelColor.a = 0;
        _fadePanel.color = panelColor;
    }
}
