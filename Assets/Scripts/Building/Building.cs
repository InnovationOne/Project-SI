using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

public class Building : PlaceableObject {
    [SerializeField] protected BuildingSO _buildingSO;
    public BuildingSO BuildingSO => _buildingSO;
    protected List<AnimalController> _housedAnimals = new List<AnimalController>();
    public List<AnimalController> HousedAnimals => _housedAnimals;

    [SerializeField] protected HayDistributor _hayDistributor;
    [SerializeField] protected Incubator _incubator;
    [SerializeField] protected DoorController _doorController;
    [SerializeField] protected int _buildingLevel = 0;

    public bool IsFull => _housedAnimals.Count >= _buildingSO.Capacity[_buildingLevel];

    public override float MaxDistanceToPlayer => 0f;

    [SerializeField] private PolygonCollider2D _stallCollider;
    [SerializeField] private BoxCollider2D _doorCollider;

    private void Start() {
        GameManager.Instance.TimeManager.OnNextDayStarted += OnNextDay;
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

    public override void Interact(PlayerController player) {
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

    public override void InitializePreLoad(int itemId) { }

    public override void InitializePostLoad() { }

    public override void LoadObject(string data) { }

    public override string SaveObject() { return string.Empty; }

    public override void PickUpItemsInPlacedObject(PlayerController player) { }
}
