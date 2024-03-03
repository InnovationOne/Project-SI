using System;
using Unity.Netcode;
using UnityEngine;

// This script manages the item databases
public class ItemManager : NetworkBehaviour {
    public static ItemManager Instance { get; private set; }

    public ItemDatabaseSO ItemDatabase;


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one ItemManager in the scene.");
        } else {
            Instance = this;
        }

        ItemDatabase.SetItemID();
        ItemDatabase.SetItemTypeID();
    }
}
