using UnityEngine;
using System;
using Unity.Netcode;

/// <summary>
/// Manages the financial aspects of the farm and town, handling money transactions and synchronization across the network.
/// </summary>
public class FinanceManager : NetworkBehaviour, IDataPersistance {
    public static FinanceManager Instance { get; private set; }

    // Events to notify subscribers about money changes
    public event Action<int> OnFarmMoneyChanged;
    public event Action<int> OnTownMoneyChanged;

    private const int MAX_MONEY = 99_999_999;

    // Networked variables to synchronize money for the farm and town
    private readonly NetworkVariable<int> _networkedFarmMoney = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> _networkedTownMoney = new NetworkVariable<int>(0);

    public int GetMoneyFarm => _networkedFarmMoney.Value;
    public int GetMoneyTown => _networkedTownMoney.Value;

    // Multiplayer-related fields
    private const float MAX_TIMEOUT = 2f;
    private bool _success;
    private bool _callbackSuccessful;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of FinanceManager in the scene!");
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        // Subscribe to changes in farm and town money
        _networkedFarmMoney.OnValueChanged += HandleFarmMoneyChanged;
        _networkedTownMoney.OnValueChanged += HandleTownMoneyChanged;

        // Initialize events with current values
        OnFarmMoneyChanged?.Invoke(_networkedFarmMoney.Value);
        OnTownMoneyChanged?.Invoke(_networkedTownMoney.Value);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        // Unsubscribe from events to prevent memory leaks
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        // Unsubscribe from network variable changes
        _networkedFarmMoney.OnValueChanged -= HandleFarmMoneyChanged;
        _networkedTownMoney.OnValueChanged -= HandleTownMoneyChanged;
    }

    /// <summary>
    /// Handles changes to the farm money network variable.
    /// </summary>
    /// <param name="oldValue">The previous value of farm money.</param>
    /// <param name="newValue">The new value of farm money.</param>
    private void HandleFarmMoneyChanged(int oldValue, int newValue) {
        if (oldValue != newValue) {
            OnFarmMoneyChanged?.Invoke(newValue);
        }
    }

    /// <summary>
    /// Handles changes to the town money network variable.
    /// </summary>
    /// <param name="oldValue">The previous value of town money.</param>
    /// <param name="newValue">The new value of town money.</param>
    private void HandleTownMoneyChanged(int oldValue, int newValue) {
        if (oldValue != newValue) {
            OnTownMoneyChanged?.Invoke(newValue);
        }
    }


    #region Client Late Join

    /// <summary>
    /// Called when a client connects to the server. Synchronizes money data with the connected client.
    /// </summary>
    /// <param name="clientId">The ID of the connected client.</param>
    private void OnClientConnected(ulong clientId) {
        SyncMoneyWithClientClientRpc(clientId);
    }

    /// <summary>
    /// Synchronizes the current money values with the newly connected client.
    /// </summary>
    /// <param name="clientId">The ID of the client to synchronize with.</param>
    [ClientRpc]
    private void SyncMoneyWithClientClientRpc(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId && !IsServer) {
            OnFarmMoneyChanged?.Invoke(_networkedFarmMoney.Value);
            OnTownMoneyChanged?.Invoke(_networkedTownMoney.Value);
        }
    }

    #endregion


    #region Add and Remove Farm Money

    /// <summary>
    /// Adds money to either the farm or town.
    /// </summary>
    /// <param name="amount">Amount to add.</param>
    /// <param name="isFarm">True if adding to farm, false for town.</param>
    [ServerRpc(RequireOwnership = false)]
    public void AddMoneyServerRpc(int amount, bool isFarm, ServerRpcParams rpcParams = default) {
        if (amount < 0) {
            Debug.LogError("Cannot add negative money.");
            return;
        }

        if (isFarm) {
            _networkedFarmMoney.Value = Mathf.Min(_networkedFarmMoney.Value + amount, MAX_MONEY);
        } else {
            _networkedTownMoney.Value = Mathf.Min(_networkedTownMoney.Value + amount, MAX_MONEY);
        }
    }

    /// <summary>
    /// Removes money from either the farm or town.
    /// </summary>
    /// <param name="amount">Amount to remove.</param>
    /// <param name="isFarm">True if removing from farm, false for town.</param>
    [ServerRpc(RequireOwnership = false)]
    public void RemoveMoneyFromFarmServerRpc(int amount, bool isFarm, ServerRpcParams rpcParams = default) {
        if (amount < 0) {
            Debug.LogError("Cannot remove negative money.");
            HandleClientCallbackClientRpc(rpcParams.Receive.SenderClientId, false);
            return;
        }

        bool success = false;

        if (isFarm) {
            if (_networkedFarmMoney.Value >= amount) {
                _networkedFarmMoney.Value -= amount;
                success = true;
            }
        } else {
            if (_networkedTownMoney.Value >= amount) {
                _networkedTownMoney.Value -= amount;
                success = true;
            }
        }

        HandleClientCallbackClientRpc(rpcParams.Receive.SenderClientId, success);
    }

    #endregion

    /// <summary>
    /// Handles client callbacks after money removal operations.
    /// </summary>
    /// <param name="clientId">The ID of the client.</param>
    /// <param name="success">Indicates if the operation was successful.</param>
    [ClientRpc]
    private void HandleClientCallbackClientRpc(ulong clientId, bool success) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            _callbackSuccessful = true;
            _success = success;
        }
    }


    #region Save and Load

    public void LoadData(GameData data) {
        _networkedFarmMoney.Value = data.MoneyOfFarm;
        _networkedTownMoney.Value = data.MoneyOfTown;
    }

    public void SaveData(GameData data) {
        data.MoneyOfFarm = _networkedFarmMoney.Value;
        data.MoneyOfTown = _networkedTownMoney.Value;
    }

    #endregion
}
