using UnityEngine;
using System;
using Unity.Netcode;
using System.Collections;
using static UnityEditor.Progress;

public class FinanceManager : NetworkBehaviour, IDataPersistance {
    public static FinanceManager Instance { get; private set; }

    public event Action<int> OnUpdateChanged;

    [Header("Debug: Finance")]
    [SerializeField] private int _moneyOfFarm;
    private const int MAX_MONEY_OF_FARM = 99999999;

    [Header("Debug: Save and Load")]
    [SerializeField] private bool _saveFinance = true;
    [SerializeField] private bool _loadFinance = true;

    // Client <-> Server communication
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
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnected;
        }

        OnUpdateChanged?.Invoke(_moneyOfFarm);
    }


    #region Client Late Join
    private void NetworkManager_OnClientConnected(ulong clientId) {
        NetworkManager_OnClientConnected_ClientRpc(clientId, _moneyOfFarm);
    }

    [ClientRpc]
    private void NetworkManager_OnClientConnected_ClientRpc(ulong clientId, int moneyOfFarm) {
        if (clientId == NetworkManager.Singleton.LocalClientId && !IsServer) {
            _moneyOfFarm = moneyOfFarm;
            OnUpdateChanged?.Invoke(_moneyOfFarm);
        }
    }
    #endregion


    #region Add money
    [ServerRpc(RequireOwnership = false)]
    public void AddMoneyToFarmServerRpc(int money) {
        if (money < 0) {
            Debug.LogError("Cannot add negative money.");
            return;
        }

        _moneyOfFarm = Mathf.Min(_moneyOfFarm + money, MAX_MONEY_OF_FARM);
        AddMoneyToFarmClientRpc(_moneyOfFarm);
    }

    [ClientRpc]
    private void AddMoneyToFarmClientRpc(int money) {
        _moneyOfFarm = money;
        OnUpdateChanged?.Invoke(_moneyOfFarm);
    }
    #endregion


    #region Remove money
    public IEnumerator PerformRemoveMoney(int money, int itemId = -1, int amount = 0, int rarity = 0) {
        _callbackSuccessful = false;
        _success = false;

        // Execute remove money
        TryRemoveMoneyFromFarmServerRpc(money);

        yield return WaitForServerResponse();

        ProcessRemoveMoneyResult(money, itemId, amount, rarity);
    }

    private IEnumerator WaitForServerResponse() {
        float startTime = Time.time;
        while (!_callbackSuccessful && (Time.time - startTime) < MAX_TIMEOUT) {
            yield return null;
        }

        if (!_callbackSuccessful && (Time.time - startTime) >= MAX_TIMEOUT) {
            Debug.LogError("Remove money TIMEOUT!");
        }
    }

    private void ProcessRemoveMoneyResult(int money, int itemId, int amount, int rarity) {
        if (!_success) {
            Debug.Log($"Not enough money available. Money needed: {money}, money available: {_moneyOfFarm}. {money - _moneyOfFarm} more needed.");
        } else {
            ManageItemAddition(money, itemId, amount, rarity);
        }
    }

    private void ManageItemAddition(int money, int itemId, int amount, int rarity) {
        if (itemId == -1) {
            Debug.Log("No item to add.");
        } else {
            Debug.Log($"{ItemManager.Instance.ItemDatabase[itemId].ItemName} added for {money} money");
            PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(itemId, amount, rarity, false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryRemoveMoneyFromFarmServerRpc(int money, ServerRpcParams serverRpcParams = default) {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (_moneyOfFarm >= money) {
            _moneyOfFarm -= money;

            HandleClientCallbackClientRpc(clientId, true);

            RemoveMoneyFromFarmClientRpc(_moneyOfFarm);

        } else {
            HandleClientCallbackClientRpc(clientId, false);
        }
    }

    [ClientRpc]
    private void RemoveMoneyFromFarmClientRpc(int money) {
        _moneyOfFarm = money;
        OnUpdateChanged?.Invoke(_moneyOfFarm);
    }

    [ClientRpc]
    private void HandleClientCallbackClientRpc(ulong clientId, bool success) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            _callbackSuccessful = true;
            _success = success;
        }
    }
    #endregion


    #region Save and Load
    public void LoadData(GameData data) {
        if (_loadFinance) {
            _moneyOfFarm = data.MoneyOfFarm;
        }
    }

    public void SaveData(GameData data) {
        if (_saveFinance) {
            data.MoneyOfFarm = _moneyOfFarm;
        }
    }
    #endregion
}
