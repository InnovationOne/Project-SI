using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsManager : MonoBehaviour {
    [SerializeField] private List<CreditsStepBase> _steps = new();
    [SerializeField] private GameObject _contextRoot;

    private bool _skipCredits = false;

    private void Awake() {
        InputManager.Instance.InitCredits();
    }

    private void OnEnable() {
        StartCredits();
    }

    public void StartCredits() {
        StopAllCoroutines();
        DeleteChildren();
        _skipCredits = false;
        if (_contextRoot != null) _contextRoot.SetActive(true);
        StartCoroutine(RunCreditsSequence());
    }


    private IEnumerator RunCreditsSequence() {
        foreach (var step in _steps) {
            if (_skipCredits || step == null) continue;

            yield return StartCoroutine(step.RunStep(_contextRoot));

            float delay = step.GetStepDelay();
            float speed = InputManager.Instance.CreditsFastForwardPressed() ? 3f : 1f;
            yield return new WaitForSeconds(delay / speed);
        }

        InputManager.Instance.CleanupCredits();
        DeleteChildren();
        if (_contextRoot != null) _contextRoot.SetActive(false);
    }

    private void Update() {
        if (InputManager.Instance.CreditsSkipPressed()) {
            _skipCredits = true;
            DeleteChildren();
            if (_contextRoot != null) _contextRoot.SetActive(false);
        }
    }

    private void DeleteChildren() {
        if (_contextRoot == null) return;

        foreach (Transform child in _contextRoot.transform) {
            Destroy(child.gameObject);
        }
    }
}
