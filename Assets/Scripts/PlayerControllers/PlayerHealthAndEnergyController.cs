using System;
using Unity.Netcode;
using UnityEngine;

// This script handels hp and energy of the player
public class PlayerHealthAndEnergyController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerHealthAndEnergyController LocalInstance { get; private set; }

    public event Action<float> OnUpdateHealth;
    public event Action<float> OnUpdateMaxHealth;
    public event Action<float> OnUpdateEnergy;
    public event Action<float> OnUpdateMaxEnergy;

    [Header("Health Parameters")]
    [SerializeField] private float _maxHealth = 100;
    [SerializeField] private float _currentHealth = 100;
    [SerializeField] private float _hpAtRespawn = 10;
    [SerializeField] private float _regenHpAmountInBed = 0.5f;

    [Header("Energy Parameters")]
    [SerializeField] private float _maxEnergy = 100;
    [SerializeField] private float _currentEnergy = 100;
    [SerializeField] private float _energyAtRespawn = 10;
    [SerializeField] private float _minimumEnergyMultiplierForFullReset = 0.25f; // Energy is set to max, when rest energy is above 25%
    [SerializeField] private float _energyMultiplierForNoneFullReset = 0.5f; // Energy is set to 50% of max, when rest energy is below 25%
    [SerializeField] private float _regenEnergyAmountInBed = 0.5f;

    // TODO Move _hospitalRespawnPosition to the hospital itself
    private Vector2 _hospitalRespawnPosition;
    private Player _localPlayer;

    private void Awake() {
        _localPlayer = GetComponent<Player>();
    }


    private void Start() {
        TimeAndWeatherManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;

        OnUpdateHealth?.Invoke(_currentHealth);
        OnUpdateMaxHealth?.Invoke(_maxHealth);
        OnUpdateEnergy?.Invoke(_currentEnergy);
        OnUpdateMaxEnergy?.Invoke(_maxEnergy);
    }

    private new void OnDestroy() {
        TimeAndWeatherManager.Instance.OnNextDayStarted -= TimeAndWeatherManager_OnNextDayStarted;
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerHealthAndEnergyController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    private void FixedUpdate() {
        // While in bed regen hp and energy.
        if (_localPlayer.InBed) {
            _currentHealth += _regenHpAmountInBed;
            _currentEnergy += _regenEnergyAmountInBed;
        }
    }

    private void TimeAndWeatherManager_OnNextDayStarted() {
        AdjustHealth(_maxHealth);
        AdjustEnergy(_currentEnergy >= _maxEnergy * _minimumEnergyMultiplierForFullReset ? _maxEnergy : _maxEnergy * _energyMultiplierForNoneFullReset);
        OnUpdateHealth?.Invoke(_currentHealth);
        OnUpdateEnergy?.Invoke(_maxEnergy);
    }


    #region Health
    /// <summary>
    /// Adjusts the player's health by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to adjust the health by.</param>
    public void AdjustHealth(float amount) {
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0, _maxHealth);
        OnUpdateHealth?.Invoke(_currentHealth);

        if (_currentHealth <= 0) {
            HandlePlayerDeath();
        }
    }
    
    /// <summary>
    /// Adjusts the maximum health of the player.
    /// </summary>
    /// <param name="newMaxHealth">The new maximum health value.</param>
    public void AdjustMaxHealth(int newMaxHealth) {
        _maxHealth = newMaxHealth;
        OnUpdateMaxHealth?.Invoke(_maxHealth);
    }

    /// <summary>
    /// Handles the player's death.
    /// </summary>
    private void HandlePlayerDeath() {
        // TODO: Play death animation

        RespawnPlayer();

        // Clear inventory on death
        //GetComponent<PlayerInventoryController>().InventoryContainer.ClearItemSlots();

        // TODO: Play wake-up animation and in-hospital event for death

        // Additional death logic
    }

    #endregion


    #region Energy
    /// <summary>
    /// Adjusts the energy of the player by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to adjust the energy by.</param>
    public void AdjustEnergy(float amount) {
        _currentEnergy = Mathf.Clamp(_currentEnergy + amount, 0, _maxEnergy);
        OnUpdateEnergy?.Invoke(_currentEnergy);

        if (_currentEnergy <= 0) {
            HandlePlayerExhaustion();
        }
    }

    /// <summary>
    /// Changes the maximum energy value for the player.
    /// </summary>
    /// <param name="newMaxEnergy">The new maximum energy value.</param>
    public void ChangeMaxEnergy(int newMaxEnergy) {
        _maxEnergy = newMaxEnergy;
        OnUpdateMaxEnergy?.Invoke(_maxEnergy);
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
    private void RespawnPlayer() {
        transform.position = _hospitalRespawnPosition;
        _currentHealth = _hpAtRespawn;
        _currentEnergy = _energyAtRespawn;
    }


    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.MaxHp = _maxHealth;
        playerData.CurrentHp = _currentHealth;
        playerData.MaxEnergy = _maxEnergy;
        playerData.CurrentEnergy = _currentEnergy;
        playerData.HosptitalRespawnPosition = _hospitalRespawnPosition;
    }

    public void LoadPlayer(PlayerData playerData) {
        _maxHealth = playerData.MaxHp;
        _currentHealth = playerData.CurrentHp;
        _maxEnergy = playerData.MaxEnergy;
        _currentEnergy = playerData.CurrentEnergy;
        _hospitalRespawnPosition = playerData.HosptitalRespawnPosition;
    }
    #endregion
}
