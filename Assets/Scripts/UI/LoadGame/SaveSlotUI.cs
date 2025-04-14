using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SaveSlotUI : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _moneyText;
    [SerializeField] private TMP_Text _timePlayedText;
    [SerializeField] private TMP_Text _dateText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _duplicateButton;
    [SerializeField] private Button _deleteButton;

    private string _profileId;
    private Action<string> _onLoadClicked;
    private Action<string> _onDuplicateClicked;
    private Action<string> _onDeleteClicked;

    public void SetData(string profileId, GameData data,
                        Action<string> onLoad, Action<string> onDuplicate, Action<string> onDelete) {
        _profileId = profileId;
        _onLoadClicked = onLoad;
        _onDuplicateClicked = onDuplicate;
        _onDeleteClicked = onDelete;

        _nameText.text = data.Players.Count > 0 ? data.Players[0].Name : "Unbekannt";
        _moneyText.text = $"Money: {data.MoneyOfFarm} G";
        _timePlayedText.text = $"Time played: {data.PlayTime:hh\\:mm}";
        _dateText.text = $"Last played: {DateTime.FromBinary(data.LastPlayed):dd.MM.yyyy}";

        _loadButton.onClick.AddListener(() => _onLoadClicked?.Invoke(_profileId));
        _duplicateButton.onClick.AddListener(() => _onDuplicateClicked?.Invoke(_profileId));
        _deleteButton.onClick.AddListener(() => _onDeleteClicked?.Invoke(_profileId));
    }
}
