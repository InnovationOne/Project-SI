using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AnimalBuilding : Building {
    [Header("Animal Building Settings")]
    [SerializeField] private AnimalBuildingSO _animalBuildingSO;
    [SerializeField] private BoxCollider2D _spawnArea;
    [SerializeField] private DoorController _doorController;
    public bool IsDoorOpen => _doorController.IsOpen;
    public Vector3 DoorPosition => _doorController.ExitPoint.position;

    public AnimalBuildingSO AnimalBuildingSO => _animalBuildingSO;

    private NetworkList<ulong> _housedAnimalIds;
    public List<ulong> HousedAnimalIdsList {
        get {
            var list = new List<ulong>();
            foreach (var id in _housedAnimalIds){ 
                list.Add(id); 
            }
            return list;
        }
    }

    public int Capacity => _animalBuildingSO.Capacity;
    private int _reservedSlots = 0;
    public bool HasFreeSpace => _housedAnimalIds.Count + _reservedSlots < _animalBuildingSO.Capacity;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            _housedAnimalIds = new NetworkList<ulong>(new List<ulong>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        }
    }

    protected override void OnConstructionFinished() {
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Add an animal if there is still room.
    /// </summary>
    public void AddAnimal(AnimalController animal) {
        if (!IsServer) return;
        if (_housedAnimalIds.Count >= Capacity) return;
        _housedAnimalIds.Add(animal.NetworkObjectId);
    }

    /// <summary>
    /// Remove an animal from this stable.
    /// </summary>
    public void RemoveAnimal(AnimalController animal) {
        if (!IsServer) return;
        _housedAnimalIds.Remove(animal.NetworkObjectId);
    }

    /// <summary>
    /// Spawn a product directly in the building.
    /// </summary>
    public void SpawnProductInside(ItemSlot product) {
        var bounds = _spawnArea.bounds;
        Vector2 spawnPos = new(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            product, 
            spawnPos, 
            Vector2.zero, 
            spreadType: ItemSpawnManager.SpreadType.Circle);
    }

    public void ReserveSlot() {
        _reservedSlots++; 
    }
    public void UnreserveSlot() { 
        _reservedSlots--; 
    }

    public override void Interact(PlayerController player) { }
}
