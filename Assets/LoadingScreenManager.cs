using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private Image _loadingIcon;
    [SerializeField] private Slider _loadingBar;
    [SerializeField] private TextMeshProUGUI _debugInfoText;
    [SerializeField] private Image _loadingBGImage;
    [SerializeField] private TextMeshProUGUI _tipText;
    [SerializeField] private Button _nextTipButton;
    [SerializeField] private Button _previousTipButton;

    [Header("Assets")]
    [SerializeField] private List<Sprite> _loadingSprites; // For images like GTA, Anno
    [SerializeField] private List<string> _tips; // Loading tips

    [Header("Loading Settings")]
    [SerializeField] private int _minimumDisplayTime; // Minimum time in seconds to display the loading screen

    private int _currentTipIndex = 0;
    private LoadingTask _loadingTask = new();

    private void Start() {
        GameManager.Instance.AudioManager.InitializeMusic(GameManager.Instance.FMODEvents.Loading);
        InitializeUI();
        StartCoroutine(StartLoading());
        StartCoroutine(CycleLoadingImages());
    }

    /// <summary>
    /// Initializes the UI elements and assigns button listeners.
    /// </summary>
    private void InitializeUI() {
        if (_tips.Count > 0) {
            UpdateTip();
        }

        _nextTipButton.onClick.AddListener(NextTip);
        _previousTipButton.onClick.AddListener(PreviousTip);
    }

    /// <summary>
    /// Coroutine to handle the loading process.
    /// </summary>
    private IEnumerator StartLoading() {
        // Start the asynchronous loading task
        var loadTask = LoadGameSceneAsync("GameScene");

        // While the task is not completed, update the UI
        while (!loadTask.IsCompleted) {
            _loadingBar.value = _loadingTask.Progress;
            _debugInfoText.text = _loadingTask.CurrentLoadingStep;
            yield return null;
        }

        // Ensure the final state is reflected
        _loadingBar.value = _loadingTask.Progress;
        _debugInfoText.text = _loadingTask.CurrentLoadingStep;

        // Once loading is complete and minimum time has passed, finalize UI
        _loadingBar.value = 1f;
        _debugInfoText.text = "Loading Complete!";
    }

    /// <summary>
    /// Asynchronously loads the specified scene and updates the loading task.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task LoadGameSceneAsync(string sceneName) {
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
        asyncOp.allowSceneActivation = false;

        while (!asyncOp.isDone) {
            // Calculate progress (0.0 to 1.0)
            _loadingTask.Progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            _loadingTask.CurrentLoadingStep = $"Loading {sceneName}: {_loadingTask.Progress * 100:F0}%";

            // When the load is almost done, allow activation
            if (asyncOp.progress >= 0.9f) {
                _loadingTask.Progress = 1f;
                _loadingTask.CurrentLoadingStep = "Finalizing...";
                _loadingBar.value = _loadingTask.Progress;
                _debugInfoText.text = _loadingTask.CurrentLoadingStep;

                // Artificial delay for testing
                await Task.Delay(_minimumDisplayTime * 1000);
                GameManager.Instance.AudioManager.StopMusic();

                asyncOp.allowSceneActivation = true;
            }

            await Task.Yield(); // Yield to keep the update responsive
        }
    }

    /// <summary>
    /// Rotates the loading icon and handles tip navigation inputs.
    /// </summary>
    private void Update() {
        // Handle tip navigation via keyboard
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) {
            NextTip();
        } else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) {
            PreviousTip();
        }
    }

    /// <summary>
    /// Updates the tip text based on the current index.
    /// </summary>
    private void UpdateTip() {
        if (_tips.Count == 0) { 
            return; 
        }

        _tipText.text = _tips[_currentTipIndex];
    }

    /// <summary>
    /// Advances to the next tip in the list.
    /// </summary>
    private void NextTip() {
        if (_tips.Count == 0) {
            return;
        }

        _currentTipIndex = (_currentTipIndex + 1) % _tips.Count;
        UpdateTip();
    }

    /// <summary>
    /// Goes back to the previous tip in the list.
    /// </summary>
    private void PreviousTip() {
        if (_tips.Count == 0) {
            return;
        }

        _currentTipIndex = (_currentTipIndex - 1 + _tips.Count) % _tips.Count;
        UpdateTip();
    }

    /// <summary>
    /// Cycles through loading images by fading between each other.
    /// </summary>
    private IEnumerator CycleLoadingImages() {
        int index = 0;
        float fadeDuration = 0.5f; // Duration for fade in/out in seconds
        while (true) {
            if (_loadingSprites.Count > 0 && _loadingBGImage != null) {
                // Fade out the current image
                yield return StartCoroutine(FadeImage(_loadingBGImage, 1f, 0f, fadeDuration));

                // Change to the next sprite
                _loadingBGImage.sprite = _loadingSprites[index % _loadingSprites.Count];
                index++;

                // Fade in the new image
                yield return StartCoroutine(FadeImage(_loadingBGImage, 0f, 1f, fadeDuration));
            }

            // Wait for the remaining time before the next transition
            // Total time per image: fade out + fade in + display time
            float displayTime = 2f - (fadeDuration * 2);
            if (displayTime > 0) {
                yield return new WaitForSeconds(displayTime);
            } else {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Fades an Image's alpha from startAlpha to endAlpha over the specified duration.
    /// </summary>
    /// <param name="image">The Image component to fade.</param>
    /// <param name="startAlpha">Starting alpha value (0 to 1).</param>
    /// <param name="endAlpha">Ending alpha value (0 to 1).</param>
    /// <param name="duration">Duration of the fade in seconds.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration) {
        float elapsed = 0f;
        Color color = image.color;
        color.a = startAlpha;
        image.color = color;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        // Ensure the final alpha is set
        color.a = endAlpha;
        image.color = color;
    }

    /// <summary>
    /// Represents the current state of the loading task.
    /// </summary>
    [System.Serializable]
    private class LoadingTask {
        public float Progress { get; set; } = 0f;
        public string CurrentLoadingStep { get; set; } = "Initializing...";
    }
}
