using System;
using Unity.Netcode;
using UnityEngine;

// This script manages the item databases
[RequireComponent(typeof(NetworkObject))]
public class ItemManager : NetworkBehaviour {
    public static ItemManager Instance { get; private set; }

    [SerializeField] ItemDatabaseSO _itemDatabase;
    public ItemDatabaseSO ItemDatabase => _itemDatabase;


    void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one ItemManager in the scene.");
        } else {
            Instance = this;
        }

        ItemDatabase.InitializeItems();
    }

    // Returns the maximum stackable amount for the given item ID.
    public int GetMaxStackableAmount(int itemId) => ItemDatabase[itemId].MaxStackableAmount;
}
