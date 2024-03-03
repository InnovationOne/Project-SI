using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
//This script is for a detailed view in the end of day screen
//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

public class DetailPrefab : MonoBehaviour
{
    public string test;

    [SerializeField] private TextMeshProUGUI name_rarity_count;
    [SerializeField] private List<TextMeshProUGUI> money_Text;
    private Color newGrey = new Color(0.66f, 0.66f, 0.66f, 0.9f); //0 money color

    public DetailPrefab CreateNew(string name, string rare, int count, int money)
    {
        if (rare == "") name_rarity_count.text = $"{name}\nx{count}";
        else name_rarity_count.text = $"{name}\n{rare}\nx{count}";
        UpdateMoney(money);

        return this;
    }

    public void UpdateMoney(int money)
    {
        List<int> listOfInt = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //Array of split int of money
        string moneyString = money.ToString("0000000000"); //Converts money to a string with 10 digits
        int x = 0;

        foreach (char c in moneyString) //Packs each digit into an list of ints
        {
            listOfInt[x] = c - '0';
            x++;
        }
        bool countOfGrey = false;
        for (int i = 0; i < listOfInt.Count; i++)
        {
            if (listOfInt[i] == 0 && countOfGrey == false) money_Text[i].color = newGrey; //When the digit is a 0 and no digit higher than 0 is shown, Color the text grey
            else //When the digit is higher then 0 or a higher digit was shown
            {
                money_Text[i].color = Color.white; //Color the text white
                countOfGrey = true;
            }
            money_Text[i].text = listOfInt[i].ToString(); //print the text on screen
        }
    }
}
