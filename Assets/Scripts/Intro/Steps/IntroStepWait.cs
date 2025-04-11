using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "IntroStepWait", menuName = "Intro/Step/Wait")]
public class IntroStepWait : IntroStepBase {
    [SerializeField] private float _duration = 1f;

    public override IEnumerator RunStep(GameObject context) {
        yield return new WaitForSeconds(_duration);
    }
}
