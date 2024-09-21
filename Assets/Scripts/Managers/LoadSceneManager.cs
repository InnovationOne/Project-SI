using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneManager : MonoBehaviour {

    public enum Scene {
        TitleScreenScene,
        GameScene,
        LoadingScene,
    }

    public static Scene _targetScene;


    // For getting from the main menu to the game for testing
    [SerializeField] private Button _continueButton;
    private void Awake() {
        if (_continueButton != null) {
            _continueButton.onClick.AddListener(() => LoadScene(Scene.GameScene));
        }
    }

    public static void LoadScene(Scene targetScene) {
        _targetScene = targetScene;
        SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static void LoadNetwork(Scene targetScene) {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }
}
