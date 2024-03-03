using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;

//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
//This script controlles the end of day screen
//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

public class EndOfDayController : MonoBehaviour
{
    public ItemContainerSO soldItemContainer; //Referenze to the sold item container
    public GameObject prefab; //The detail prefab

    [SerializeField] private TextMeshProUGUI date; //Date
    [SerializeField] private List<GameObject> buttons; //List of the buttons
    [SerializeField] private List<Image> itemImages; //Images of the items
    [SerializeField] private List<Image> shadowImages; //Images of the shadows

    [SerializeField] private List<TextMeshProUGUI> farm_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> small_Farm_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> collectibles_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> small_Collectibles_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> fish_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> small_Fish_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> insects_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> small_Insects_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> minerals_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> small_Minerals_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> others_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> small_Others_Money_text; //Money text
    [SerializeField] private List<TextMeshProUGUI> total_Money_text; //Money text

    private List<List<ItemSlot>> itemLists = new List<List<ItemSlot>>(); //Breaken down list of items in the send list
    private List<int> money = new List<int>() { 0, 0, 0, 0, 0, 0, 0 }; //Money of the categories
    private Color newGrey = new Color(0.66f, 0.66f, 0.66f, 0.9f); //0 money color
    private Color none = new Color(1f, 1f, 1f, 0f); //0 money color
    private Color show = new Color(1f, 1f, 1f, 1f); //0 money color

    /// <summary>
    /// Gets called when the day ends or the player goes to sleep
    /// </summary>
    public void ShowPanel()
    {
        gameObject.SetActive(true); //Activate the game object
        //date.text = GameManager.instance.dayTimeController.EndOfDayDate(); //Write the date

        foreach (GameObject gameObject in buttons)
        {
            gameObject.transform.GetChild(0).gameObject.SetActive(true); //Shows all small
            gameObject.transform.GetChild(1).gameObject.SetActive(false); //Hides all extended
        }

        Sort(); //Sorts out the send list into the categories
        GetRandomImage(); //Gets a random image
        UpdateMoneyText(); //Update the money text
    } 

    /// <summary>
    /// Splits the send list into smaller lists of the categories
    /// </summary>
    private void Sort()
    {
        List<string> names = new List<string>(); //Empty list of strings to be filled with the item names currently in the inventory
        List<ItemSO> slotItemCopy = new List<ItemSO>(); //Copy of list of item slots
        List<int> slotCountCopy = new List<int>(); //Copy of list of item slots
        List<ItemSlot> sendList = new List<ItemSlot>(); //New item list

        foreach (ItemSlot slot in soldItemContainer.ItemSlots) //Copies the item names, items and counts to a new list
        {
            if (slot.Item != null)
            {
                names.Add(slot.Item.ItemName);
                slotItemCopy.Add(slot.Item);
                slotCountCopy.Add(slot.Amount);
                slot.Clear();
            }
        }

        names = names.Distinct().ToList(); //Delete duplicates
        names.Sort(); //Sort the list


        for (int i = 0; i < names.Count; i++) //Goes throught the names list
        {
            for (int q = 3; q >= 0; q--) //Goes throught the rarity index
            {
                for (int j = 0; j < slotItemCopy.Count; j++) //Goes throught the item list
                {
                    if (slotItemCopy[j].ItemName == names[i]) //Checks if the name is correct and for the rarity index
                    {
                        if (!slotItemCopy[j].IsStackable) slotCountCopy[j] = 1;

                        for (int r = 0; r < soldItemContainer.ItemSlots.Count; r++)
                        {
                            if (soldItemContainer.ItemSlots[r].Item == slotItemCopy[j]) //If the slot item is equal to the added item and the slot item is not fully stacked
                            {
                                soldItemContainer.ItemSlots[r].Amount += slotCountCopy[j];
                                slotCountCopy[j] = 0;
                            }
                        }

                        if (slotCountCopy[j] > 0) //If the count ist greater then 0 after all slots with the same item are filled up
                        {
                            ItemSlot itemSlot = soldItemContainer.ItemSlots.Find(x => x.Item == null); //Look for an empty slot
                            if (itemSlot != null) //If there is an empty slot
                            {
                                itemSlot.Item = slotItemCopy[j]; //Add the item to the slot
                                itemSlot.Amount = slotCountCopy[j]; //Add the count to the slot
                            }
                        }
                    }
                }
            }
        }

        //Adds the items to the different categories
        for (int x = 0; x < 6; x++)
        {
            itemLists.Add(new List<ItemSlot>()); //Creates all the category lists
            foreach (ItemSlot slot in soldItemContainer.ItemSlots)
            {
                
                if (slot.Item != null)
                {
                    itemLists[x].Add(slot);
                    money[x] += slot.Item.LowestRaritySellPrice * slot.Amount;
                }
            }
        }

        //Adds the detailed prefab for every item in the different category lists
        for (int x = 0; x < 6; x++)
        {
            foreach (ItemSlot slot in itemLists[x])
            {
                if (slot.Item.ItemName != null)
                {
                    //DetailPrefab detailPrefab = PanelManager.instance.detailPrefab.CreateNew(slot.item.itemName, slot.item.rarityName, slot.amount, slot.item.sellPrice * slot.amount);

                    GameObject gameObjectReference = Instantiate(prefab); //Instantiates the prefab

                    gameObjectReference.transform.SetParent(gameObject.transform.GetChild(6).gameObject.transform.GetChild(x).gameObject.transform.GetChild(1).gameObject.transform.GetChild(2).gameObject.transform.GetChild(0));
                    gameObjectReference.transform.localScale = new Vector3(1, 1, 1); //Scales the prefab
                }
            }
        }

        //Add all money together
        for (int i = 0; i < money.Count - 1; i++) 
        {
            money[6] += money[i]; //Total money count
        }
    }

    /// <summary>
    /// Updates all images with a random item icon
    /// </summary>
    private void GetRandomImage()
    {
        for (int i = 0; i < 6; i++)
        {
            itemImages[i].sprite = null;
            if (itemLists[i].Count > 0)
            {
                itemImages[i].sprite = itemLists[i][Random.Range(0, itemLists[i].Count - 1)].Item.ItemIcon; //Gets a random image
                itemImages[i].color = show; //Removes the random image
                shadowImages[i].gameObject.SetActive(true); //Activates the shadow
            }
            else
            {
                itemImages[i].sprite = null; //Removes the random image
                itemImages[i].color = none; //Removes the random image
                shadowImages[i].gameObject.SetActive(false); //Deactivates the shadow
            }
        }        
    }

    /// <summary>
    /// Updates the money text
    /// </summary>
    private void UpdateMoneyText()
    {
        UpdateMoney(0, farm_Money_text);
        UpdateMoney(0, small_Farm_Money_text);
        UpdateMoney(1, collectibles_Money_text);
        UpdateMoney(1, small_Collectibles_Money_text);
        UpdateMoney(2, fish_Money_text);
        UpdateMoney(2, small_Fish_Money_text);
        UpdateMoney(3, insects_Money_text);
        UpdateMoney(3, small_Insects_Money_text);
        UpdateMoney(4, minerals_Money_text);
        UpdateMoney(4, small_Minerals_Money_text);
        UpdateMoney(5, others_Money_text);
        UpdateMoney(5, small_Others_Money_text);
        UpdateMoney(6, total_Money_text);
    }

    /// <summary>
    /// Switches the two versions
    /// </summary>
    /// <param name="category">Category</param>
    public void Switch(int category)
    {
        buttons[category].transform.GetChild(0).gameObject.SetActive(!buttons[category].transform.GetChild(0).gameObject.activeSelf);
        buttons[category].transform.GetChild(1).gameObject.SetActive(!buttons[category].transform.GetChild(1).gameObject.activeSelf);
    }

    /// <summary>
    /// When the player presses on the continue button
    /// </summary>
    public void Continue()
    {
        gameObject.SetActive(false); //Hides the end of day screen
        FinanceManager.Instance.AddMoneyToFarmServerRpc(money[6]); //Adds the total to the farms money

        foreach (ItemSlot slot in soldItemContainer.ItemSlots) slot.Clear(); //Clears the sold item container

        itemLists = new List<List<ItemSlot>>(); //Breaken down list of items in the send list
        money = new List<int>() { 0, 0, 0, 0, 0, 0, 0 }; //Money of the categories
        soldItemContainer.ItemSlots = new List<ItemSlot>();

        for (int i = 0; i < 6; i++)
        {
            foreach (Transform child in gameObject.transform.GetChild(6).gameObject.transform.GetChild(i).gameObject.transform.GetChild(1).gameObject.transform.GetChild(2).gameObject.transform.GetChild(0))
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Updates the money counter
    /// </summary>
    public void UpdateMoney(int index, List<TextMeshProUGUI> money_Text)
    {
        List<int> listOfInt = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //Array of split int of money
        string moneyString = money[index].ToString("0000000000"); //Converts money to a string with 10 digits
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
