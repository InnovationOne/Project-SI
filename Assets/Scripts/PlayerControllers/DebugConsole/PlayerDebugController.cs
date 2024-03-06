using System.Collections.Generic;
using UnityEngine;

public class PlayerDebugController : MonoBehaviour {
    public static PlayerDebugController Instance { get; private set; }

    public bool ShowDebugConsole { get; private set; } = false;
    private bool showHelp = false;
    private string inputString = "";

    public static DebugCommand REMOVE_ALL_CROPS;
    public static DebugCommand<int, int> TIME;
    public static DebugCommand<int> DAY;
    public static DebugCommand<int> SEASON;
    public static DebugCommand<int> YEAR;
    public static DebugCommand<int, int, int> ADD_ITEM;

    public static DebugCommand HELP;

    public List<DebugCommandBase> CommandList;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one local instance of PlayerDebugController in the scene!");
            return;
        }
        Instance = this;


        REMOVE_ALL_CROPS = new DebugCommand("remove_all_crops", "Removes all crops from the farm", "remove_all_crops", () => {
            Debug.Log("Removing all crops");
            foreach (var crop in CropsManager.Instance.CropTileContainer.CropTiles) {
                CropsManager.Instance.DestroyCropTilePlantClientRpc(crop.CropPosition);
            }
        });

        TIME = new DebugCommand<int, int>("time", "Sets the time to the given value", "time <hour> <minute>", (int hour, int minute) => {
            TimeAndWeatherManager.Instance.SetTime(hour, minute);
        });

        DAY = new DebugCommand<int>("day", "Sets the day to the given value", "day <day>", (int day) => {
            TimeAndWeatherManager.Instance.SetDay(day);
        });

        SEASON = new DebugCommand<int>("season", "Sets the season to the given value", "season <season_id>", (int seasonId) => {
            TimeAndWeatherManager.Instance.SetSeason(seasonId);
        });

        YEAR = new DebugCommand<int>("year", "Sets the year to the given value", "year <year>", (int year) => {
            TimeAndWeatherManager.Instance.SetYear(year);
        });

        ADD_ITEM = new DebugCommand<int, int, int>("add_item", "Adds an item to the inventory", "add_item <item_id> <amount> <rarity_id>", (int itemId, int amount, int rarityId) => {

            PlayerInventoryController.LocalInstance.InventoryContainer.AddItemToItemContainer(itemId, amount, rarityId, false);
        });

        HELP = new DebugCommand("help", "Shows all available commands", "help", () => {
            showHelp = true;
        });

        CommandList = new List<DebugCommandBase> {
            REMOVE_ALL_CROPS,
            TIME,
            DAY,
            SEASON,
            YEAR,
            ADD_ITEM,
            HELP,
        };
    }

    private void Start() {
        InputManager.Instance.OnDebugConsoleAction += ToggleDebugConsole;
        InputManager.Instance.OnEnterAction += OnEnter;
        InputManager.Instance.OnEscapeAction += OnEscape;
    }

    private void ToggleDebugConsole() {
        ShowDebugConsole = !ShowDebugConsole;
    }

    private void OnEscape() {
        ShowDebugConsole = false;
    }

    public void OnEnter() {
        if (ShowDebugConsole) {
            HandleInput();
            inputString = "";
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
                    DebugCommandBase command = CommandList[i] as DebugCommandBase;
                    string label = $"{command.CommandFormat} - {command.CommandDescription}";
                    Rect labelRect = new Rect(5, 20 * i, viewPort.width - 100, 20);
                    GUI.Label(labelRect, label);
                }

                GUI.EndScrollView();

                y += 100;
            }

            GUI.Box(new Rect(0, y, Screen.width, 30), "");
            GUI.backgroundColor = new Color(0, 0, 0, 100);

            string textFieldName = "InputTextField";
            GUI.SetNextControlName(textFieldName);
            inputString = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20, 20), inputString);
            if (GUI.GetNameOfFocusedControl() == string.Empty) {
                GUI.FocusControl(textFieldName);
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
                    Debug.Log("Invoking command2");
                    command2.Invoke(int.Parse(properties[1]));
                }
            } else if (CommandList[i] is DebugCommand<int, int> command3) {
                if (inputString.Contains(command3.CommandId)) {
                    command3.Invoke(int.Parse(properties[1]), int.Parse(properties[2]));
                }
            }
            else if (CommandList[i] is DebugCommand<int, int, int> command4) {
                if (inputString.Contains(command4.CommandId)) {
                    command4.Invoke(int.Parse(properties[1]), int.Parse(properties[2]), int.Parse(properties[3]));
                }
            }
        }

    }
}
