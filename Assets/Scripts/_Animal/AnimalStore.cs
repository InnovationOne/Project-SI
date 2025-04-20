using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Store for purchasing animals. Handles purchase, pending delivery, and next-day delivery.
/// </summary>
public class AnimalStore : Store {
    [Header("Animal Store Settings")]
    [SerializeField] private AnimalSO[] _animalsForSale;  // configured in Inspector
    /*
    // Purchased entries: key = building NetId, value = list of AnimalSO to deliver
    private Dictionary<ulong, List<AnimalSO>> _pendingDeliveries = new();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            GameManager.Instance.TimeManager.OnNextDayStarted += DeliverAnimals;
        }
    }

    public override void Interact(PlayerController player) {
        // Open store UI listing _animalsForSale
        StoreUI.Instance.ShowAnimalStore(_animalsForSale);
    }

    /// <summary>
    /// Called when player clicks an AnimalSO in the store UI.
    /// </summary>
    public void PurchaseAnimal(AnimalSO animalSO, ulong buildingNetId) {
        // Deduct money
        if (!GameManager.Instance.FinanceManager.RemoveMoney(animalSO.BuyPrice)) return;

        // Queue for delivery
        if (!_pendingDeliveries.TryGetValue(buildingNetId, out var list)) {
            list = new List<AnimalSO>();
            _pendingDeliveries[buildingNetId] = list;
        }
        list.Add(animalSO);
        // Notify player
        NotificationManager.Instance.ShowMessage($"{animalSO.AnimalName} bestellt, Lieferung morgen.");
    }

    private void DeliverAnimals() {
        foreach (var kv in _pendingDeliveries) {
            if (kv.Key == 0) continue;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(kv.Key, out var netObj))
                continue;
            var building = netObj.GetComponent<AnimalBuilding>();
            if (building == null) continue;

            foreach (var animalSO in kv.Value) {
                // Spawn at stall entrance
                Vector3 spawnPos = building.DoorPosition;
                AnimalManager.Instance.RequestSpawnJuvenile(animalSO.AnimalId, spawnPos);
                building.AddAnimal(
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects[
                        netObj.NetworkObjectId]
                        .GetComponent<AnimalController>());
                NotificationManager.Instance.ShowMessage($"{animalSO.AnimalName} geliefert in {building.BuildingSO.BuildingName}.");
            }
        }
        _pendingDeliveries.Clear();
    }*/
}
