using UnityEngine.SceneManagement;

public static class CustomLoadSceneManager {

    public static Scene TargetScene { get; private set; }

    public enum Scene {
        TitleScreenScene,
        LoadingScene,
        GameScene,
        // Add other scenes as needed
    }

    /// <summary>
    /// Loads a scene asynchronously via the LoadingScene.
    /// </summary>
    /// <param name="targetScene">The actual scene to load after the loading screen.</param>
    public static void LoadScene(Scene targetScene) {
        TargetScene = targetScene;
        SceneManager.LoadScene(Scene.LoadingScene.ToString(), LoadSceneMode.Single);
    }
}
