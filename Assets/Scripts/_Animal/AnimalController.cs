using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The AnimalController manages a single animal and its basic interactions:
/// - Feed, pet, pick up product (if available)
/// - Passes more complex logic to other components such as StateMachine, Friendship, Production.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class AnimalController : NetworkBehaviour, IInteractable {
    [SerializeField] private AnimalSO _animalData;
    [SerializeField] private string _animalName;
    [SerializeField] private bool _wasFed;
    public bool WasFed => _wasFed;
    [SerializeField] private bool _wasPetted;
    [SerializeField] private bool _gaveItem;

    [SerializeField] private int _stallID; // ID des Stalls in dem das Tier lebt
    [SerializeField] private AnimalVisual _animalVisual;

    [SerializeField] private int _friendship;
    [SerializeField] private int _maxFriendship;

    [SerializeField] private bool _isAdult = false;
    [SerializeField] private int _daysAsJuv = 0;

    // Components
    private AnimalStateMachine _stateMachine;
    private AnimalNavigation _navigation;
    private TimeManager _timeManager;


    public float MaxDistanceToPlayer => 2f;
    public bool CircleInteract => false;

    private void Awake() {
        _stateMachine = GetComponent<AnimalStateMachine>();
        _navigation = GetComponent<AnimalNavigation>();
    }

    public void Initialize(AnimalSO animalData, string animalName, int stallID) {
        _animalData = animalData;
        _animalName = animalName;
        _stallID = stallID;
        _friendship = animalData.InitialFriendship;
        _maxFriendship = animalData.MaxFriendship;

        //_stateMachine.SetStateIdle();

    }


    private void ShowLove() => _animalVisual.ShowLoveIcon();    

    public void InitializePreLoad(int itemId) { }

    public void PickUpItemsInPlacedObject(PlayerController player) { }

    public void Interact(PlayerController player) {
        throw new NotImplementedException();
    }
}

