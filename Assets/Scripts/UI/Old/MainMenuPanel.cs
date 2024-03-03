using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
//Left Main Menu
//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

public class MainMenuPanel : MonoBehaviour
{
    public static MainMenuPanel instance { get; private set; }

    //*TODO* Disable alle buttons in main menu after one is clicked
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private List<GameObject> buttons;
    [SerializeField] private List<GameObject> panels;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one Main Menu Panel in the scene.");
            return;
        }

        instance = this;
    }

    private void Start()
    {
        DisableButtonsDependingOnData();
    }

    public void DisableButtonsDependingOnData()
    {
        if (!DataPersistenceManager.Instance.HasGameData())
        {
            continueButton.interactable = false;
            loadGameButton.interactable = false;
        }
    }

    public void SetPanelAndHighlight(int panelNum)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (i == panelNum)
                continue;
            panels[i].GetComponent<TitleScreenAnimation>().ToggleMenu();
            buttons[i].SetActive(false);
        }
        
        panels[panelNum].GetComponent<TitleScreenAnimation>().ToggleMenu();
        buttons[panelNum].SetActive(true);
    }

    public void RemovePanelHighlight()
    {
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].SetActive(false);
    }

    /// <summary>
    /// Continue
    /// </summary>
    public void Continue()
    {
        //Save the game bevor loading a new scene
        DataPersistenceManager.Instance.SaveGame();

        //Load the next scene, which will automaticly load the save
        //LoadSceneManager.Instance.LoadScene(1);
    }

    /// <summary>
    /// Quit game
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}
