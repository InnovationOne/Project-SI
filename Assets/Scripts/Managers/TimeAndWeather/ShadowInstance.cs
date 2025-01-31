using UnityEngine;

[DefaultExecutionOrder(999)]
[ExecuteInEditMode]
public class ShadowInstance : MonoBehaviour {
    [Range(0, 10f)] public float BaseLength = 1f;

    private void OnEnable() {
        GameManager.Instance.TimeManager.RegisterShadow(this);
    }

    private void OnDisable() {
        GameManager.Instance.TimeManager.UnregisterShadow(this);
    }
}
