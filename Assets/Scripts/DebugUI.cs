using UnityEngine;
using System;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _debugText;
    [SerializeField] private Image _image;

    private float _deltaTime = 0.0f;
    private DateTime _startTime;
    private CheatUI _cheatConsole;

    private void Awake() {
        _debugText = GetComponentInChildren<TextMeshProUGUI>();
        _image = GetComponentInChildren<Image>();
        _cheatConsole = GetComponentInChildren<CheatUI>();
    }

    private void Start() {
        var inputManager = InputManager.Instance;
        inputManager.DebugConsole_OnCheatConsoleAction += ToggleCheatConsole;
        inputManager.DebugConsole_OnDebugConsoleAction += ToggleDebugConsole;

        _startTime = DateTime.Now;
        _debugText.gameObject.SetActive(false);
        _image.gameObject.SetActive(false);
        _cheatConsole.gameObject.SetActive(false);
    }

    /// <summary>
    /// Toggles the cheat console on or off.
    /// </summary>
    private void ToggleCheatConsole() {
        _cheatConsole.gameObject.SetActive(!_cheatConsole.gameObject.activeSelf);
    }

    /// <summary>
    /// Toggles the visibility of the debug console.
    /// </summary>
    private void ToggleDebugConsole() {
        bool isActive = !_debugText.gameObject.activeSelf;
        _debugText.gameObject.SetActive(isActive);
        _image.gameObject.SetActive(isActive);
        if (isActive) {
            InputManager.Instance.EnableDebugConsoleActionMap();
        } else {
            _cheatConsole.gameObject.SetActive(false);
            InputManager.Instance.EnablePlayerActionMap();
        }
    }

    /// <summary>
    /// Updates the debug screen text with various game information.
    /// </summary>
    private void Update() {
        if (_debugText.gameObject.activeSelf) {
            // Update deltaTime
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            // Get FPS
            float fps = 1.0f / _deltaTime;

            // Get Player position
            Vector3 playerPosition = Player.LocalInstance != null ? Player.LocalInstance.transform.position : Vector3.zero;

            // Get game time
            string gameTime = GetGameTime();

            // Get current date and time
            string realDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Get ingame date and time
            string ingameDateTime = TimeAndWeatherManager.Instance.GetDateTime;

            // Get weather
            string weather = TimeAndWeatherManager.Instance.GetWeather;

            // Get farm stats
            int farmMoney = FinanceManager.Instance.GetMoney;

            // Get player stats
            float playerHealth = PlayerHealthAndEnergyController.LocalInstance.CurrentHealth;
            float playerMaxHealth = PlayerHealthAndEnergyController.LocalInstance.MaxHealth;
            float playerEnergy = PlayerHealthAndEnergyController.LocalInstance.CurrentEnergy;
            float playerMaxEnergy = PlayerHealthAndEnergyController.LocalInstance.MaxEnergy;

            // Get memory usage
            string memoryUsage = GetMemoryUsage();

            // Get PC information
            string pcInfo = GetPCInfo();

            // Use StringBuilder to update debug text
            var sb = new StringBuilder();
            sb.AppendLine($"Version: {Application.version}")
              .AppendLine()
              .AppendLine($"FPS: {Mathf.Ceil(fps)}")
              .AppendLine()
              .AppendLine($"Playtime: {gameTime}")
              .AppendLine($"True time and date: {realDateTime}")
              .AppendLine()
              .AppendLine($"Player position: {playerPosition}")
              .AppendLine($"Ingame time and date: {ingameDateTime}")
              .AppendLine($"Weather: {weather}")
              .AppendLine($"Money: {farmMoney}")
              .AppendLine()
              .AppendLine($"HP: {playerHealth}, Max HP: {playerMaxHealth}")
              .AppendLine($"Energy: {playerEnergy}, Max Energy: {playerMaxEnergy}")
              .AppendLine()
              .AppendLine($"Memory consumption: {memoryUsage}")
              .AppendLine()
              .AppendLine($"PC Info: {pcInfo}")
              .AppendLine()
              .AppendLine($"Press F1 for Cheat-Console");

            _debugText.text = sb.ToString();
        }
    }

    /// <summary>
    /// Gets the current game time as a formatted string.
    /// </summary>
    /// <returns>The game time as a formatted string (hh:mm:ss).</returns>
    private string GetGameTime() {
        TimeSpan gameTime = DateTime.Now - _startTime;
        return gameTime.ToString(@"hh\:mm\:ss");
    }

    /// <summary>
    /// Retrieves the memory usage of the current process.
    /// </summary>
    /// <returns>A string representing the memory usage in megabytes (MB).</returns>
    private string GetMemoryUsage() {
        long memoryUsed = Process.GetCurrentProcess().WorkingSet64;
        return (memoryUsed / (1024 * 1024)) + " MB";
    }

    /// <summary>
    /// Retrieves information about the PC, including CPU, GPU, and resolution.
    /// </summary>
    /// <returns>A string containing the PC information.</returns>
    private string GetPCInfo() {
        StringBuilder sb = new StringBuilder();

        // Get CPU info
        sb.AppendLine("CPU: " + SystemInfo.processorType);

        // Get GPU info
        sb.AppendLine("GPU: " + SystemInfo.graphicsDeviceName);

        // Get resolution
        sb.AppendLine("Resolution: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height);

        return sb.ToString();
    }
}
