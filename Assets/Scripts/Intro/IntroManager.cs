using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroManager : MonoBehaviour {
    [SerializeField] private List<IntroStepBase> _steps = new();
    [SerializeField] private GameObject _contextRoot;

    private bool _skipIntro;

    private void Awake() {
        _skipIntro = PlayerPrefs.GetInt("SkipIntro", 0) == 1;
    }

    private void Start() {
        if (_skipIntro) {
            _contextRoot.SetActive(false);
            Destroy(gameObject);
            return;
        }

        InputManager.Instance.InitIntro();
        StartCoroutine(RunIntroSequence());
    }

    private IEnumerator RunIntroSequence() {
        if (_contextRoot != null) _contextRoot.SetActive(true);

        foreach (var step in _steps) {
            if (step == null) continue;
            yield return StartCoroutine(step.RunStep(_contextRoot));
        }

        InputManager.Instance.CleanupIntro();
        if (_contextRoot != null) Destroy(_contextRoot);
        Destroy(gameObject);
    }
}