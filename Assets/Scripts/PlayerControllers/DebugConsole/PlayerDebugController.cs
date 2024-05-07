using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerDebugController : NetworkBehaviour {
    public static PlayerDebugController LocalInstance { get; private set; }

    public bool ShowDebugConsole { get; private set; } = false;
    private bool showHelp = false;
    private string inputString = "";
    private int commandHistoryIndex = 0;
    private List<string> commandHistory;

    private List<string> autoCompleteSuggestions;
    private bool showAutoComplete = false;

    public static DebugCommand REMOVE_ALL_CROPS;
    public static DebugCommand START_NEXT_DAY;
    public static DebugCommand<int, int> SET_TIME;
    public static DebugCommand<int> SET_DAY;
    public static DebugCommand<int> SET_SEASON;
    public static DebugCommand<int> SET_YEAR;
    public static DebugCommand<int> SET_WEATHER;
    public static DebugCommand<int, int, int> ADD_ITEM;

    public static DebugCommand HELP;

    public List<DebugCommandBase> CommandList;

    private void Awake() {
        commandHistory = new List<string>();
        autoCompleteSuggestions = new List<string>();


        REMOVE_ALL_CROPS = new DebugCommand("remove_all_crops", "Removes all crops from the farm", "remove_all_crops", () => {
            foreach (var crop in CropsManager.Instance.CropTileContainer.CropTileMap.Values) {
                CropsManager.Instance.DestroyCropTilePlantClientRpc(crop.CropPosition);
            }
        });

        START_NEXT_DAY = new DebugCommand("start_next_day", "Starts the next day", "start_next_day", () => {
            TimeAndWeatherManager.Instance.CheatStartNextDay();
        });

        SET_TIME = new DebugCommand<int, int>("set_time", "Sets the time to the given value", "set_time <hour> <minute>", (int hour, int minute) => {
            TimeAndWeatherManager.Instance.CheatSetTime(hour, minute);
        });

        SET_DAY = new DebugCommand<int>("set_day", "Sets the day to the given value", "set_day <day>", (int day) => {
            TimeAndWeatherManager.Instance.CheatSetDay(day);
        });

        SET_SEASON = new DebugCommand<int>("set_season", "Sets the season to the given value", "set_season <season_id>", (int seasonId) => {
            TimeAndWeatherManager.Instance.CheatSetSeason(seasonId);
        });

        SET_YEAR = new DebugCommand<int>("set_year", "Sets the year to the given value", "set_year <year>", (int year) => {
            TimeAndWeatherManager.Instance.CheatSetYear(year);
        });

        SET_WEATHER = new DebugCommand<int>("set_weather", "Sets the weather to the given value", "set_weather <weather_id>", (int weatherId) => {
            TimeAndWeatherManager.Instance.CheatSetWeather(weatherId);
        });

        ADD_ITEM = new DebugCommand<int, int, int>("add_item", "Adds an item to the inventory", "add_item <item_id> <amount> <rarity_id>", (int itemId, int amount, int rarityId) => {

            PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(new ItemSlot(itemId, amount, rarityId), false);
        });

        HELP = new DebugCommand("help", "Shows all available commands", "help", () => {
            showHelp = !showHelp;
        });

        CommandList = new List<DebugCommandBase> {
            REMOVE_ALL_CROPS,
            START_NEXT_DAY,
            SET_TIME,
            SET_DAY,
            SET_SEASON,
            SET_YEAR,
            SET_WEATHER,
            ADD_ITEM,
            HELP,
        };
    }

    private void Start() {
        InputManager.Instance.OnDebugConsoleAction += ToggleDebugConsole;
        InputManager.Instance.OnEnterAction += OnEnter;
        InputManager.Instance.OnEscapeAction += OnEscape;
        InputManager.Instance.OnArrowUpAction += OnArrowUp;
        InputManager.Instance.OnArrowDownAction += OnArrowDown;
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerDebugController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    private void ToggleDebugConsole() {
        ShowDebugConsole = !ShowDebugConsole;
    }

    private void OnEscape() {
        ShowDebugConsole = false;
    }

    private void OnEnter() {
        if (ShowDebugConsole) {
            HandleInput();

            // Don't add the same command twice in a row
            if (commandHistory.Count == 0 || !inputString.Equals(commandHistory[^1])) {
                commandHistory.Add(inputString);
                // Update index to point to the next "new" command location
                commandHistoryIndex = commandHistory.Count;
            }

            inputString = "";
        }
    }

    private void OnArrowUp() {
        if (ShowDebugConsole && commandHistory.Count > 0 && commandHistoryIndex > 0) {
            commandHistoryIndex--;
            inputString = commandHistory[commandHistoryIndex];
        }
    }

    private void OnArrowDown() {
        if (ShowDebugConsole && commandHistory.Count > 0 && commandHistoryIndex < commandHistory.Count - 1) {
            commandHistoryIndex++;
            inputString = commandHistory[commandHistoryIndex];
        } else if (ShowDebugConsole && commandHistoryIndex == commandHistory.Count - 1) {
            // Allow the user to return to a blank input after the last command
            commandHistoryIndex++;
            inputString = "";
        }
    }

    private void UpdateAutoCompleteSuggestions() {
        string input = inputString.ToLower();
        autoCompleteSuggestions.Clear();

        if (!string.IsNullOrEmpty(input)) {
            foreach (var command in CommandList) {
                if (command.CommandId.StartsWith(input)) {
                    autoCompleteSuggestions.Add(command.CommandId);
                }
            }

            showAutoComplete = autoCompleteSuggestions.Count > 0;
        } else {
            showAutoComplete = false;
        }
    }

    private Vector2 scroll;
    private void OnGUI() {
        if (ShowDebugConsole) {
            float y = 0f;

            if (showHelp) {
                GUI.Box(new Rect(0, y, Screen.width, 100), "");

                Rect viewPort = new Rect(0, 0, Screen.width - 30, 20 * CommandList.Count);
                scroll = GUI.BeginScrollView(new Rect(0, y + 5f, Screen.width, 90), scroll, viewPort);

                for (int i = 0; i < CommandList.Count; i++) {
                    DebugCommandBase command = CommandList[i];
                    string label = $"{command.CommandFormat} - {command.CommandDescription}";
                    Rect labelRect = new Rect(5, 20 * i, viewPort.width - 100, 20);
                    GUI.Label(labelRect, label);
                }

                GUI.EndScrollView();

                y += 101;
            }

            GUI.Box(new Rect(0, y, Screen.width, 30), "");
            GUI.backgroundColor = new Color(0, 0, 0, 100);

            string textFieldName = "InputTextField";
            GUI.SetNextControlName(textFieldName);
            inputString = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20, 20), inputString);
            if (GUI.GetNameOfFocusedControl() == string.Empty) {
                GUI.FocusControl(textFieldName);
            }

            y += 31;

            UpdateAutoCompleteSuggestions();
            if (showAutoComplete) {
                GUI.Box(new Rect(0, y, Screen.width, 100), "");

                Rect viewPort = new Rect(0, 0, Screen.width - 30, 20 * autoCompleteSuggestions.Count);
                scroll = GUI.BeginScrollView(new Rect(0, y + 5f, Screen.width, 90), scroll, viewPort);

                for (int i = 0; i < autoCompleteSuggestions.Count; i++) {
                    if (GUI.Button(new Rect(5, 25 * i, viewPort.width - 100, 20), autoCompleteSuggestions[i])) {
                        inputString = autoCompleteSuggestions[i];
                        showAutoComplete = false;
                    }
                }

                GUI.EndScrollView();
            }
        }
    }

    private void HandleInput() {
        string[] properties = inputString.Split(' ');

        for (int i = 0; i < CommandList.Count; i++) {
            if (CommandList[i] is DebugCommand command1) {
                if (inputString.Contains(command1.CommandId)) {
                    command1.Invoke();
                }
            } else if (CommandList[i] is DebugCommand<int> command2) {
                if (inputString.Contains(command2.CommandId)) {
                    command2.Invoke(int.Parse(properties[1]));
                }
            } else if (CommandList[i] is DebugCommand<int, int> command3) {
                if (inputString.Contains(command3.CommandId)) {
                    command3.Invoke(int.Parse(properties[1]), int.Parse(properties[2]));
                }
            } else if (CommandList[i] is DebugCommand<int, int, int> command4) {
                if (inputString.Contains(command4.CommandId)) {
                    command4.Invoke(int.Parse(properties[1]), int.Parse(properties[2]), int.Parse(properties[3]));
                }
            } else {
                Debug.LogError("Command not found");
            }
        }
    }
}
