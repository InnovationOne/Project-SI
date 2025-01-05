using UnityEngine;
using System;
using Unity.Netcode;

/// <summary>
/// Manages money for farm and town, syncing changes across the network.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class FinanceManager : NetworkBehaviour, IDataPersistance {
    // Events to notify subscribers about money changes
    public event Action<int> OnFarmMoneyChanged;
    public event Action<int> OnTownMoneyChanged;

    const int MAX_MONEY = 99_999_999;

    // Networked variables to synchronize money for the farm and town
    readonly NetworkVariable<int> _networkedFarmMoney = new(0);
    readonly NetworkVariable<int> _networkedTownMoney = new(0);

    public int GetMoneyFarm => _networkedFarmMoney.Value;
    public int GetMoneyTown => _networkedTownMoney.Value;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        _networkedFarmMoney.OnValueChanged += HandleFarmMoneyChanged;
        _networkedTownMoney.OnValueChanged += HandleTownMoneyChanged;

        // Invoke current values so clients see the correct initial amounts
        OnFarmMoneyChanged?.Invoke(_networkedFarmMoney.Value);
        OnTownMoneyChanged?.Invoke(_networkedTownMoney.Value);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        _networkedFarmMoney.OnValueChanged -= HandleFarmMoneyChanged;
        _networkedTownMoney.OnValueChanged -= HandleTownMoneyChanged;
    }

    // Notifies subscribers when farm money changes.
    void HandleFarmMoneyChanged(int oldValue, int newValue) {
        if (oldValue != newValue) {
            OnFarmMoneyChanged?.Invoke(newValue);
        }
    }

    // Notifies subscribers when town money changes.
    void HandleTownMoneyChanged(int oldValue, int newValue) {
        if (oldValue != newValue) {
            OnTownMoneyChanged?.Invoke(newValue);
        }
    }

    // Called when a new client connects, syncs current money to them.
    void OnClientConnected(ulong clientId) => SyncMoneyWithClientClientRpc(clientId);

    [ClientRpc]
    void SyncMoneyWithClientClientRpc(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId && !IsServer) {
            OnFarmMoneyChanged?.Invoke(_networkedFarmMoney.Value);
            OnTownMoneyChanged?.Invoke(_networkedTownMoney.Value);
        }
    }

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

    [ServerRpc(RequireOwnership = false)]
    public void RemoveMoneyServerRpc(int amount, bool isFarm, ServerRpcParams rpcParams = default) {
        if (amount < 0) {
            Debug.LogError("Cannot remove negative money.");
            return;
        }

        if (isFarm) {
            if (_networkedFarmMoney.Value >= amount) {
                _networkedFarmMoney.Value -= amount;
            }
        } else {
            if (_networkedTownMoney.Value >= amount) {
                _networkedTownMoney.Value -= amount;
            }
        }
    }

    public void LoadData(GameData data) {
        _networkedFarmMoney.Value = data.MoneyOfFarm;
        _networkedTownMoney.Value = data.MoneyOfTown;
    }

    public void SaveData(GameData data) {
        data.MoneyOfFarm = _networkedFarmMoney.Value;
        data.MoneyOfTown = _networkedTownMoney.Value;
    }
}
