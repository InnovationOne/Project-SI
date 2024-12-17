using UnityEngine.SceneManagement;
using UnityEngine;
public class SI_LoadSceneManager : MonoBehaviour {

    public Scene TargetScene { get; private set; }

    public enum Scene {
        TitleScreenScene,
        LoadingScene,
        GameScene,
        // Add other scenes as needed
    }

    public void LS() {
        LoadScene(Scene.LoadingScene);
    }

    /// <summary>
    /// Loads a scene asynchronously via the LoadingScene.
    /// </summary>
    /// <param name="targetScene">The actual scene to load after the loading screen.</param>
    public void LoadScene(Scene targetScene) {
        TargetScene = targetScene;
        SceneManager.LoadScene(Scene.LoadingScene.ToString(), LoadSceneMode.Single);
    }
}
