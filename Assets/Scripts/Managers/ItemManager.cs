using UnityEngine;

// This script manages the item databases
public class ItemManager : MonoBehaviour {
    public static ItemManager Instance { get; private set; }


    [SerializeField] ItemDatabaseSO _itemDatabase;
    public ItemDatabaseSO ItemDatabase => _itemDatabase;


    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one ItemManager instance found!");
            return;
        }
        Instance = this;

        ItemDatabase.InitializeItems();
    }

    // Returns the maximum stackable amount for the given item ID.
    public int GetMaxStackableAmount(int itemId) => ItemDatabase[itemId].MaxStackableAmount;
}
