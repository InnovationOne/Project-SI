using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CheatUI : MonoBehaviour {
    private int _commandHistoryIndex = 0;
    private List<string> _commandHistory;
    private Dictionary<string, DebugCommandBase> _commandDictionary;

    [SerializeField] private Transform _content;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TextMeshProUGUI _textPrefab;
    [SerializeField] private Button _buttonPrefab;
    [SerializeField] private Transform _autocomplete;


    [Header("Crops Manager")]
    public static DebugCommand REMOVE_ALL_CROPS;

    [Header("Finanz Manager")]
    public static DebugCommand<int> ADD_FARM_MONEY;
    public static DebugCommand<int> REMOVE_FARM_MONEY;
    public static DebugCommand<int> ADD_TOWN_MONEY;
    public static DebugCommand<int> REMOVE_TOWN_MONEY;

    [Header("Item Manager")]
    public static DebugCommand<int, int, int> ADD_ITEM;
    public static DebugCommand<int, int, int> REMOVE_ITEM;
    public static DebugCommand REMOVE_ALL_ITEMS;

    [Header("Time and Weather Manager")]
    public static DebugCommand START_NEXT_DAY;
    public static DebugCommand<int, int> SET_TIME;
    public static DebugCommand<int> SET_DAY;
    public static DebugCommand<int> SET_SEASON;
    public static DebugCommand<int> SET_YEAR;
    public static DebugCommand<int> SET_WEATHER;

    [Header("Placeable Objects Manager")]
    public static DebugCommand REMOVE_ALL_OBJECTS;

    [Header("Player Health and Energy Controller")]
    public static DebugCommand<int> SET_HP;
    public static DebugCommand<int> SET_MAX_HP;
    public static DebugCommand<int> SET_ENERGY;
    public static DebugCommand<int> SET_MAX_ENERGY;

    [Header("Toolbelt Controller")]
    public static DebugCommand<int> SET_TOOLBELT_SIZE;

    [Header("Inventory Controller")]
    public static DebugCommand<int> SET_INVENTORY_SIZE;

    public List<DebugCommandBase> CommandList;

    private void Awake() {
        _commandHistory = new List<string>();
        _commandDictionary = new Dictionary<string, DebugCommandBase>();

        // Initialize commands and populate the dictionary
        InitializeCommands();
    }

    private void InitializeCommands() {
        // Crops Manager
        REMOVE_ALL_CROPS = new DebugCommand("remove_all_crops", "Removes all crops from the farm.", "remove_all_crops", () => {
            GameManager.Instance.CropsManager.CropTiles.Clear();
        });
        _commandDictionary.Add(REMOVE_ALL_CROPS.CommandId, REMOVE_ALL_CROPS);

        // Finanz Manager
        ADD_FARM_MONEY = new DebugCommand<int>("add_farm_money", "Adds money to the farm's account.", "add_farm_money <amount>", (int amount) => {
            GameManager.Instance.FinanceManager.AddMoneyServerRpc(amount, true);
        });
        _commandDictionary.Add(ADD_FARM_MONEY.CommandId, ADD_FARM_MONEY);

        REMOVE_FARM_MONEY = new DebugCommand<int>("remove_farm_money", "Removes money from the town's account.", "remove_farm_money <amount>", (int amount) => {
            GameManager.Instance.FinanceManager.RemoveMoneyServerRpc(amount, true);
        });
        _commandDictionary.Add(REMOVE_FARM_MONEY.CommandId, REMOVE_FARM_MONEY);

        ADD_TOWN_MONEY = new DebugCommand<int>("add_town_money", "Adds money to the farm's account.", "add_town_money <amount>", (int amount) => {
            GameManager.Instance.FinanceManager.AddMoneyServerRpc(amount, false);
        });
        _commandDictionary.Add(ADD_TOWN_MONEY.CommandId, ADD_TOWN_MONEY);

        REMOVE_TOWN_MONEY = new DebugCommand<int>("remove_town_money", "Removes money from the town's account.", "remove_town_money <amount>", (int amount) => {
            GameManager.Instance.FinanceManager.RemoveMoneyServerRpc(amount, false);
        });
        _commandDictionary.Add(REMOVE_TOWN_MONEY.CommandId, REMOVE_TOWN_MONEY);

        // Item Manager
        ADD_ITEM = new DebugCommand<int, int, int>("add_item", "Adds an item to the inventory.", "add_item <item_id> <amount> <rarity_id>", (int itemId, int amount, int rarityId) => {
            PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(new ItemSlot(itemId, amount, rarityId), false);
        });
        _commandDictionary.Add(ADD_ITEM.CommandId, ADD_ITEM);

        REMOVE_ITEM = new DebugCommand<int, int, int>("remove_item", "Removes an item from the inventory.", "remove_item <item_id> <amount> <rarity_id>", (int itemId, int amount, int rarityId) => {
            PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.RemoveItem(new ItemSlot(itemId, amount, rarityId));
        });
        _commandDictionary.Add(REMOVE_ITEM.CommandId, REMOVE_ITEM);

        REMOVE_ALL_ITEMS = new DebugCommand("remove_all_items", "Removes all items from the inventory.", "remove_all_items", () => {
            PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.ClearItemContainer();
        });
        _commandDictionary.Add(REMOVE_ALL_ITEMS.CommandId, REMOVE_ALL_ITEMS);

        // Time and Weather Manager
        START_NEXT_DAY = new DebugCommand("start_next_day", "Starts the next day.", "start_next_day", () => {
            GameManager.Instance.TimeManager.CheatStartNextDayServerRpc();
        });
        _commandDictionary.Add(START_NEXT_DAY.CommandId, START_NEXT_DAY);

        SET_TIME = new DebugCommand<int, int>("set_time", "Sets the time to given hour and minute values.", "set_time <hour> <minute>", (int hour, int minute) => {
            GameManager.Instance.TimeManager.CheatSetTimeServerRpc(hour, minute);
        });
        _commandDictionary.Add(SET_TIME.CommandId, SET_TIME);

        SET_DAY = new DebugCommand<int>("set_day", "Sets the day to the given value.", "set_day <day>", (int day) => {
            GameManager.Instance.TimeManager.CheatSetDayServerRpc(day);
        });
        _commandDictionary.Add(SET_DAY.CommandId, SET_DAY);

        SET_SEASON = new DebugCommand<int>("set_season", "Sets the season to the given value.", "set_season <season_id>", (int seasonId) => {
            GameManager.Instance.TimeManager.CheatSetSeasonServerRpc(seasonId);
        });
        _commandDictionary.Add(SET_SEASON.CommandId, SET_SEASON);

        SET_YEAR = new DebugCommand<int>("set_year", "Sets the year to the given value.", "set_year <year>", (int year) => {
            GameManager.Instance.TimeManager.CheatSetYearServerRpc(year);
        });
        _commandDictionary.Add(SET_YEAR.CommandId, SET_YEAR);

        SET_WEATHER = new DebugCommand<int>("set_weather", "Sets the weather to the given value.", "set_weather <weather_id>", (int weatherId) => {
            GameManager.Instance.WeatherManager.CheatSetWeatherServerRpc(weatherId);
        });
        _commandDictionary.Add(SET_WEATHER.CommandId, SET_WEATHER);

        // Placeable Objects Manager
        REMOVE_ALL_OBJECTS = new DebugCommand("remove_all_objects", "Removes all placeable objects from the farm.", "remove_all_objects", () => {
            PlaceableObjectsManager.Instance.Cheat_RemoveAllObjects();
        });
        _commandDictionary.Add(REMOVE_ALL_OBJECTS.CommandId, REMOVE_ALL_OBJECTS);

        // Player Health and Energy Controller
        SET_HP = new DebugCommand<int>("set_hp", "Sets the player's health to the given value.", "set_hp <hp>", (int hp) => {
            PlayerController.LocalInstance.PlayerHealthAndEnergyController.AdjustHealth(hp);
        });
        _commandDictionary.Add(SET_HP.CommandId, SET_HP);

        SET_MAX_HP = new DebugCommand<int>("set_max_hp", "Sets the player's max health to the given value.", "set_max_hp <max_hp>", (int maxHp) => {
            PlayerController.LocalInstance.PlayerHealthAndEnergyController.AdjustMaxHealth(maxHp);
        });
        _commandDictionary.Add(SET_MAX_HP.CommandId, SET_MAX_HP);

        SET_ENERGY = new DebugCommand<int>("set_energy", "Sets the player's energy to the given value.", "set_energy <energy>", (int energy) => {
            PlayerController.LocalInstance.PlayerHealthAndEnergyController.AdjustEnergy(energy);
        });
        _commandDictionary.Add(SET_ENERGY.CommandId, SET_ENERGY);

        SET_MAX_ENERGY = new DebugCommand<int>("set_max_energy", "Sets the player's max energy to the given value.", "set_max_energy <max_energy>", (int maxEnergy) => {
            PlayerController.LocalInstance.PlayerHealthAndEnergyController.AdjustMaxEnergy(maxEnergy);
        });
        _commandDictionary.Add(SET_MAX_ENERGY.CommandId, SET_MAX_ENERGY);

        // Toolbelt Controller
        SET_TOOLBELT_SIZE = new DebugCommand<int>("set_toolbelt_size", "Sets the size of the player's toolbelt to the given value.", "set_toolbelt_size <size_Id>", (int size_Id) => {
            PlayerController.LocalInstance.PlayerToolbeltController.SetToolbeltSize(size_Id);
        });
        _commandDictionary.Add(SET_TOOLBELT_SIZE.CommandId, SET_TOOLBELT_SIZE);

        // Inventory Controller
        SET_INVENTORY_SIZE = new DebugCommand<int>("set_inventory_size", "Sets the size of the player's inventory to the given value.", "set_inventory_size <size_Id>", (int size_Id) => {
            PlayerController.LocalInstance.PlayerInventoryController.SetInventorySize(size_Id);
        });
        _commandDictionary.Add(SET_INVENTORY_SIZE.CommandId, SET_INVENTORY_SIZE);

        CommandList = new List<DebugCommandBase>(_commandDictionary.Values);
    }

    private void Start() {
        GameManager.Instance.InputManager.DebugConsole_OnEnterAction += OnEnter;
        GameManager.Instance.InputManager.DebugConsole_OnArrowUpAction += OnArrowUp;
        GameManager.Instance.InputManager.DebugConsole_OnArrowDownAction += OnArrowDown;
        _inputField.onValueChanged.AddListener(UpdateAutoCompleteSuggestions);
        ShowHelp();
    }

    private void OnDestroy() {
        GameManager.Instance.InputManager.DebugConsole_OnEnterAction -= OnEnter;
        GameManager.Instance.InputManager.DebugConsole_OnArrowUpAction -= OnArrowUp;
        GameManager.Instance.InputManager.DebugConsole_OnArrowDownAction -= OnArrowDown;
        _inputField.onValueChanged.RemoveListener(UpdateAutoCompleteSuggestions);
    }

    /// <summary>
    /// Handles the logic when the user presses the enter key in the input field.
    /// </summary>
    private void OnEnter() {
        HandleInput();

        // Don't add the same command twice in a row
        if (_commandHistory.Count == 0 || !_inputField.text.Equals(_commandHistory[^1])) {
            _commandHistory.Add(_inputField.text);
            // Update index to point to the next "new" command location
            _commandHistoryIndex = _commandHistory.Count;
        }

        _inputField.text = "";
    }

    /// <summary>
    /// Handles the event when the up arrow key is pressed.
    /// Retrieves the previous command from the command history and updates the input field.
    /// </summary>
    private void OnArrowUp() {
        if (_commandHistory.Count > 0 && _commandHistoryIndex > 0) {
            _commandHistoryIndex--;
            _inputField.text = _commandHistory[_commandHistoryIndex];
        }
    }

    /// <summary>
    /// Handles the event when the arrow down key is pressed.
    /// </summary>
    private void OnArrowDown() {
        if (_commandHistory.Count > 0 && _commandHistoryIndex < _commandHistory.Count - 1) {
            _commandHistoryIndex++;
            _inputField.text = _commandHistory[_commandHistoryIndex];
        } else if (_commandHistoryIndex == _commandHistory.Count - 1) {
            // Allow the user to return to a blank input after the last command
            _commandHistoryIndex++;
            _inputField.text = "";
        }
    }

    /// <summary>
    /// Displays the help information for all available commands.
    /// </summary>
    private void ShowHelp() {
        foreach (var command in CommandList) {
            string text = $"{command.CommandFormat} - {command.CommandDescription}";
            var instance = Instantiate(_textPrefab, _content);
            instance.text = text;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content as RectTransform);
    }

    /// <summary>
    /// Updates the autocomplete suggestions based on the input string.
    /// </summary>
    /// <param name="input">The input string to match against the command formats.</param>
    private void UpdateAutoCompleteSuggestions(string input) {
        foreach (Transform child in _autocomplete) {
            child.gameObject.SetActive(false);
        }

        input = input.ToLower();

        if (!string.IsNullOrEmpty(input)) {
            foreach (var command in CommandList) {
                if (command.CommandFormat.StartsWith(input)) {
                    var instance = Instantiate(_buttonPrefab, _autocomplete);
                    instance.GetComponentInChildren<TextMeshProUGUI>().text = command.CommandFormat;
                    var button = instance.GetComponent<Button>();
                    button.onClick.AddListener(() => OnSuggestionClicked(command.CommandFormat));
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_autocomplete as RectTransform);
        }
    }

    /// <summary>
    /// Sets the text of the input field to the given suggestion and activates the input field.
    /// </summary>
    /// <param name="suggestion">The suggestion to set as the text of the input field.</param>
    private void OnSuggestionClicked(string suggestion) {
        _inputField.text = suggestion;
        _inputField.ActivateInputField();
    }

    /// <summary>
    /// Handles the input entered in the cheat console.
    /// </summary>
    private void HandleInput() {
        string[] properties = _inputField.text.Split(' ');

        if (_commandDictionary.TryGetValue(properties[0], out var command)) {
            switch (command) {
                case DebugCommand debugCommand:
                    debugCommand.Invoke();
                    break;
                case DebugCommand<int> debugCommandInt:
                    debugCommandInt.Invoke(int.Parse(properties[1]));
                    break;
                case DebugCommand<int, int> debugCommandIntInt:
                    debugCommandIntInt.Invoke(int.Parse(properties[1]), int.Parse(properties[2]));
                    break;
                case DebugCommand<int, int, int> debugCommandIntIntInt:
                    debugCommandIntIntInt.Invoke(int.Parse(properties[1]), int.Parse(properties[2]), int.Parse(properties[3]));
                    break;
            }
        }
    }
}
