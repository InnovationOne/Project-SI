using System;
using UnityEngine;

/// <summary>
/// The AnimalController manages a single animal and its basic interactions:
/// - Feed, pet, pick up product (if available)
/// - Passes more complex logic to other components such as StateMachine, Friendship, Production.
/// </summary>
public class AnimalController : MonoBehaviour, IInteractable {
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

    [NonSerialized] private float _maxDistanceToPlayer;
    public float MaxDistanceToPlayer => _maxDistanceToPlayer;

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

        _timeManager.OnNextDayStarted += OnNextDay;
    }

    public void Interact(Player player) {
        var currentItemId = PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId;
        var currentItem = ItemManager.Instance.ItemDatabase[currentItemId];

        if (currentItem == _animalData.FeedItem && !_wasFed) {
            _wasFed = true;
            ModifyFriendship(5);
            ShowLove();
            return;
        }

        if (!_wasPetted) {
            ModifyFriendship(currentItem == _animalData.PetItem ? 10 : 5);
            _wasPetted = true;
            ShowLove();
            return;
        }

        if (!_gaveItem) {
            var productSlot = GetTodaysProduct();
            if (productSlot.ItemId >= 0) {
                PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(productSlot, false);
                _gaveItem = true;
                ShowLove();
            }
        }
    }

    private void OnNextDay() {
        _wasFed = false;
        _wasPetted = false;
        _gaveItem = false;

        if (!_isAdult) {
            _daysAsJuv++;
            if (_daysAsJuv >= _animalData.GrowthDays) {
                _isAdult = true;
                // Here you could customize the appearance of the animal (change spriter)
            }
        }
    }

    #region -------------------- Friendship --------------------
    public void ModifyFriendship(int amount) {
        _friendship += amount;
        if (_friendship < 0) _friendship = 0;
        if (_friendship > _maxFriendship) _friendship = _maxFriendship;
    }

    public int GetFriendshipValue() {
        return _friendship;
    }

    public float GetNormalizedFriendship() {
        return (float)_friendship / (float)_maxFriendship;
    }
    #endregion -------------------- Friendship --------------------

    #region -------------------- Production --------------------
    public ItemSlot GetTodaysProduct() {
        float val = GetNormalizedFriendship();
        int quality = 0;
        if (val > 0.98f) quality = 3;
        else if (val > 0.92f) quality = 2;
        else if (val > 0.80f) quality = 1;

        // Gibt 1x das Produkt zurück, mit ermittelter Qualität
        return new ItemSlot(_animalData.ProductItem.ItemId, 1, quality);
    }
    #endregion -------------------- Production --------------------

    private void ShowLove() => _animalVisual.ShowLoveIcon();    

    public void InitializePreLoad(int itemId) { }

    public void PickUpItemsInPlacedObject(Player player) { }
}

