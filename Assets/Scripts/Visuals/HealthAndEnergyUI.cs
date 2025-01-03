using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkObject))]
public class HealthAndEnergyUI : NetworkBehaviour {
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

    private PlayerHealthAndEnergyController _playerHealthAndEnergyController;


    /// <summary>
    /// Subscribes to the player's health and energy events.
    /// </summary>
    public override void OnNetworkSpawn() {
        _playerHealthAndEnergyController = PlayerController.LocalInstance.PlayerHealthAndEnergyController;
        _playerHealthAndEnergyController.OnUpdateHealth += ChangeHp;
        _playerHealthAndEnergyController.OnUpdateMaxHealth += ChangeMaxHp;
        _playerHealthAndEnergyController.OnUpdateEnergy += ChangeEnergy;
        _playerHealthAndEnergyController.OnUpdateMaxEnergy += ChangeMaxEnergy;
    }

    /// <summary>
    /// This method is called when the GameObject is being destroyed.
    /// It unsubscribes the event handlers from the player controller's events.
    /// </summary>
    private new void OnDestroy() {
        _playerHealthAndEnergyController.OnUpdateHealth -= ChangeHp; // Beim beenden ne null ref exeption. Sollte aber nicht stören. 
        _playerHealthAndEnergyController.OnUpdateMaxHealth -= ChangeMaxHp;
        _playerHealthAndEnergyController.OnUpdateEnergy -= ChangeEnergy;
        _playerHealthAndEnergyController.OnUpdateMaxEnergy -= ChangeMaxEnergy;
    }

    /// <summary>
    /// Updates the health and energy sliders every frame.
    /// </summary>
    private void Update() {
        UpdateSlider(_healthSlider, _healthSliderShrink, ref _damageHealthShrinkTimer);
        UpdateSlider(_energySlider, _energySliderShrink, ref _energyUsedShrinkTimer);
    }

    /// <summary>
    /// Updates the given mainSlider and shrinkSlider based on the timer and shrink speed.
    /// </summary>
    /// <param name="mainSlider">The main slider to update.</param>
    /// <param name="shrinkSlider">The shrink slider to update.</param>
    /// <param name="timer">A reference to the timer value.</param>
    private void UpdateSlider(Slider mainSlider, Slider shrinkSlider, ref float timer) {
        if (timer > 0) {
            timer -= Time.deltaTime;
        }
        if (mainSlider.value != shrinkSlider.value && timer <= 0) {
            shrinkSlider.value -= _shrinkSpeed * Time.deltaTime;
            if (shrinkSlider.value < mainSlider.value) {
                shrinkSlider.value = mainSlider.value;
            }
        }
    }

    /// <summary>
    /// Changes the current health value and updates the health UI elements accordingly.
    /// </summary>
    /// <param name="hp">The new health value.</param>
    private void ChangeHp(float hp) {
        _damageHealthShrinkTimer = hp < _healthSlider.value ? DAMAGED_HEALTH_SHRINK_TIMER_MAX : 0;
        _healthSlider.value = hp;
        _healthText.text = hp.ToString();
        if (hp >= _healthSliderShrink.value) {
            _healthSliderShrink.value = hp;
        }
    }

    /// <summary>
    /// Changes the maximum HP value for the health slider.
    /// </summary>
    /// <param name="maxHp">The new maximum HP value.</param>
    private void ChangeMaxHp(float maxHp) {
        _healthSlider.maxValue = maxHp;
        _healthSliderShrink.maxValue = maxHp;
    }

    /// <summary>
    /// Changes the energy value and updates the energy UI elements.
    /// </summary>
    /// <param name="energy">The new energy value.</param>
    private void ChangeEnergy(float energy) {
        _energyUsedShrinkTimer = energy < _energySlider.value ? ENERGY_USED_SHRINK_TIMER_MAX : 0;
        _energySlider.value = energy;
        _energyText.text = energy.ToString();
        if (energy >= _energySliderShrink.value) {
            _energySliderShrink.value = energy;
        }
    }

    /// <summary>
    /// Changes the maximum energy value for the energy slider.
    /// </summary>
    /// <param name="maxEnergy">The new maximum energy value.</param>
    private void ChangeMaxEnergy(float maxEnergy) {
        _energySlider.maxValue = maxEnergy;
        _energySliderShrink.maxValue = maxEnergy;
    }

    /// <summary>
    /// Toggles the visibility of the UI element.
    /// </summary>
    public void ToggleUI() => gameObject.SetActive(!gameObject.activeSelf);
}
