using UnityEngine;
using System.Collections.Generic;

public class Building : PlaceableObject {
    [SerializeField] protected BuildingSO _buildingSO;
    public BuildingSO BuildingSO => _buildingSO;
    protected List<AnimalController> _housedAnimals = new List<AnimalController>();
    public List<AnimalController> HousedAnimals => _housedAnimals;

    [SerializeField] protected HayDistributor _hayDistributor;
    [SerializeField] protected Incubator _incubator;
    [SerializeField] protected DoorController _doorController;
    [SerializeField] protected int _buildingLevel = 0;

    public float MaxDistanceToPlayer => 3f;
    public bool IsFull => _housedAnimals.Count >= _buildingSO.Capacity[_buildingLevel];

    [SerializeField] private PolygonCollider2D _stallCollider;
    [SerializeField] private BoxCollider2D _doorCollider;

    private void Start() {
        TimeManager.Instance.OnNextDayStarted += OnNextDay;
    }

    private void OnNextDay() {
        foreach (var animal in GetAnimalsOutside()) {
            float chance = Random.value;
            if (chance < 0.3f) {
                Debug.Log("Animal attacked and removed: " + animal.name);
                RemoveAnimal(animal);
            }
        }
    }

    public virtual void Interact(PlayerController player) {
        // Öffnet z. B. ein UI, das zeigt, wie viele Tiere hier drin sind, 
        // ob Heu verteilt wurde, Incubator Status, etc.
        Debug.Log("Interacting with Stall: " + _buildingSO.BuildingName);
    }

    public virtual void AddAnimal(AnimalController animal) {
        if (IsFull) {
            Debug.Log("Stall is full!");
            return;
        }

        _housedAnimals.Add(animal);
        Debug.Log("Animal added to " + _buildingSO.BuildingName);
    }

    public virtual void RemoveAnimal(AnimalController animal) {
        if (_housedAnimals.Contains(animal)) {
            _housedAnimals.Remove(animal);
            Debug.Log("Animal removed from " + _buildingSO.BuildingName);
        }
    }

    public virtual List<AnimalController> GetAnimalsOutside() {
        var animalsOutside = new List<AnimalController>();
        foreach (var animal in _housedAnimals) {
            if (!_stallCollider.bounds.Contains(animal.transform.position)) {
                animalsOutside.Add(animal);
            }
        }
        return animalsOutside;
    }
}
