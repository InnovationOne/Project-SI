using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(NetworkObject))]
public class PauseGameManager : NetworkBehaviour {
    public event Action OnShowLocalPauseGame;
    public event Action OnHideLocalPauseGame;
    public event Action OnShowPauseGame;
    public event Action OnHidePauseGame;
    public event Action OnShowUIForPause;
    public event Action OnHideUIForPause;

    public NetworkVariable<bool> IsGamePaused { get; private set; } = new(false);

    private bool autoTestGamePausedState = false;
    private bool isLocalGamePaused;
    private Dictionary<ulong, bool> playerPausedDict;


    private void Awake() {
        playerPausedDict = new Dictionary<ulong, bool>();
    }

    public override void OnNetworkSpawn() {
        IsGamePaused.OnValueChanged += IsGamePaused_OnValueChanged;

        if (IsServer) {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }
    }

    private void IsGamePaused_OnValueChanged(bool previousValue, bool newValue) {
        if (IsGamePaused.Value) {
            OnShowPauseGame?.Invoke();
            Time.timeScale = 0f;
            OnHideUIForPause?.Invoke();
        } else {
            OnHidePauseGame?.Invoke();
            Time.timeScale = 1f;
            OnShowUIForPause?.Invoke();
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId) {
        autoTestGamePausedState = true;
    }

    private void Start() {
        GameManager.Instance.InputManager.OnEscapeAction += InputManager_TogglePauseGame;

        UIManager.Instance.PauseGameUI.OnResumeGameButtonPressed += InputManager_TogglePauseGame;
        UIManager.Instance.PauseGameUI.OnOptionsButtonPressed += PauseGamePanel_OnOptionsButtonPressed;
        UIManager.Instance.PauseGameUI.OnTitleScreenButtonPressed += PauseGamePanel_OnTitleScreenButtonPressed;
        UIManager.Instance.PauseGameUI.OnExitGameButtonPressed += PauseGamePanel_OnExitGameButtonPressed;
    }

    private void LateUpdate() {
        // After a player that paused disconnects check if the game should be unpaused
        if (autoTestGamePausedState) {
            autoTestGamePausedState = false;

            TestGamePausedState();
        }
    }

    public void InputManager_TogglePauseGame() {
        // Close inventory first
        if (UIManager.Instance.InventoryRoot.activeSelf) {
            return;
        }

        // Toggle the local game paused state
        isLocalGamePaused = !isLocalGamePaused;

        if (isLocalGamePaused) {
            PauseGameServerRpc();

            OnShowLocalPauseGame?.Invoke();
        } else {
            UnpauseGameServerRpc();

            OnHideLocalPauseGame?.Invoke();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PauseGameServerRpc(ServerRpcParams serverRpcParams = default) {
        playerPausedDict[serverRpcParams.Receive.SenderClientId] = true;

        TestGamePausedState();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnpauseGameServerRpc(ServerRpcParams serverRpcParams = default) {
        playerPausedDict[serverRpcParams.Receive.SenderClientId] = false;

        TestGamePausedState();
    }

    private void TestGamePausedState() {
        return;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (playerPausedDict.ContainsKey(clientId) && playerPausedDict[clientId]) {
                // This player is paused
                IsGamePaused.Value = true;
                return;
            }
        }

        // All players are unpaused
        IsGamePaused.Value = false;
    }

    private void PauseGamePanel_OnOptionsButtonPressed() {
        Debug.Log("Options Menu not implemented yet");
    }

    private void PauseGamePanel_OnTitleScreenButtonPressed() {
        Time.timeScale = 1f;
        Debug.Log("Main Menu Button Pressed");
        //NetworkManager.Singleton.Shutdown();
        //LoadSceneManager.LoadScene(LoadSceneManager.Scene.TitleScreenScene);
    }

    private void PauseGamePanel_OnExitGameButtonPressed() {
        Application.Quit();
    }
}
