using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "IntroStepAnimation", menuName = "Intro/Step/Animation")]
public class IntroStepAnimation : IntroStepBase {
    [Tooltip("Animator with activatable state.")]
    [SerializeField] private Animator _animatorPrefab;

    [Tooltip("Trigger name to start the animation.")]
    [SerializeField] private string _triggerName = "Play";

    [Tooltip("Waiting time in seconds (optional if no exit event).")]
    [SerializeField] private float _duration = 2f;

    public override IEnumerator RunStep(GameObject context) {
        if (_animatorPrefab == null || string.IsNullOrEmpty(_triggerName)) yield break;

        var _animator = Instantiate(_animatorPrefab, context.transform);
        _animator.SetTrigger(_triggerName);

        float timer = 0f;
        while (timer < _duration) {
            if (CanSkip && InputManager.Instance.SkipPressed()) break;
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(_animator.gameObject);
    }
}