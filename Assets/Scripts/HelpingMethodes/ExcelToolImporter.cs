using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


#if UNITY_EDITOR
// ExcelToolImporterEditor is a custom editor UI for the ExcelToolImporter class
[CustomEditor(typeof(ExcelToolImporter)), CanEditMultipleObjects]
public class ExcelToolImporterEditor : Editor {
    public override void OnInspectorGUI() {
        GUILayout.Label("Automated!", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Tools")) {
            if (Application.isPlaying) {
                ExcelToolImporter.Instance.ReadFile();
            } else {
                Debug.LogError("You can only create new tools while in play mode.");
            }
        }

        DrawDefaultInspector();
    }
}


public class ExcelToolImporter : MonoBehaviour {
    public static ExcelToolImporter Instance { get; private set; }

    [Header("CSV file to read")]
    [SerializeField] private TextAsset _csvFile;

    [Header("Tool params")]
    [SerializeField] private ItemSO _itemSOTemplate;
    [SerializeField] private ItemDatabaseSO _itemDatabase;
    [SerializeField] private ToolActionSO _pickUp;

    private const char CSV_FILE_DIVIDER = ';';
    private const string STRING_DIVIDER = ", ";
    private const string SIZE_DIVIDER = "x";
    private const string TOOLS_SO_FOLDER_PATH = "Assets/ScriptableObjects/Tools/";
    private const string TOOLS_SPRITE_FOLDER_PATH = "Assets/Sprites/Tools/";
    private const string TOOLACTION_SO_FOLDER_PATH = "Assets/ScriptableObjects/ToolActions/";

    // Tool params
    private class ToolEntry {
        public string NameEnglish;
        public string NameGerman;
        public string Location;
        public string FullDescription;
        public string ItemInfoText;
        public string FunnyText;
        public string Wood;
        public string Stone;
        public string Copper;
        public string Iron;
        public string Gold;
        public string Diamond;
    }


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one ExcelToolImporter in the scene.");
        } else {
            Instance = this;
        }
    }

    public void ReadFile() {
        // Read the entire CSV file into an array
        string[] lines = File.ReadAllLines("Assets/CSVs/" + _csvFile.name + ".csv");

        // Create a list to store all data from the CSV file
        var dataEntries = new List<ToolEntry>();

        // Extract the data from each column of the CSV file (skip the first column as it contains headers)
        for (int i = 1; i < lines[0].Split(CSV_FILE_DIVIDER).Length; i++) {
            var entry = new ToolEntry();

            entry.NameEnglish = lines[0].Split(CSV_FILE_DIVIDER)[i];
            if (entry.NameEnglish == "") {
                break;
            }

            entry.NameGerman = lines[1].Split(CSV_FILE_DIVIDER)[i];
            entry.Location = lines[3].Split(CSV_FILE_DIVIDER)[i];
            entry.FullDescription = lines[5].Split(CSV_FILE_DIVIDER)[i];
            entry.ItemInfoText = lines[6].Split(CSV_FILE_DIVIDER)[i];
            entry.FunnyText = lines[7].Split(CSV_FILE_DIVIDER)[i];
            entry.Wood = lines[9].Split(CSV_FILE_DIVIDER)[i];
            entry.Stone = lines[10].Split(CSV_FILE_DIVIDER)[i];
            entry.Copper = lines[11].Split(CSV_FILE_DIVIDER)[i];
            entry.Iron = lines[12].Split(CSV_FILE_DIVIDER)[i];
            entry.Gold = lines[13].Split(CSV_FILE_DIVIDER)[i];
            entry.Diamond = lines[14].Split(CSV_FILE_DIVIDER)[i];

            // Add the DataEntry instance to the list
            dataEntries.Add(entry);
        }

        // Tools
        CheckAndDeleteItemDatabaseForTool(dataEntries);
        DeleteTools(); // Delete old Tools
        CreateToolItemSO(dataEntries);
    }

    private void CheckAndDeleteItemDatabaseForTool(List<ToolEntry> entries) {
        for (int i = 0; i < entries.Count; i++) {
            for (int j = 0; j < _itemDatabase.Items.Count; j++) {
                if (entries[i].NameEnglish == _itemDatabase.Items[j].ItemName) {
                    _itemDatabase.Items.RemoveAt(j);
                    break;
                }
            }
        }
    }

    // Delete all Tools in Tools folder
    private void DeleteTools() {
        if (Directory.Exists(TOOLS_SO_FOLDER_PATH)) {
            var files = Directory.GetFiles(TOOLS_SO_FOLDER_PATH);
            foreach (var file in files) {
                if (!file.Contains(_itemSOTemplate.name)) {
                    File.Delete(file);
                }
            }
        }
        Directory.CreateDirectory(TOOLS_SO_FOLDER_PATH);
    }



    // Method for creating Scriptable Objects
    private void CreateToolItemSO(List<ToolEntry> entries) {
        foreach (var entry in entries) {
            // Create a new ScriptableObject based on the template
            ItemSO newItemSO = Instantiate(_itemSOTemplate);

            // Set properties of the new ScriptableObject based on the data from the CSV file
            newItemSO.ItemType = ItemTypes.Tools;
            newItemSO.WikiType = WikiTypes.ToolAndCraft;
            newItemSO.ItemName = entry.NameEnglish;
            newItemSO.FullDescription = entry.FullDescription;
            newItemSO.ItemInfoText = entry.ItemInfoText;
            newItemSO.FunnyText = entry.FunnyText;
            newItemSO.CanBeMuseum = false;
            newItemSO.IsWeapon = false;
            newItemSO.IsStackable = false;
            newItemSO.CanBeSold = false;
            newItemSO.CanRestoreHpOrEnergy = false;
            newItemSO.ItemIcon = AssetDatabase.LoadAssetAtPath<Sprite>(TOOLS_SPRITE_FOLDER_PATH + "Tool.png");
            newItemSO.ToolItemRarity = GetToolRaritySprite(newItemSO.ItemName);

            if (newItemSO.ItemName == "Axe" || newItemSO.ItemName == "Pickaxe" || newItemSO.ItemName == "Scythe" || newItemSO.ItemName == "Watering Can") {
                newItemSO.OnGridAction.Add(GetToolAction(newItemSO.ItemName));

                if (newItemSO.ItemName == "Axe" || newItemSO.ItemName == "Pickaxe") {
                    newItemSO.OnGridAction.Add(_pickUp);
                }

                newItemSO.UsageOrDamageOnAction = new List<int>();
                newItemSO.EnergyOnAction = new List<int>();

                string[] toolRaritys = { entry.Wood, entry.Stone, entry.Copper, entry.Iron, entry.Gold, entry.Diamond };
                foreach (string toolRarity in toolRaritys) {
                    string[] parts = toolRarity.Split(STRING_DIVIDER);
                    if (parts[0] == "") {   // This rarity is not available for this tool
                        continue;
                    }
                    newItemSO.UsageOrDamageOnAction.Add(int.Parse(parts[0]));
                    newItemSO.EnergyOnAction.Add(int.Parse(parts[1]));
                    if (!string.IsNullOrEmpty(parts[2])) {
                        newItemSO.VolumeOrBiteRate.Add(int.Parse(parts[2]));
                    }
                }

            } else if (newItemSO.ItemName == "Fishing Rod") {
                Debug.Log("Fishing Rod");

            } else if (newItemSO.ItemName == "Hoe") {
                newItemSO.OnGridAction.Add(GetToolAction(newItemSO.ItemName));

                string[] toolRaritys = { entry.Wood, entry.Stone, entry.Copper, entry.Iron, entry.Gold, entry.Diamond };
                foreach (string toolRarity in toolRaritys) {
                    newItemSO.UsageOrDamageOnAction.Add(
                        int.Parse(toolRarity.Split(STRING_DIVIDER)[0].Split(SIZE_DIVIDER)[0]) * 10 +
                        int.Parse(toolRarity.Split(STRING_DIVIDER)[0].Split(SIZE_DIVIDER)[1]));
                    newItemSO.EnergyOnAction.Add(int.Parse(toolRarity.Split(STRING_DIVIDER)[1]));
                }

            } else if (newItemSO.ItemName == "Milking Bucket") {
                Debug.Log("Milking Bucket");

            } else if (newItemSO.ItemName == "Shears") {
                Debug.Log("Shears");
            }



            // Save the new ScriptableObject
            AssetDatabase.CreateAsset(newItemSO, TOOLS_SO_FOLDER_PATH + entry.NameEnglish + ".asset");
            AssetDatabase.SaveAssets();

            _itemDatabase.Items.Add(newItemSO);

            EditorUtility.SetDirty(_itemDatabase);
        }
    }

    private List<Sprite> GetToolRaritySprite(string nameEnglish) {
        string spriteSheetPath = TOOLS_SPRITE_FOLDER_PATH + nameEnglish + ".png";

        var sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath).OfType<Sprite>().ToList();
        string[] rarities = { "Wood", "Stone", "Copper", "Iron", "Gold", "Diamond" };

        var output = new List<Sprite> {
            sprites.Find(x => x.name == nameEnglish + " Wood"),
            sprites.Find(x => x.name == nameEnglish + " Stone"),
            sprites.Find(x => x.name == nameEnglish + " Copper"),
            sprites.Find(x => x.name == nameEnglish + " Iron"),
            sprites.Find(x => x.name == nameEnglish + " Gold"),
            sprites.Find(x => x.name == nameEnglish + " Diamond")
        };

        return output;
    }

    private ToolActionSO GetToolAction(string nameEnglish) {
        if (Directory.Exists(TOOLACTION_SO_FOLDER_PATH)) {
            var files = Directory.GetFiles(TOOLACTION_SO_FOLDER_PATH);
            foreach (var file in files) {
                if (file.Contains(nameEnglish)) {
                    return AssetDatabase.LoadAssetAtPath<ToolActionSO>(file);
                }
            }
        }

        Debug.LogError("ToolAction not found: " + nameEnglish);
        return null;
    }
}

#endif