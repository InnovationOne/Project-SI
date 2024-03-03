using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlot : MonoBehaviour
{
    public string profileId = "";

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI townName;
    [SerializeField] private TextMeshProUGUI lastPlayed;
    [SerializeField] private TextMeshProUGUI timePlayed;
    [SerializeField] private Image cityCoatOfArms;

    public void SetData(GameData data)
    {
        if (data == null)
            return;

        //playerName.text = data.playerName;
        townName.text = data.TownName;
        lastPlayed.text = DateTime.FromBinary(data.LastPlayed).ToString();
        timePlayed.text = data.TimePlayed.ToString() + " hours played";
        //cityCoatOfArms.sprite = data.cityCoatOfArms.sprite;
    }

    /// <summary>
    /// Gets the current profile id
    /// </summary>
    /// <returns>Profile idme</returns>
    public string GetProfileId()
    {
        return profileId;
    }

    public void Load()
    {
        //Update the selected profile id to be used for data persistence
        DataPersistenceManager.Instance.ChangeSelectedProfile(this.GetProfileId());

        //Save the game bevor loading a new scene
        DataPersistenceManager.Instance.SaveGame();

        //Load the scene
        //LoadSceneManager.Instance.LoadScene(1);
    }

    public void Duplicate()
    {
        DataPersistenceManager.Instance.DuplicateFile(this.GetProfileId());
        LoadGamePanel.instance.ActivateMenu();
        MainMenuPanel.instance.DisableButtonsDependingOnData();
    }

    public void Delete()
    {
        DataPersistenceManager.Instance.DeleteFile(this.GetProfileId());
        LoadGamePanel.instance.ActivateMenu();
        MainMenuPanel.instance.DisableButtonsDependingOnData();
    }

    public void OpenFolder()
    {
        DataPersistenceManager.Instance.OpenFileInExplorer(this.GetProfileId());
    }
}
