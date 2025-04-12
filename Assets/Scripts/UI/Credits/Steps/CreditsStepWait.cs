using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CreditsStepWait", menuName = "MainMenu/Credits/Step/Wait")]
public class CreditsStepWait : CreditsStepBase {
    [SerializeField] private float _duration = 1f;
    public override IEnumerator RunStep(GameObject context) {
        yield return new WaitForSeconds(_duration);
    }
}
