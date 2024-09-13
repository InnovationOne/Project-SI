using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneManager : MonoBehaviour {

    public enum Scene {
        TitleScreenScene,
        GameScene,
        LoadingScene,
    }

    private static Scene _targetScene;

    public static void LoadScene(Scene targetScene) {
        _targetScene = targetScene;
        SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static void LoadNetwork(Scene targetScene) {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }
}
