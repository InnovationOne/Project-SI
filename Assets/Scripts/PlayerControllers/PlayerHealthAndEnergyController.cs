using System;
using Unity.Netcode;
using UnityEngine;

// Manages player's health and energy, supporting network synchronization and persistence
[RequireComponent(typeof(NetworkObject))]
public class PlayerHealthAndEnergyController : NetworkBehaviour, IPlayerDataPersistance {
    // Events to notify listeners about health and energy updates
    public event Action<float> OnUpdateHealth;
    public event Action<float> OnUpdateMaxHealth;
    public event Action<float> OnUpdateEnergy;
    public event Action<float> OnUpdateMaxEnergy;

    [Header("Health Parameters")]
    [SerializeField] float _maxHealth = 100;
    public float MaxHealth => _maxHealth;

    [SerializeField] float _currentHealth = 100;
    public float CurrentHealth => _currentHealth;

    [SerializeField] float _hpAtRespawn = 10;
    [SerializeField] float _regenHpAmountInBed = 0.5f;

    [Header("Energy Parameters")]
    [SerializeField] float _maxEnergy = 100;
    public float MaxEnergy => _maxEnergy;

    [SerializeField] float _currentEnergy = 100;
    public float CurrentEnergy => _currentEnergy;

    [SerializeField] float _energyAtRespawn = 10;

    [SerializeField, Tooltip("If current energy on day end is above this fraction, next day energy is full.")]
    float _minimumEnergyMultiplierForFullReset = 0.25f;

    [SerializeField, Tooltip("If energy is lower than the minimum fraction, next day energy is set to 50% of max.")]
    float _energyMultiplierForNonFullReset = 0.5f;

    [SerializeField] float _regenEnergyAmountInBed = 0.5f;

    // TODO Move _hospitalRespawnPosition to the hospital itself
    Vector2 _hospitalRespawnPosition;
    PlayerController _playerController;
    PlayerInventoryController _inventoryController;
    TimeManager _timeManager;

    void Awake() {
        _playerController = GetComponent<PlayerController>();
        _inventoryController = GetComponent<PlayerInventoryController>();
    }

    void Start() {
        _timeManager = TimeManager.Instance;
        _timeManager.OnNextDayStarted += HandleNextDayStarted;

        OnUpdateHealth?.Invoke(_currentHealth);
        OnUpdateMaxHealth?.Invoke(_maxHealth);
        OnUpdateEnergy?.Invoke(_currentEnergy);
        OnUpdateMaxEnergy?.Invoke(_maxEnergy);
    }

    new void OnDestroy() {
        _timeManager.OnNextDayStarted -= HandleNextDayStarted;
        base.OnDestroy();
    }

    void FixedUpdate() {
        if (IsOwner && _playerController.InBed) {
            RegenerateHealthAndEnergy();
        }
    }

    void RegenerateHealthAndEnergy() {
        float dt = Time.deltaTime;
        AdjustHealth(_regenHpAmountInBed * dt);
        AdjustEnergy(_regenEnergyAmountInBed * dt);
    }

    void HandleNextDayStarted() {
        AdjustHealth(_maxHealth - _currentHealth);

        float targetEnergy = _currentEnergy >= _maxEnergy * _minimumEnergyMultiplierForFullReset
            ? _maxEnergy
            : _maxEnergy * _energyMultiplierForNonFullReset;

        AdjustEnergy(targetEnergy - _currentEnergy);

        // Invoke events to update UI or other listeners
        OnUpdateHealth?.Invoke(_currentHealth);
        OnUpdateEnergy?.Invoke(_currentEnergy);
    }

    #region Health
    /// <summary>
    /// Adjusts the player's health by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to adjust the health by.</param>
    public void AdjustHealth(float amount) {
        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);

        // Invoke event only if health has changed
        if (!Mathf.Approximately(previousHealth, _currentHealth)) {
            OnUpdateHealth?.Invoke(_currentHealth);
        }

        if (_currentHealth <= 0f) {
            HandlePlayerDeath();
        }
    }

    /// <summary>
    /// Adjusts the maximum health of the player.
    /// </summary>
    /// <param name="newMaxHealth">The new maximum health value.</param>
    public void AdjustMaxHealth(float newMaxHealth) {
        if (newMaxHealth <= 0f) {
            Debug.LogWarning("Max health must be greater than zero.");
            return;
        }

        _maxHealth = newMaxHealth;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        OnUpdateMaxHealth?.Invoke(_maxHealth);
        OnUpdateHealth?.Invoke(_currentHealth);
    }

    /// <summary>
    /// Handles the player's death.
    /// </summary>
    private void HandlePlayerDeath() {
        // TODO: Play death animation

        RespawnPlayer();

        // Clear inventory on death
        if (_inventoryController != null) {
            _inventoryController.InventoryContainer.ClearItemContainer();
        }

        // TODO: Play wake-up animation and in-hospital event
    }

    #endregion

    #region Energy

    /// <summary>
    /// Adjusts the energy of the player by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to adjust the energy by.</param>
    public void AdjustEnergy(float amount) {
        float previousEnergy = _currentEnergy;
        _currentEnergy = Mathf.Clamp(_currentEnergy + amount, 0f, _maxEnergy);

        // Invoke event only if energy has changed
        if (!Mathf.Approximately(previousEnergy, _currentEnergy)) {
            OnUpdateEnergy?.Invoke(_currentEnergy);
        }

        if (_currentEnergy <= 0f) {
            HandlePlayerExhaustion();
        }
    }

    /// <summary>
    /// Adjusts the maximum energy of the player.
    /// </summary>
    /// <param name="newMaxEnergy">The new maximum energy value.</param>
    public void AdjustMaxEnergy(float newMaxEnergy) {
        if (newMaxEnergy <= 0f) {
            Debug.LogWarning("Max energy must be greater than zero.");
            return;
        }

        _maxEnergy = newMaxEnergy;
        _currentEnergy = Mathf.Clamp(_currentEnergy, 0f, _maxEnergy);
        OnUpdateMaxEnergy?.Invoke(_maxEnergy);
        OnUpdateEnergy?.Invoke(_currentEnergy);
    }

    /// <summary>
    /// Handles the exhaustion of the player.
    /// </summary>
    private void HandlePlayerExhaustion() {
        // TODO: Play exhausted animation

        // Respawn next day?

        // TODO: Play wake-up animation and in-hospital event for exhaustion

        // Additional exhaustion logic
    }
    #endregion

    /// <summary>
    /// Respawns the player at the hospital respawn position and restores their health to the value at respawn.
    /// </summary>
    void RespawnPlayer() {
        if (_hospitalRespawnPosition == Vector2.zero) {
            Debug.LogWarning("Hospital respawn position is not set.");
            return;
        }

        transform.position = _hospitalRespawnPosition;
        _currentHealth = _hpAtRespawn;
        _currentEnergy = _energyAtRespawn;

        // Invoke events to update UI
        OnUpdateHealth?.Invoke(_currentHealth);
        OnUpdateEnergy?.Invoke(_currentEnergy);
    }


    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.MaxHp = _maxHealth;
        playerData.CurrentHp = _currentHealth;
        playerData.MaxEnergy = _maxEnergy;
        playerData.CurrentEnergy = _currentEnergy;
    }

    public void LoadPlayer(PlayerData playerData) {
        _maxHealth = playerData.MaxHp;
        _currentHealth = Mathf.Clamp(playerData.CurrentHp, 0f, _maxHealth);

        _maxEnergy = playerData.MaxEnergy;
        _currentEnergy = Mathf.Clamp(playerData.CurrentEnergy, 0f, _maxEnergy);

        // Invoke events to update UI or other listeners
        OnUpdateMaxHealth?.Invoke(_maxHealth);
        OnUpdateHealth?.Invoke(_currentHealth);
        OnUpdateMaxEnergy?.Invoke(_maxEnergy);
        OnUpdateEnergy?.Invoke(_currentEnergy);
    }
    #endregion
}
