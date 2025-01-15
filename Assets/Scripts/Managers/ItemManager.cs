using UnityEngine;

// This script manages the item databases
public class ItemManager : MonoBehaviour {
    [SerializeField] ItemDatabaseSO _itemDatabase;
    public ItemDatabaseSO ItemDatabase => _itemDatabase;


    void Awake() {
        ItemDatabase.InitializeItems();
    }

    // Returns the maximum stackable amount for the given item ID.
    public int GetMaxStackableAmount(int itemId) => ItemDatabase[itemId].MaxStackableAmount;
}
