using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthAndEnergyPanel : MonoBehaviour {
    public static HealthAndEnergyPanel Instance { get; private set; }

    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _healthSliderShrink;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private Slider _energySlider;
    [SerializeField] private Slider _energySliderShrink;
    [SerializeField] private TextMeshProUGUI _energyText;

    private const float DAMAGED_HEALTH_SHRINK_TIMER_MAX = 1f;
    private const float ENERGY_USED_SHRINK_TIMER_MAX = 1f;

    private float _damageHealthShrinkTimer;
    private float _energyUsedShrinkTimer;
    private float _shrinkSpeed = 90f;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of HealthAndEnergyPanel in the scene!");
            return;
        }
        Instance = this;
    }

    private void Update() {
        _damageHealthShrinkTimer -= Time.deltaTime;
        if (_damageHealthShrinkTimer < 0 && _healthSlider.value != _healthSliderShrink.value) {
            _healthSliderShrink.value -= _shrinkSpeed * Time.deltaTime;
        }

        _energyUsedShrinkTimer -= Time.deltaTime;
        if (_energyUsedShrinkTimer < 0 && _energySlider.value != _energySliderShrink.value) {
            _energySliderShrink.value -= _shrinkSpeed * Time.deltaTime;
        }
    }

    public void ChangeHp(float hp) {
        if (hp < _healthSlider.value) {
            // Damage
            _damageHealthShrinkTimer = DAMAGED_HEALTH_SHRINK_TIMER_MAX;
        } else {
            // Heal
            _healthSliderShrink.value = hp;
        }

        _healthSlider.value = hp;
        _healthText.text = hp.ToString();
    }

    public void ChangeMaxHp(int maxHP) {
        _healthSlider.maxValue = maxHP;
    }

    public void ChangeEnergy(float energy) {
        if (energy < _energySlider.value) {
            // Exhaust
            _energyUsedShrinkTimer = ENERGY_USED_SHRINK_TIMER_MAX;
        } else {
            // Rest
            _energySliderShrink.value = energy;
        }

        _energySlider.value = energy;
        _energyText.text = energy.ToString();
    }

    public void ChangeMaxEnergy(int maxEnergy) {
        _energySlider.maxValue = maxEnergy;
    }

    public void TogglePanel() {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
