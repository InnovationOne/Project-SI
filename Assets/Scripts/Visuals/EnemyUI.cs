using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour {
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Canvas _canvas;

    [Header("Follow Settings")]
    [SerializeField] private Vector2 _offset = new(0, 1.5f);

    private Enemy _enemy;

    private void Awake() {
        _enemy = GetComponentInParent<Enemy>();
    }

    private void Start() {
        if (_enemy != null) {
            _healthSlider.maxValue = _enemy.EnemySO.Health;
            _healthSlider.value = _enemy.CurrentHealth;
        }
    }

    private void Update() {
        if (_enemy != null) {
            _healthSlider.value = _enemy.CurrentHealth;
        }

        // Position above Enemy
        Vector3 pos = _enemy.transform.position + (Vector3)_offset;
        _canvas.transform.position = Camera.main.WorldToScreenPoint(pos);
    }
}
