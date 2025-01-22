using System.Collections;
using UnityEngine;

public class TargetDummyEnemy : Enemy {
    float _timeBeforDestroy = 5.0f;
    float _fadeTime = 1.0f;

    public override void Attack(Vector2 enemyPos) { }

    protected override void Die() {
        base.Die();
        Debug.Log("[TargetDummyEnemy] destroyed!");

        var animator = GetComponent<Animator>();
        int stateHash = Animator.StringToHash("Death");
        if (animator.HasState(0, stateHash)) {
            animator.Play(stateHash);
        }

        StartCoroutine(Fade());
    }

    IEnumerator Fade() {
        yield return new WaitForSeconds(_timeBeforDestroy);
        float elapsedTime = 0.0f;
        var spriteRenderer = GetComponent<SpriteRenderer>();
        float startAlpha = spriteRenderer.color.a;
        while (elapsedTime < _fadeTime) {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 0.0f, elapsedTime / _fadeTime);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
            yield return null;
        }
        Destroy(gameObject);
    }
}
