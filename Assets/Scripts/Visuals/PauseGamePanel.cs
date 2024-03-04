using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseGamePanel : MonoBehaviour {
    public static PauseGamePanel Instance { get; private set; }

    public event Action OnResumeGameButtonPressed;
    public event Action OnOptionsButtonPressed;
    public event Action OnTitleScreenButtonPressed;
    public event Action OnExitGameButtonPressed;

    [Header("Local Game Paused")]
    [SerializeField] private Transform _localPauseGamePanel;
    [SerializeField] private Button _resumeGameButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _titleScreenButton;
    [SerializeField] private Button _exitGameButton;

    [Header("Game Paused")]
    [SerializeField] private Transform _pauseGamePanel;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one local instance of PauseGamePanel in the scene!");
            return;
        }
        Instance = this;

        _resumeGameButton.onClick.AddListener(() => OnResumeGameButtonPressed?.Invoke());
        _optionsButton.onClick.AddListener(() => OnOptionsButtonPressed?.Invoke());
        _titleScreenButton.onClick.AddListener(() => OnTitleScreenButtonPressed?.Invoke());
        _exitGameButton.onClick.AddListener(() => OnExitGameButtonPressed?.Invoke());
    }

    private void Start() {
        PauseGameManager.Instance.OnShowLocalPauseGame += PauseMenuManager_ShowLocalPauseGamePanel;
        PauseGameManager.Instance.OnHideLocalPauseGame += PauseMenuManager_HideLocalPauseGamePanel;
        PauseGameManager.Instance.OnShowPauseGame += PauseMenuManager_ShowPauseGamePanel;
        PauseGameManager.Instance.OnHidePauseGame += PauseMenuManager_HidePauseGamePanel;

        gameObject.SetActive(false);
        _localPauseGamePanel.gameObject.SetActive(false);
        _pauseGamePanel.gameObject.SetActive(false);
    }

    private void PauseMenuManager_ShowLocalPauseGamePanel() {
        gameObject.SetActive(true);
        _localPauseGamePanel.gameObject.SetActive(true);

        if (_pauseGamePanel.gameObject.activeSelf) {
            _pauseGamePanel.gameObject.SetActive(false);
        }
    }

    private void PauseMenuManager_HideLocalPauseGamePanel() {
        _localPauseGamePanel.gameObject.SetActive(false);

        if (PauseGameManager.Instance.IsGamePaused.Value) {
            _pauseGamePanel.gameObject.SetActive(true);
            return;
        }

        gameObject.SetActive(false);
    }

    private void PauseMenuManager_ShowPauseGamePanel() {
        if (_localPauseGamePanel.gameObject.activeSelf) {
            return;
        }

        gameObject.SetActive(true);

        _pauseGamePanel.gameObject.SetActive(true);
    }

    private void PauseMenuManager_HidePauseGamePanel() {
        gameObject.SetActive(false);
        _pauseGamePanel.gameObject.SetActive(false);
    }
}
