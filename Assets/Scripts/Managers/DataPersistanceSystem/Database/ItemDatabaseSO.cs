using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// This script is for a database of items
[CreateAssetMenu(menuName = "Database/Item Database")]
public class ItemDatabaseSO : ScriptableObject {
#if UNITY_EDITOR
    /// <summary>
    /// Custom editor for ItemDatabaseSO.
    /// </summary>
    [CustomEditor(typeof(ItemDatabaseSO))]
    public class ItemDatabaseSOEditor : Editor {
        public override void OnInspectorGUI() {
            // Reference to the current ItemDatabaseSO object
            ItemDatabaseSO itemDatabase = (ItemDatabaseSO)target;

            if (GUILayout.Button("Start Verification")) {
                VerifyItemDatabase(itemDatabase);
            }

            GUILayout.Space(10); // Adds some space

            if (GUILayout.Button("Save items to text file")) {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "ItemList.txt");

                itemDatabase.InitializeItems();
                itemDatabase.SaveItemsToTextFile(filePath);
            }

            GUILayout.Space(10); // Adds some space

            // Draws the standard inspector below the custom button
            DrawDefaultInspector();
        }

        /// <summary>
        /// Initiates the item database verification process.
        /// </summary>
        /// <param name="itemDatabase">The ItemDatabaseSO instance to verify.</param>
        private void VerifyItemDatabase(ItemDatabaseSO itemDatabase) {
            const string title = "Verify Item Database";
            const string message = "Would you like to check all ItemSO in the project and add or report missing items?";
            const string option1 = "Yes, Auto Add Missing Items";
            const string option2 = "Yes, Report Missing Items";
            const string option3 = "No";

            var option = EditorUtility.DisplayDialogComplex(title, message, option1, option3, option2);

            switch (option) {
                case 0:
                    itemDatabase.VerifyAllItemsInDatabase(0);
                    break;
                case 2:
                    itemDatabase.VerifyAllItemsInDatabase(1);
                    break;
                case 1:
                    // User chose 'No', do nothing
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Converts the _items array to a HashSet for efficient lookups.
    /// </summary>
    private HashSet<ItemSO> ItemSet => new HashSet<ItemSO>(_items);

    /// <summary>
    /// Verifies all ItemSO assets in the project against the database.
    /// </summary>
    /// <param name="option">0 to auto-add missing items, 1 to report missing items.</param>
    public void VerifyAllItemsInDatabase(int option) {
        // Retrieve all ItemSO asset GUIDs
        var guids = AssetDatabase.FindAssets("t:ItemSO");
        var allItems = new List<ItemSO>(guids.Length);

        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (item != null) {
                allItems.Add(item);
            }
        }

        // Identify missing ItemSO assets
        var missingItems = allItems.Where(item => !ItemSet.Contains(item) &&
                                                 item.name != "_Template").ToList();

        if (missingItems.Count > 0) {
            switch (option) {
                case 0:
                    AddMissingItems(missingItems);
                    break;
                case 1:
                    ReportMissingItems(missingItems);
                    break;
            }
        } else {
            EditorUtility.DisplayDialog("Verification Complete", "All ItemSO are included in the database.", "OK");
            Debug.Log("All ItemSO are included in the database.");
        }
    }

    // Methode zum Speichern der ItemName und ItemId in eine Textdatei
    private void SaveItemsToTextFile(string filePath) {
        int nameColumnWidth = 30; // Definiere eine feste Breite für die Item-Namen-Spalte

        if (_items == null) {
            Debug.LogError("_items ist null. Keine Daten zum Speichern vorhanden.");
            return;
        }

        using (var writer = new StreamWriter(filePath)) {
            foreach (var item in _items) {
                if (item == null) {
                    Debug.LogWarning("Ein Element in _items ist null und wird übersprungen.");
                    continue;
                }

                string itemName = item.ItemName;
                if (string.IsNullOrEmpty(itemName)) {
                    Debug.LogWarning($"Das Item mit ID {item.ItemId} hat keinen Namen. Setze einen Standardnamen.");
                    itemName = "Unbenannt";
                }

                string formattedName = itemName.PadRight(nameColumnWidth);
                writer.WriteLine($"{formattedName}\t{item.ItemId}");
            }
        }
        Debug.Log("Item-Liste wurde auf " + filePath + " gespeichert.");
    }


    /// <summary>
    /// Adds missing ItemSO assets to the database.
    /// </summary>
    /// <param name="missingItems">List of missing ItemSO assets.</param>
    private void AddMissingItems(List<ItemSO> missingItems) {
        var updatedItemList = new List<ItemSO>(_items.Length + missingItems.Count);
        updatedItemList.AddRange(_items);
        updatedItemList.AddRange(missingItems);
        _items = updatedItemList.ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        foreach (var item in missingItems) {
            Debug.Log($"Item '{item.name}' was automatically added to the database.");
        }

        EditorUtility.DisplayDialog("Verification Complete", $"{missingItems.Count} ItemSO were added to the database.", "OK");
    }

    /// <summary>
    /// Reports missing ItemSO assets by logging their paths.
    /// </summary>
    /// <param name="missingItems">List of missing ItemSO assets.</param>
    private void ReportMissingItems(List<ItemSO> missingItems) {
        foreach (var item in missingItems) {
            var path = AssetDatabase.GetAssetPath(item);
            Debug.LogWarning($"Item '{item.name}' is not in the database and is located at: {path}");
        }

        EditorUtility.DisplayDialog("Verification Complete", $"{missingItems.Count} ItemSO are missing from the database. Check the Console for details.", "OK");
    }
#endif

    // List of items in the database
    [SerializeField] private ItemSO[] _items = Array.Empty<ItemSO>();

    // Cache to store items by their IDs for fast lookup
    private Dictionary<int, ItemSO> _cache = new();

    /// <summary>
    /// Initializes the items in the items database on Start() and cache all items.
    /// </summary>
    public void InitializeItems() {
        for (int i = 0; i < _items.Length; i++) {
            _items[i].ItemId = i;
            _cache[i] = _items[i]; // Populate the cache
        }
    }

    /// <summary>
    /// Indexer to access items by their IDs from the cache
    /// </summary>
    public ItemSO this[int itemId] {
        get {
            if (_cache.TryGetValue(itemId, out var item)) {
                return item;
            } else if (itemId == -1) {
                return null;
            } else {
                throw new KeyNotFoundException($"Item with ID {itemId} does not exist in the cache.");
            }
        }
    }
}
