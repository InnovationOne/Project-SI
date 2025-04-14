using System.Collections;
using UnityEngine;

public abstract class CreditsStepBase : ScriptableObject {
    internal readonly float _refHeight = 360f;
    internal readonly float _scrollSpeed = 40f;
    internal readonly float _offsetSpawnPosition = 50f;

    [Tooltip("If active, this step can be skipped by pressing the skip button.")]
    [SerializeField] private bool _canSkip = true;

    public bool CanSkip => _canSkip;


    public virtual void OnStepStart(GameObject context) { }
    public virtual void OnStepEnd(GameObject context) { }

    public virtual float GetStepDelay() => 0.5f;
    public abstract IEnumerator RunStep(GameObject context);
}
