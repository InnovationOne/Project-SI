using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using TMPro;
using System.Linq;
using System;

//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
//Handels the functionality for the NewGame-Menu
//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

public enum MaleName
{
    John, William, James, George, Charles, Robert, Joseph, Frank, Edward, Thomas, Henry, Walter, Harry, Willie, Arthur, Albert, Clarence, Fred, Harold,Paul, Raymond, Richard, Roy, Joe, Louis, 
    Carl, Ralph, Earl, Jack, Earnest, David, Samuel, Howard, Charlie, Francis, Herbert, Lawrence, Theodore, Alfred, Andrew, Sam, Elmer, Eugene, Leo, Michael, Lee, Herman, Anthony, Daniel, Leonard    
}

public enum FemaleName
{
    Mary, Helen, Margaret, Anna, Ruth, Elizabeth, Dorothy, Marie, Florence, Mildred, Alice, Ethel, Lillian, Gladys, Edna, Frances, Rose, Annie, Grace, Bertha, Emma, Bessie, Clara, Hazel, Irene,
    Gertrude, Louise, Catherine, Martha, Mabel, Pearl, Edith, Esther, Minnie, Myrtle, Ida, Josephine, Evelyn, Elsie, Eva, Thelma, Ruby, Agnes, Sarah, Viola, Nellie, Beatrice, Julia, Laura, Lillie
}


public class NewGamePanel : MonoBehaviour
{
    public static NewGamePanel instance { get; private set; }

    [SerializeField] private Gender selectedGender = Gender.Female;
    [SerializeField] private TMP_InputField characterName;
    [SerializeField] private GameObject skipIntro;
    
    [SerializeField] private List<Sprite> character;
    [SerializeField] private List<Sprite> hair;

    private GameData gameData;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one New Game Panel in the scene.");
            return;
        }

        instance = this;
    }

    private void Start()
    {
        RandomName();
    }

    /// <summary>
    /// Swaps the gender of the character
    /// </summary>
    public void SwapGender()
    {
        if (selectedGender == Gender.Male) 
            selectedGender = Gender.Female;
        else
            selectedGender = Gender.Male;
    }

    /// <summary>
    /// Turns the character to the left
    /// </summary>
    public void TurnLeft()
    {
        
    }

    /// <summary>
    /// Turns the character to the right
    /// </summary>
    public void TurnRight()
    {

    }

    /// <summary>
    /// Puts a random name in the input field
    /// </summary>
    public void RandomName()
    {
        string lastName = characterName.text;
        while (string.Equals(lastName, characterName.text))
        {
            if (selectedGender == Gender.Male)
            {
                MaleName test = (MaleName)UnityEngine.Random.Range(0, Enum.GetValues(typeof(MaleName)).Length);
                characterName.text = test.ToString();
            }
            else
            {
                FemaleName test = (FemaleName)UnityEngine.Random.Range(0, Enum.GetValues(typeof(FemaleName)).Length);
                characterName.text = test.ToString();
            }
        }            
    }

    public void StartYourAdventure()
    {
        // Create new gamedata and playerdata objects
        GameData gameData = new GameData();
        PlayerData playerData = new PlayerData();

        //Creates a new game with the gameData
        DataPersistenceManager.Instance.NewGame(gameData);
        //PlayerDataManager.Instance.AddNewPlayer(playerData);

        //Skip or play the intro
        if (skipIntro)
        {
            //Skip the intro
            //LoadSceneManager.Instance.LoadScene(1);
        }
        else
        {
            //Play the intro
            //LoadSceneManager.Instance.LoadScene(1);
        }

        
    }
}
