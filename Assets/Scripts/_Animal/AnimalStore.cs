using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class AnimalStore : Store {
    private Dictionary<int, AnimalSO> _purchasedAnimalsPendingDelivery = new();

    private PlayerInventoryController _pIC;
    private FinanceManager _fM;
    private PlaceableObjectsManager _pOM;
    private ItemManager _iM;
    private NetworkSpawnManager _nSM;

    void Start() {
        _pIC = PlayerInventoryController.LocalInstance;
        _fM = FinanceManager.Instance;
        _pOM = PlaceableObjectsManager.Instance;
        _iM = ItemManager.Instance;
        _nSM = NetworkManager.Singleton.SpawnManager;

        TimeManager.Instance.OnNextDayStarted += OnNextDay;
    }

    public override void OnLeftClick(ItemSO itemSO) {
        if (itemSO is AnimalSO) {
            AnimalSO animalSO = itemSO as AnimalSO;

            // Search for a stall that can house the animal, TODO make this a choice of the player.
            for (int i = 0; i < _pOM.PlaceableObjects.Count; i++) {
                int objId = _pOM.PlaceableObjects[i].ObjectId;
                BuildingSO buildingSO = _iM.ItemDatabase[objId] as BuildingSO;
                if (buildingSO.AnimalSize == animalSO.AnimalSize) {
                    var netId = _pOM.PlaceableObjects[i].PrefabNetworkObjectId;
                    if (!_nSM.SpawnedObjects.TryGetValue(netId, out NetworkObject netObj)) {
                        Debug.LogError($"No NetworkObject with the ID {netId} found.");
                        return;
                    }

                    Building building = netObj.GetComponent<Building>();
                    if (!building.IsFull) {
                        _purchasedAnimalsPendingDelivery.Add(i, animalSO);
                        Debug.Log("Animal purchased, will be delivered tomorrow.");
                        return;
                    }
                }
            }
        } else {
            for (int i = 0; i < _storeContainer.ItemSlots.Count; i++) {
                if (itemSO.ItemId == _storeContainer.ItemSlots[i].ItemId) {
                    if (_pIC.InventoryContainer.CanAddItem(new ItemSlot(itemSO.ItemId, 1, 0), false)) {
                        _fM.RemoveMoneyServerRpc(itemSO.BuyPrice, true);
                    }
                }
            }
        }
    }

    public void OnNextDay() {
        foreach (var kvp in _purchasedAnimalsPendingDelivery) {
            var animalGO = new GameObject("Animal_" + kvp.Value.AnimalType.ToString());
            var ac = animalGO.AddComponent<AnimalController>();
            animalGO.AddComponent<AnimalStateMachine>();
            animalGO.AddComponent<AnimalNavigation>();
            animalGO.AddComponent<AnimalVisual>();

            // TODO: When buying let the player choose the name.
            var animalName = kvp.Value.AnimalType.ToString();

            ac.Initialize(kvp.Value, animalName, kvp.Key);

            var netId = _pOM.PlaceableObjects[kvp.Key].PrefabNetworkObjectId;
            if (!_nSM.SpawnedObjects.TryGetValue(netId, out NetworkObject netObj)) {
                Debug.LogError($"No NetworkObject with the ID {netId} found.");
                return;
            }
            Building building = netObj.GetComponent<Building>();
            building.AddAnimal(ac);

            Debug.Log($"Animal delivered to stall {building.BuildingSO.BuildingName}.");
        }

        _purchasedAnimalsPendingDelivery.Clear();
    }
}
