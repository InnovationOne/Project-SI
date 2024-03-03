using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

// ExcelPlantImporterEditor is a custom editor UI for the ExcelPlantImporter class
[CustomEditor(typeof(ExcelPlantImporter)), CanEditMultipleObjects]
public class ExcelPlantImporterEditor : Editor {
    public override void OnInspectorGUI() {
        GUILayout.Label("Automated!", EditorStyles.boldLabel);
        if (GUILayout.Button("Create, Fruits, Crops and Seeds")) {
            if (Application.isPlaying) {
                ExcelPlantImporter.Instance.ReadFile();
            } else {
                Debug.LogError("You can only create new fruits, crops and seeds while in play mode.");
            }
        }

        DrawDefaultInspector();
    }
}


// Imports plants from an excel file
public class ExcelPlantImporter : MonoBehaviour {
    public static ExcelPlantImporter Instance { get; private set; }

    [Header("CSV file to read")]
    [SerializeField] private TextAsset _csvFile;

    [Header("Seed and fruit params")]
    [SerializeField] private ToolActionSO _seedToolAction;
    [SerializeField] private ItemSO _itemSOTemplate;
    [SerializeField] private ItemDatabaseSO _itemDatabase;

    [Header("Crop params")]
    [SerializeField] private CropSO _cropSOTemplate;
    [SerializeField] private CropDatabase _cropDatabase;


    private const char CSV_FILE_DIVIDER = ';';
    private const string SEEDS_SO_FOLDER_PATH = "Assets/ScriptableObjects/Fruit/Seeds/";
    private const string SEEDS_SPRITE_FOLDER_PATH = "Assets/Sprites/Fruit/Seeds/";
    private const string FRUITS_SO_FOLDER_PATH = "Assets/ScriptableObjects/Fruit/Fruits/";
    private const string FRUITS_SPRITE_FOLDER_PATH = "Assets/Sprites/Fruit/Fruits/";
    private const string CROPS_SO_FOLDER_PATH = "Assets/Resources/Crops/CropSOs/";
    private const string CROPS_SPRITE_FOLDER_PATH = "Assets/Resources/Crops/Sprites/";


    // Plant params
    private class PlantEntry {
        public string NameEnglish;
        public string NameGerman;
        public string Location;
        public string FullDescriptionSeed;
        public string ItemInfoTextSeed;
        public string FunnyTextSeed;
        public int MinSeedFromSeedExtractor;
        public int MaxSeedFromSeedExtractor;
        public string FullDescriptionFruit;
        public string ItemInfoTextFruit;
        public string FunnyTextFruit;
        public int Hp;
        public int Energy;
        public int BuyPrice;
        public int SellPrice;
        public string UsedIn;
        public int FavorThroughSacrifice;
        public int Rarity;
        public int DaysToGrow;
        public bool LeavesHole;
        public bool IsHarvestedByScythe;
        public bool CanRegrow;
        public int TimeToRegrow;
        public string SeasonsToGrow;
        public int MinItemAmountToSpawn;
        public int MaxItemAmountToSpawn;
        public int ThirdStage;
        public int FourthStage;
    }

    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one ExcelToCropSO in the scene.");
        } else {
            Instance = this;
        }
    }

    public void ReadFile() {
        // Read the entire CSV file into an array
        string[] lines = File.ReadAllLines("Assets/CSVs/" + _csvFile.name + ".csv");

        // Create a list to store all data from the CSV file
        var dataEntries = new List<PlantEntry>();

        // Extract the data from each column of the CSV file (skip the first column as it contains headers)
        for (int i = 1; i < lines[0].Split(CSV_FILE_DIVIDER).Length; i++) {
            var entry = new PlantEntry();

            entry.NameEnglish = lines[0].Split(CSV_FILE_DIVIDER)[i];
            if (entry.NameEnglish == "") {
                break;
            }

            entry.NameGerman = lines[1].Split(CSV_FILE_DIVIDER)[i];
            entry.Location = lines[4].Split(CSV_FILE_DIVIDER)[i];
            entry.FullDescriptionSeed = lines[6].Split(CSV_FILE_DIVIDER)[i];
            entry.ItemInfoTextSeed = lines[7].Split(CSV_FILE_DIVIDER)[i];
            entry.FunnyTextSeed = lines[8].Split(CSV_FILE_DIVIDER)[i];
            entry.MinSeedFromSeedExtractor = int.Parse(lines[10].Split(CSV_FILE_DIVIDER)[i]);
            entry.MaxSeedFromSeedExtractor = int.Parse(lines[11].Split(CSV_FILE_DIVIDER)[i]);
            entry.FullDescriptionFruit = lines[14].Split(CSV_FILE_DIVIDER)[i];
            entry.ItemInfoTextFruit = lines[15].Split(CSV_FILE_DIVIDER)[i];
            entry.FunnyTextFruit = lines[16].Split(CSV_FILE_DIVIDER)[i];
            entry.Hp = int.Parse(lines[18].Split(CSV_FILE_DIVIDER)[i]);
            entry.Energy = int.Parse(lines[19].Split(CSV_FILE_DIVIDER)[i]);
            entry.BuyPrice = int.Parse(lines[21].Split(CSV_FILE_DIVIDER)[i]);
            entry.SellPrice = int.Parse(lines[22].Split(CSV_FILE_DIVIDER)[i]);
            entry.UsedIn = lines[24].Split(CSV_FILE_DIVIDER)[i];
            entry.FavorThroughSacrifice = int.Parse(lines[26].Split(CSV_FILE_DIVIDER)[i]);
            entry.Rarity = int.Parse(lines[29].Split(CSV_FILE_DIVIDER)[i]);
            entry.DaysToGrow = int.Parse(lines[30].Split(CSV_FILE_DIVIDER)[i]);
            entry.LeavesHole = bool.Parse(lines[31].Split(CSV_FILE_DIVIDER)[i]);
            entry.IsHarvestedByScythe = bool.Parse(lines[32].Split(CSV_FILE_DIVIDER)[i]);
            entry.CanRegrow = bool.Parse(lines[33].Split(CSV_FILE_DIVIDER)[i]);
            entry.TimeToRegrow = int.Parse(lines[34].Split(CSV_FILE_DIVIDER)[i]);
            entry.SeasonsToGrow = lines[35].Split(CSV_FILE_DIVIDER)[i];
            entry.MinItemAmountToSpawn = int.Parse(lines[37].Split(CSV_FILE_DIVIDER)[i]);
            entry.MaxItemAmountToSpawn = int.Parse(lines[38].Split(CSV_FILE_DIVIDER)[i]);
            entry.ThirdStage = int.Parse(lines[40].Split(CSV_FILE_DIVIDER)[i]);
            entry.FourthStage = int.Parse(lines[41].Split(CSV_FILE_DIVIDER)[i]);

            // Add the DataEntry instance to the list
            dataEntries.Add(entry);
        }

        // Fruit
        CheckAndDeleteItemDatabaseForFruits(dataEntries);
        DeleteFruitSOs(); // Delete old FruitSOs
        CreateFruitItemSO(dataEntries);
        Debug.Log("FruitSOs created");

        // Crop
        DeleteCropSOs(); // Delete old CropSOs
        CreateCropSO(dataEntries);
        Debug.Log("CropSOs created");

        // Seed
        CheckAndDeleteItemDatabaseForSeeds(dataEntries);
        DeleteSeedSOs(); // Delete old SeedSOs
        CreateSeedItemSO(dataEntries);
        Debug.Log("SeedSOs created");

        Debug.Log("Finished reading Fruit_CSV file");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void CheckAndDeleteItemDatabaseForFruits(List<PlantEntry> entries) {
        for (int i = 0; i < entries.Count; i++) {
            for (int j = 0; j < _itemDatabase.Items.Count; j++) {
                if (entries[i].NameEnglish == _itemDatabase.Items[j].ItemName) {
                    _itemDatabase.Items.RemoveAt(j);
                    break;
                }
            }
        }
    }

    private void CheckAndDeleteItemDatabaseForSeeds(List<PlantEntry> entries) {
        for (int i = 0; i < entries.Count; i++) {
            for (int j = 0; j < _itemDatabase.Items.Count; j++) {
                if (entries[i].NameEnglish + " Seed" == _itemDatabase.Items[j].ItemName) {
                    _itemDatabase.Items.RemoveAt(j);
                    break;
                }
            }
        }
    }

    #region Seed Params
    // Delete all SeedSOs in Seed folder
    private void DeleteSeedSOs() {
        if (Directory.Exists(SEEDS_SO_FOLDER_PATH)) {
            var files = Directory.GetFiles(SEEDS_SO_FOLDER_PATH);
            foreach (var file in files) {
                File.Delete(file);
            }
        }
        Directory.CreateDirectory(SEEDS_SO_FOLDER_PATH);
    }

    // Method for creating Scriptable Objects
    private void CreateSeedItemSO(List<PlantEntry> entries) {
        foreach (var entry in entries) {
            // Create a new ScriptableObject based on the template
            ItemSO newItemSO = Instantiate(_itemSOTemplate);

            // Set properties of the new ScriptableObject based on the data from the CSV file
            newItemSO.ItemType = ItemTypes.Seeds;
            newItemSO.WikiType = WikiTypes.PlantAndSeed;
            newItemSO.ItemName = entry.NameEnglish + " Seed";
            newItemSO.ItemIcon = AssetDatabase.LoadAssetAtPath(SEEDS_SPRITE_FOLDER_PATH + entry.NameEnglish + " Seed.png", typeof(Sprite)) as Sprite;
            newItemSO.FullDescription = entry.FullDescriptionSeed;
            newItemSO.ItemInfoText = entry.ItemInfoTextSeed;
            newItemSO.FunnyText = entry.FunnyTextSeed;
            newItemSO.CanBeMuseum = false;
            newItemSO.IsWeapon = false;
            newItemSO.IsStackable = true;
            newItemSO.MaxStackableAmount = 100;
            newItemSO.CanBeSold = true;
            newItemSO.BuyPrice = entry.BuyPrice;
            newItemSO.LowestRaritySellPrice = entry.SellPrice;
            newItemSO.CanRestoreHpOrEnergy = false;
            newItemSO.OnGridAction.Add(_seedToolAction);
            newItemSO.CropToGrow = GetItemCropToGrow(entry.NameEnglish);

            // Save the new ScriptableObject
            AssetDatabase.CreateAsset(newItemSO, SEEDS_SO_FOLDER_PATH + entry.NameEnglish + " Seed.asset");
            AssetDatabase.SaveAssets();

            _itemDatabase.Items.Add(newItemSO);

            EditorUtility.SetDirty(_itemDatabase);
        }
    }

    private CropSO GetItemCropToGrow(string nameEnglish) {
        if (Directory.Exists(CROPS_SO_FOLDER_PATH)) {
            var files = Directory.GetFiles(CROPS_SO_FOLDER_PATH);
            foreach (var file in files) {
                if (file.Contains(nameEnglish)) {
                    return AssetDatabase.LoadAssetAtPath<CropSO>(file);
                }
            }
        }

        Debug.LogError("Crop not found: " + nameEnglish);
        return null;
    }
    #endregion


    #region Fruit Params
    // Delete all FruitSOs in Fruit folder
    private void DeleteFruitSOs() {
        if (Directory.Exists(FRUITS_SO_FOLDER_PATH)) {
            var files = Directory.GetFiles(FRUITS_SO_FOLDER_PATH);
            foreach (var file in files) {
                File.Delete(file);
            }
        }
        Directory.CreateDirectory(FRUITS_SO_FOLDER_PATH);
    }

    // Method for creating Scriptable Objects
    private void CreateFruitItemSO(List<PlantEntry> entries) {
        foreach (var entry in entries) {
            // Create a new ScriptableObject based on the template
            ItemSO newItemSO = Instantiate(_itemSOTemplate);

            // Set properties of the new ScriptableObject based on the data from the CSV file
            newItemSO.ItemType = ItemTypes.Plants;
            newItemSO.WikiType = WikiTypes.PlantAndSeed;
            newItemSO.ItemName = entry.NameEnglish;
            newItemSO.ItemIcon = AssetDatabase.LoadAssetAtPath(FRUITS_SPRITE_FOLDER_PATH + entry.NameEnglish + ".png", typeof(Sprite)) as Sprite;
            newItemSO.FullDescription = entry.FullDescriptionFruit;
            newItemSO.ItemInfoText = entry.ItemInfoTextFruit;
            newItemSO.FunnyText = entry.FunnyTextFruit;
            newItemSO.CanBeMuseum = false;
            newItemSO.IsWeapon = false;
            newItemSO.IsStackable = true;
            newItemSO.MaxStackableAmount = 100;
            newItemSO.CanBeSold = true;
            newItemSO.BuyPrice = entry.BuyPrice;
            newItemSO.LowestRaritySellPrice = entry.SellPrice;
            newItemSO.CanRestoreHpOrEnergy = true;
            newItemSO.LowestRarityRestoringHpAmount = entry.Hp;
            newItemSO.LowestRarityRestoringEnergyAmount = entry.Energy;

            // Save the new ScriptableObject
            AssetDatabase.CreateAsset(newItemSO, FRUITS_SO_FOLDER_PATH + entry.NameEnglish + ".asset");
            AssetDatabase.SaveAssets();

            _itemDatabase.Items.Add(newItemSO);

            EditorUtility.SetDirty(_itemDatabase);
        }
    }
    #endregion


    #region Crop Params
    // Delete all CropSOs in Crop folder
    private void DeleteCropSOs() {
        if (Directory.Exists(CROPS_SO_FOLDER_PATH)) {
            var files = Directory.GetFiles(CROPS_SO_FOLDER_PATH);
            foreach (var file in files) {
                if (!file.Contains(_cropSOTemplate.name)) {
                    File.Delete(file);
                }
            }
        }
        Directory.CreateDirectory(CROPS_SO_FOLDER_PATH);
    }

    // Method for creating Scriptable Objects
    private void CreateCropSO(List<PlantEntry> entries) {
        foreach (var entry in entries) {
            // Create a new ScriptableObject based on the template
            CropSO newCropSO = Instantiate(_cropSOTemplate);

            string[] seasonNames = entry.SeasonsToGrow.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var seasonList = new List<TimeAndWeatherManager.SeasonName>();
            for (int i = 0; i < seasonNames.Length; i++) {
                if (Enum.TryParse(seasonNames[i].Trim(), true, out TimeAndWeatherManager.SeasonName season)) {
                    seasonList.Add(season);
                } else {
                    Debug.LogError("Could not parse season name: " + seasonNames[i]);
                    return;
                }
            }

            // Set properties of the new ScriptableObject based on the data from the CSV file
            newCropSO.CropRarity = entry.Rarity;
            newCropSO.DaysToGrow = entry.DaysToGrow;
            newCropSO.ItemToGrowAndSpawn = GetItemGrowAndSpawn(entry.NameEnglish);
            newCropSO.LeavesHole = entry.LeavesHole;
            newCropSO.IsHarvestedByScythe = entry.IsHarvestedByScythe;
            newCropSO.CanRegrow = entry.CanRegrow;
            newCropSO.DaysToRegrow = entry.TimeToRegrow;
            newCropSO.SeasonsToGrow = seasonList;
            newCropSO.MinItemAmountToSpawn = entry.MinItemAmountToSpawn;
            newCropSO.MaxItemAmountToSpawn = entry.MaxItemAmountToSpawn;
            newCropSO.TimeGrowthStages.Add(entry.ThirdStage);
            newCropSO.TimeGrowthStages.Add(entry.FourthStage);
            newCropSO.DeadSpritesGrowthStages = GetDeadCropSprites(entry.NameEnglish);
            newCropSO.SpritesGrowthStages = GetAliveCropSprites(entry.NameEnglish);

            // Save the new ScriptableObject
            AssetDatabase.CreateAsset(newCropSO, CROPS_SO_FOLDER_PATH + entry.NameEnglish + ".asset");
            AssetDatabase.SaveAssets();
        }
    }

    private List<Sprite> GetDeadCropSprites(string nameEnglish) {
        string spriteSheetPath = CROPS_SPRITE_FOLDER_PATH + nameEnglish + " Dead.png";

        var sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath).OfType<Sprite>().ToList();
        
        var output = new List<Sprite>();
        for (int i = 0; i < sprites.Count; i++) {
            output.Add(sprites.Find(x => x.name == nameEnglish + " Dead_" + i));
        }
        
        return output;
    }

    private List<Sprite> GetAliveCropSprites(string nameEnglish) {
        string spriteSheetPath = CROPS_SPRITE_FOLDER_PATH + nameEnglish + ".png";

        var sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath).OfType<Sprite>().ToList();

        var output = new List<Sprite>();
        for (int i = 0; i < sprites.Count; i++) {
            output.Add(sprites.Find(x => x.name == nameEnglish + "_" + i));
        }

        return output;
    }

    private ItemSO GetItemGrowAndSpawn(string nameEnglish) {
        if (Directory.Exists(FRUITS_SO_FOLDER_PATH)) {
            var files = Directory.GetFiles(FRUITS_SO_FOLDER_PATH);
            foreach (var file in files) {
                if (file.Contains(nameEnglish)) {
                    return AssetDatabase.LoadAssetAtPath<ItemSO>(file);
                }
            }
        }

        Debug.LogError("Item not found: " + nameEnglish);
        return null;
    }
    #endregion
}
#endif
