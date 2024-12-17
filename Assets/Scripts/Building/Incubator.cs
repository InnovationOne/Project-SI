using System.Linq;
using UnityEngine;

/// <summary>
/// Brutkasten: Eier hineinlegen, nach 10080 Minuten (7 Tage) Küken ausbrüten, 
/// falls Platz im Stall vorhanden.
/// </summary>
public class Incubator : MonoBehaviour, IInteractable {
    private int _eggItemID = 0;
    private const float INCUBATION_TIME = 10080f; // Minutes
    private float _elapsedTime = 0f;
    private bool _running = false;
    private Building _parentStall;
    private ItemSO[] _hatchableEggs;

    private PlayerToolbeltController _pTC;
    private PlayerInventoryController _pIC;

    public float MaxDistanceToPlayer => 2f;

    private void Start() {
        _pTC = PlayerToolbeltController.LocalInstance;
        _pIC = PlayerInventoryController.LocalInstance;
        _parentStall = GetComponentInParent<Building>();

    } 

    public void Interact(Player player) {
        foreach(var ItemSO in _hatchableEggs) {
            if (_pTC.GetCurrentlySelectedToolbeltItemSlot().ItemId == ItemSO.ItemId && _eggItemID == 0) {                
                _eggItemID = ItemSO.ItemId;
                _running = true;
                _pIC.InventoryContainer.RemoveItem(new ItemSlot(ItemSO.ItemId, 1, 0));
                return;
            }
        }

        if (_eggItemID != 0) {
            _pIC.InventoryContainer.AddItem(new ItemSlot(_eggItemID, 1, 0), false);
            _eggItemID = 0;
            _running = false;
            _elapsedTime = 0;
        }
    }

    private void Update() {
        if (!_parentStall.IsFull && _eggItemID != 0) {
            _running = true;
        }

        if (_running) {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= INCUBATION_TIME) {
                TryHatchEgg();
            }
        }
    }

    private void TryHatchEgg() {
        if (!_parentStall.IsFull) {
            // Neues Tier spawnen
            // ... Instanziere ein Küken hier ...
            Debug.Log("Egg hatched into a chick!");
            _eggItemID = 0;
            _running = false;
            _elapsedTime = 0;
        } else {
            Debug.Log("No space in stall. Incubation paused.");
            _running = false;
        }
    }

    public void PickUpItemsInPlacedObject(Player player) { }

    public void InitializePreLoad(int itemId) { }
}
