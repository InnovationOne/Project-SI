using System.Collections.Generic;
using UnityEngine;

public class AnimalBuilding : Building {
    [Header("Animal Building Settings")]
    [SerializeField] private AnimalBuildingSO _animalBuildingSO;

    private readonly List<AnimalController> _housedAnimals = new();
    public IReadOnlyList<AnimalController> HousedAnimals => _housedAnimals;

    /// <summary>
    /// Maximum number of animals in the building.
    /// </summary>
    public int Capacity => _animalBuildingSO.Capacity;

    protected override void OnConstructionFinished() {
        base.OnConstructionFinished();
        // Nach dem Bau z.B. Tür, Heu‑Verteiler, Inkubator aktivieren …
    }

    /// <summary>
    /// Add an animal if there is still room.
    /// </summary>
    public virtual void AddAnimal(AnimalController animal) {
        if (_housedAnimals.Count >= Capacity) {
            Debug.LogWarning($"{_animalBuildingSO.name} ist voll ({Capacity}).");
            return;
        }
        _housedAnimals.Add(animal);
        Debug.Log($"Tier {animal.name} in {_animalBuildingSO.name} untergebracht.");
    }

    /// <summary>
    /// Remove an animal from this stable.
    /// </summary>
    public virtual void RemoveAnimal(AnimalController animal) {
        if (_housedAnimals.Remove(animal)) {
            Debug.Log($"Tier {animal.name} aus {_animalBuildingSO.name} entfernt.");
        }
            
    }

    public override void Interact(PlayerController player) { }
}
