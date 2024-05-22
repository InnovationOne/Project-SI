using UnityEngine;
using System;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour {
    private TextMeshProUGUI _debugText;
    private Image _image;

    private float _deltaTime = 0.0f;
    private DateTime _startTime;

    private void Awake() {
        _debugText = GetComponentInChildren<TextMeshProUGUI>();
        _image = GetComponentInChildren<Image>();
    }

    private void Start() {
        _startTime = DateTime.Now;
        _debugText.gameObject.SetActive(false);
        _image.gameObject.SetActive(false);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F3)) {
            _debugText.gameObject.SetActive(!_debugText.gameObject.activeSelf);
            _image.gameObject.SetActive(!_image.gameObject.activeSelf);
        }

        if (_debugText.gameObject.activeSelf) {
            // Update deltaTime
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            // Get FPS
            float fps = 1.0f / _deltaTime;

            // Get Player position (assuming you have a method for this)
            Vector3 playerPosition = Player.LocalInstance.transform.position;

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

            // Update debug text
            _debugText.text = 
                $"Version: {Application.version}\n\n" +
                $"FPS: {Mathf.Ceil(fps)}\n\n" +
                $"Playtime: {gameTime}\n" +
                $"True time and date: {realDateTime}\n\n" +
                $"Player position: {playerPosition}\n" +                
                $"Ingame time and date: {ingameDateTime}\n" +
                $"Weather: {weather}\n" +
                $"Money: {farmMoney}\n\n" +
                $"HP: {playerHealth}, Max HP: {playerMaxHealth}\n" +
                $"Energy: {playerEnergy}, Max Energy: {playerMaxEnergy}\n\n" +
                $"Memory consumption: {memoryUsage}\n\n" +
                $"PC Info: \n{pcInfo}\n\n" +
                $"Press F1 for Cheat-Console";
        }
    }

    string GetGameTime() {
        TimeSpan gameTime = DateTime.Now - _startTime;
        return gameTime.ToString(@"hh\:mm\:ss");
    }

    string GetMemoryUsage() {
        long memoryUsed = Process.GetCurrentProcess().WorkingSet64;
        return (memoryUsed / (1024 * 1024)) + " MB";
    }

    string GetPCInfo() {
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
