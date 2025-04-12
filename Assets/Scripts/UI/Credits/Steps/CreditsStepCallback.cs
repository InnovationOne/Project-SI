using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "CreditsStepCallback", menuName = "MainMenu/Credits/Step/Callback")]
public class CreditsStepCallback : CreditsStepBase {
    [SerializeField] private UnityEvent _onStep;
    public override IEnumerator RunStep(GameObject context) {
        _onStep?.Invoke();
        yield return null;
    }
}
