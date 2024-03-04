using UnityEngine;
using System;
using Unity.Netcode;
using System.Collections;

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
    private bool _callbackSuccessfull;
    private float _timeout;
    private float _elapsedTime;


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Finance Manager in the scene.");
        } else {
            Instance = this;
        }
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
            Debug.LogError("Cannot add negative money to the farm.");
            return;
        }

        _moneyOfFarm += money;

        if (_moneyOfFarm > MAX_MONEY_OF_FARM) {
            _moneyOfFarm = MAX_MONEY_OF_FARM;
        }

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
        ResetCallbackParams();

        // Execute remove money
        TryRemoveMoneyFromFarmServerRpc(money);

        // Wait for the ServerRpc response
        while (!_callbackSuccessfull && _elapsedTime < _timeout) {
            yield return null;
            _elapsedTime += Time.deltaTime;
        }

        if (!_success) {
            if (_elapsedTime >= _timeout) {
                Debug.LogError("Remove money TIMEOUT!");
            }
            // Player has NOT enough money.
            Debug.Log($"No enough money available. Money needed {money}, money available {_moneyOfFarm}. {money - _moneyOfFarm} money more needed");
        } else {
            if (itemId == -1) {
                // Don't add an item to the inventory
                // do smth else
            } else {
                // Add the item to the inventory
                Debug.Log($"{ItemManager.Instance.ItemDatabase.GetItem(itemId).ItemName} added for {money} money");
                PlayerInventoryController.LocalInstance.InventoryContainer.AddItemToItemContainer(itemId, amount, rarity, false);
            }
        }
    }

    private void ResetCallbackParams() {
        _callbackSuccessfull = false;
        _success = false;
        _timeout = MAX_TIMEOUT;
        _elapsedTime = 0f;
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
            _callbackSuccessfull = true;
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
