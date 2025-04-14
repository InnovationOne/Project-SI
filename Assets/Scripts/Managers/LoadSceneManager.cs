using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
public class LoadSceneManager : MonoBehaviour {
    public static LoadSceneManager Instance { get; private set; }

    public enum SceneName {
        MainMenuScene,
        LoadingScene,
        GameScene,
        // Further scenes if required
    }

    private SceneName _targetScene;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of LoadSceneManager in the scene!");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Loads the desired target scene via the LoadingScene.
    /// </summary>
    public void LoadSceneAsync(SceneName targetScene) {
        _targetScene = targetScene;
        SceneManager.LoadScene(SceneName.LoadingScene.ToString(), LoadSceneMode.Single);
    }

    /// <summary>
    /// This method is called by the LoadingScene to load the actual target.
    /// </summary>
    public IEnumerator LoadTargetSceneAsync() {
        yield return new WaitForSeconds(0.25f); // Optional delay for UI transition

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_targetScene.ToString());
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone) {
            yield return null;
        }
    }
}
