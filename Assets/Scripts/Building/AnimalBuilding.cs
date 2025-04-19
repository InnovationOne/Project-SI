using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AnimalBuilding : Building {
    [Header("Animal Building Settings")]
    [SerializeField] private AnimalBuildingSO _animalBuildingSO;
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

    /// <summary>
    /// Maximum number of animals in the building.
    /// </summary>
    public int Capacity => _animalBuildingSO.Capacity;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            _housedAnimalIds = new NetworkList<ulong>(
                new List<ulong>(),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);
        }
    }

    protected override void OnConstructionFinished() {
        base.OnConstructionFinished();
        // Nach dem Bau z.B. Tür, Heu‑Verteiler, Inkubator aktivieren …
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

    public override void Interact(PlayerController player) { }
}
