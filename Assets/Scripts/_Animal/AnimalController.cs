using System;
using UnityEngine;


public class AnimalController : MonoBehaviour, IInteractable {
    [Header("Animal Settings")]
    [SerializeField] private AnimalSO _animal;
    [SerializeField] private string _animalName;

    [Header("Animal Condition")]
    [SerializeField] private bool _wasFed;
    [SerializeField] private bool _wasPetted;
    [SerializeField] private bool _gaveItem;

    [Header("Stall Settings")]
    [SerializeField] private int _stallID;

    [Header("Visual Settings")]
    [SerializeField] private AnimalVisual _animalVisual;

    [NonSerialized] private float _maxDistanceToPlayer;
    public virtual float MaxDistanceToPlayer { get => _maxDistanceToPlayer; }

    public void InitializePreLoad(int itemId) { }

    public void Interact(Player player) {
        if (PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId == _animal.ItemToFeed.ItemId && !_wasFed) {
            ShowLove();
            _wasFed = true;
            Debug.Log("Animal was fed");
        } else if (!_wasPetted) {
            ShowLove();
            _wasPetted = true;
            Debug.Log("Animal was petted");
        } else if (_wasFed && _wasPetted && !_gaveItem) {
            ShowLove();
            _gaveItem = true;
            Debug.Log("Animal gave item");
        }
    }

    public void PickUpItemsInPlacedObject(Player player) { }

    private void ShowLove() {
        _animalVisual.ShowLoveIcon();
    }
}
