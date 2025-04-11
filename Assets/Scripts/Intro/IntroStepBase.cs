using System.Collections;
using UnityEngine;

public abstract class IntroStepBase : ScriptableObject {
    [Tooltip("Falls aktiv, kann dieser Step durch Mausklick übersprungen werden.")]
    [SerializeField] private bool _canSkip = true;

    public bool CanSkip => _canSkip;

    public virtual void OnStepStart(GameObject context) { }
    public virtual void OnStepEnd(GameObject context) { }

    public abstract IEnumerator RunStep(GameObject context);
}
