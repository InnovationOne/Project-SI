using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadGameManager : MonoBehaviour {
    [Header("Slot Instanzierung")]
    [SerializeField] private GameObject _saveSlotPrefab;
    [SerializeField] private Transform _slotContainer;

    [Header("Bestätigungsdialog")]
    [SerializeField] private GameObject _confirmationDialog;
    [SerializeField] private Button _confirmDeleteButton;
    [SerializeField] private Button _cancelDeleteButton;

    private string _profileIdToDelete;

    private void Start() {
        LoadAllSaveSlots();
        SetupDialogButtons();
    }

    private void LoadAllSaveSlots() {
        var allSaves = DataPersistenceManager.Instance.GetAllProfilesGameData();

        foreach (Transform child in _slotContainer) {
            Destroy(child.gameObject); // Clean slate
        }

        foreach (var pair in allSaves) {
            string profileId = pair.Key;
            GameData data = pair.Value;

            GameObject slotGO = Instantiate(_saveSlotPrefab, _slotContainer);
            var slotUI = slotGO.GetComponent<SaveSlotUI>();
            slotUI.SetData(profileId, data,
                OnLoadClicked,
                OnDuplicateClicked,
                OnDeleteClicked);
        }
    }

    private void OnLoadClicked(string profileId) {
        DataPersistenceManager.Instance.ChangeSelectedProfile(profileId);
        LoadSceneManager.Instance.LoadSceneAsync(LoadSceneManager.SceneName.GameScene);
    }

    private void OnDuplicateClicked(string profileId) {
        DataPersistenceManager.Instance.DuplicateFile(profileId);
        LoadAllSaveSlots();
    }

    private void OnDeleteClicked(string profileId) {
        _profileIdToDelete = profileId;
        _confirmationDialog.SetActive(true);
    }

    private void SetupDialogButtons() {
        _confirmDeleteButton.onClick.AddListener(() => {
            DataPersistenceManager.Instance.DeleteFile(_profileIdToDelete);
            _confirmationDialog.SetActive(false);
            LoadAllSaveSlots();
        });

        _cancelDeleteButton.onClick.AddListener(() => {
            _profileIdToDelete = null;
            _confirmationDialog.SetActive(false);
        });

        _confirmationDialog.SetActive(false);
    }
}
