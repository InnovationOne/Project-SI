using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour {
    [Header("Tabs (Buttons)")]
    [SerializeField] private Button _graphicsTab;
    [SerializeField] private Button _audioTab;
    [SerializeField] private Button _controlsTab;
    [SerializeField] private Button _gameplayTab;

    [Header("Panels (Content)")]
    [SerializeField] private GameObject _graphicsPanel;
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _controlsPanel;
    [SerializeField] private GameObject _gameplayPanel;

    private Dictionary<Button, GameObject> _tabToPanelMap;

    private void Awake() {
        _tabToPanelMap = new Dictionary<Button, GameObject> {
            { _graphicsTab, _graphicsPanel },
            { _audioTab, _audioPanel },
            { _controlsTab, _controlsPanel },
            { _gameplayTab, _gameplayPanel }
        };

        foreach (var pair in _tabToPanelMap) {
            Button tab = pair.Key;
            tab.onClick.AddListener(() => ShowPanel(pair.Value));
        }

        // Standardm‰ﬂig erstes Panel aktivieren
        ShowPanel(_graphicsPanel);
    }

    private void ShowPanel(GameObject activePanel) {
        foreach (var panel in _tabToPanelMap.Values) {
            panel.SetActive(panel == activePanel);
        }
    }
}
