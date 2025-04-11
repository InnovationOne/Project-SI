using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "IntroStepCallback", menuName = "Intro/Step/Callback")]
public class IntroStepCallback : IntroStepBase {
    [SerializeField] private UnityEvent _onStep;

    public override IEnumerator RunStep(GameObject context) {
        _onStep?.Invoke();
        yield return null;
    }
}