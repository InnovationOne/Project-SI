using Unity.Netcode;
using UnityEngine;

// This script handels hp and energy of the player
public class PlayerHealthAndEnergyController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerHealthAndEnergyController LocalInstance { get; private set; }

    [Header("Debug: Hp-Params")]
    [SerializeField] private int _maxHp = 100;
    [SerializeField] private float _currentHp = 100;
    [SerializeField] private int _hpAtRespawn = 10;
    [SerializeField] private float _regenHpAmountInBed = 0.5f;

    [Header("Debug: Energy-Params")]
    [SerializeField] private int _maxEnergy = 100;
    [SerializeField] private float _currentEnergy = 100;
    [SerializeField] private int _energyAtRespawn = 10;
    [SerializeField] private float _minimumEnergyMultiplierForFullReset = 0.25f; // Energy is set to max, when rest energy is above 25%
    [SerializeField] private float _energyMultiplierForNoneFullReset = 0.5f; // Energy is set to 50% of max, when rest energy is below 25%
    [SerializeField] private float _regenEnergyAmountInBed = 0.5f;

    // TODO Move _hospitalRespawnPosition to the hospital itself
    private Vector2 _hospitalRespawnPosition;
    private HealthAndEnergyPanel _healthAndEnergyPanel;
    private TimeAndWeatherManager _timeAndWeatherManager;
    private Player _localPlayer;

    private void Awake() {
        _localPlayer = GetComponent<Player>();
    }


    private void Start() {
        // Get references
        _healthAndEnergyPanel = HealthAndEnergyPanel.Instance;
        _timeAndWeatherManager = TimeAndWeatherManager.Instance;
        
        // Subscribe to event
        _timeAndWeatherManager.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;

        // Update UI
        UpdateHealthUI();
        UpdateMaxHealthUI();
        UpdateEnergyUI();
        UpdateMaxEnergyUI();
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
            Debug.Log($"Player is in bed. Restored {_regenHpAmountInBed} hp and {_regenEnergyAmountInBed} energy.");
            _currentHp += _regenHpAmountInBed;
            _currentEnergy += _regenEnergyAmountInBed;
        }
    }

    private void TimeAndWeatherManager_OnNextDayStarted() {
        Debug.Log("Restored max hp for next day.");
        RestoreHp(_maxHp);

        var energyToRestore = _currentEnergy >= _maxEnergy * _minimumEnergyMultiplierForFullReset ? _maxEnergy : _maxEnergy * _energyMultiplierForNoneFullReset;
        Debug.Log($"Restored {energyToRestore} for next day.");
        RestoreEnergy(energyToRestore);
    }


    #region Health
    public void RemoveHp(float amount) {
        // Log the action
        Debug.Log("Hp removed.");

        // Deduct health
        _currentHp -= amount;

        // Update UI
        UpdateHealthUI();

        // Check if the player is dead
        if (_currentHp <= 0) {
            Debug.Log("Player is dead.");
            // TODO: Play death animation

            ClearInventory();

            RespawnPlayer();

            // TODO: Play wake-up animation and in-hospital event for death
        }
    }


    public void RestoreHp(float amount) {
        // Log the action
        Debug.Log("Hp restored.");

        // Increase health
        _currentHp += amount;

        // Ensure health doesn't exceed the maximum
        if (_currentHp > _maxHp) {
            Debug.Log("Max Hp reached.");
            _currentHp = _maxHp;
        }

        // Update UI
        UpdateHealthUI();
    }

    private void UpdateHealthUI() {
        _healthAndEnergyPanel.ChangeHp(_currentHp);
    }

    private void ClearInventory() {
        Debug.Log("Cleared inventory.");
        // Clear the player's inventory
        //GetComponent<PlayerInventoryController>().InventoryContainer.slots.Clear();
    }

    private void RespawnPlayer() {
        Debug.Log("Set player to hospital position and set hp.");
        transform.position = _hospitalRespawnPosition;
        _currentHp = _hpAtRespawn;

        UpdateHealthUI();
    }

    public void ChangeMaxHp(int newMaxHp) {
        // Log the action
        Debug.Log("Max Hp changed.");

        // Update maximum health
        _maxHp = newMaxHp;

        // Update UI with new maximum health
        _healthAndEnergyPanel.ChangeMaxHp(_maxHp);
    }

    private void UpdateMaxHealthUI() {
        _healthAndEnergyPanel.ChangeMaxHp(_maxHp);
    }

    #endregion


    #region Energy
    public void RemoveEnergy(float amount) {
        // Log the action
        Debug.Log("Energy removed.");

        // Deduct energy
        _currentEnergy -= amount;

        // Update UI
        UpdateEnergyUI();

        // Check if the player is exhausted
        if (_currentEnergy <= 0) {
            Debug.Log("Player is exhausted.");

            // TODO: Play exhausted animation

            RespawnPlayer();

            // TODO: Play wake-up animation and in-hospital event for exhaustion
        }
    }

    public void RestoreEnergy(float amount) {
        Debug.Log("Energy restored.");

        // Increase energy
        _currentEnergy += amount;

        // Ensure energy doesn't exceed the maximum
        if (_currentEnergy > _maxEnergy) {
            Debug.Log("Max energy reached.");
            _currentEnergy = _maxEnergy;
        }

        // Update UI
        UpdateEnergyUI();
    }

    private void UpdateEnergyUI() {
        _healthAndEnergyPanel.ChangeEnergy(_currentEnergy);
    }

    public void ChangeMaxEnergy(int newMaxEnergy) {
        Debug.Log("Max Energy changed.");

        // Update maximum energy
        _maxEnergy = newMaxEnergy;

        // Update UI with new maximum energy
        UpdateMaxEnergyUI();
    }

    private void UpdateMaxEnergyUI() {
        _healthAndEnergyPanel.ChangeHp(_currentHp);
    }
    #endregion


    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.MaxHp = _maxHp;
        playerData.CurrentHp = _currentHp;
        playerData.MaxEnergy = _maxEnergy;
        playerData.CurrentEnergy = _currentEnergy;
        playerData.HosptitalRespawnPosition = _hospitalRespawnPosition;
    }

    public void LoadPlayer(PlayerData playerData) {
        _maxHp = playerData.MaxHp;
        _currentHp = playerData.CurrentHp;
        _maxEnergy = playerData.MaxEnergy;
        _currentEnergy = playerData.CurrentEnergy;
        _hospitalRespawnPosition = playerData.HosptitalRespawnPosition;
    }
    #endregion
}
