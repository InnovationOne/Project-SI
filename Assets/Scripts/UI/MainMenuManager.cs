using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour {
    [Header("Main Menu Buttons")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _loadGameButton;
    [SerializeField] private Button _multiplayerButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _quitButton;

    [Header("Submenu Panels")]
    [SerializeField] private GameObject _newGamePanel;
    [SerializeField] private GameObject _loadGamePanel;
    [SerializeField] private GameObject _multiplayerPanel;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private GameObject _creditsPanel;

    private Dictionary<string, GameObject> _panelMap;


    void Awake() {
        _panelMap = new Dictionary<string, GameObject> {
            { "NewGame", _newGamePanel },
            { "LoadGame", _loadGamePanel },
            { "Multiplayer", _multiplayerPanel },
            { "Options", _optionsPanel },
            { "Credits", _creditsPanel },
        };

        _continueButton.onClick.AddListener(OnContinueClicked);
        _newGameButton.onClick.AddListener(() => ShowPanel("NewGame"));
        _loadGameButton.onClick.AddListener(() => ShowPanel("LoadGame"));
        _multiplayerButton.onClick.AddListener(() => ShowPanel("Multiplayer"));
        _optionsButton.onClick.AddListener(() => ShowPanel("Options"));
        _creditsButton.onClick.AddListener(() => ShowPanel("Credits"));
        _quitButton.onClick.AddListener(OnQuitClicked);

        HideAllPanels();
    }

    private void Start() {
        // Check for any saved profiles.
        var profiles = DataPersistenceManager.Instance.GetAllProfilesGameData();
        _continueButton.interactable = profiles.Count > 0;

        // Preload the last saved game data into memory.
        StartCoroutine(DelayedPreloadGameData());
    }

    private IEnumerator DelayedPreloadGameData() {
        yield return new WaitForSeconds(0.1f);
        DataPersistenceManager.Instance.LoadGame();
    }

    private void ShowPanel(string panelKey) {
        foreach (var kvp in _panelMap) {
            kvp.Value.SetActive(kvp.Key == panelKey);
        }
    }

    private void HideAllPanels() {
        foreach (var panel in _panelMap.Values) {
            panel.SetActive(false);
        }
    }

    private void OnContinueClicked() {
        LoadSceneManager.Instance.LoadSceneAsync(LoadSceneManager.SceneName.GameScene);
    }

    private void OnQuitClicked() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        DataPersistenceManager.Instance.QuitGame();
#endif
    }
}
