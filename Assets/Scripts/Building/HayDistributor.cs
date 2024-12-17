using UnityEngine;

/// <summary>
/// Verteilt Heu vom Silo zur Futterbank. Manuell oder automatisch.
/// </summary>
public class HayDistributor : MonoBehaviour, IInteractable {
    [SerializeField] private ItemSO _hayItem;

    private const int MAX_HAY_CAPACITY = 20;
    private int _hayInFeeder = 0;
    public int HayInFeeder => _hayInFeeder;
    public float MaxDistanceToPlayer => 2f;

    private PlayerToolbeltController _pTC;
    private PlayerInventoryController _pIC;

    private void Start() {
        _pTC = PlayerToolbeltController.LocalInstance;
        _pIC = PlayerInventoryController.LocalInstance;
        TimeManager.Instance.OnNextDayStarted += OnNextDay;
    }

    public void Interact(Player player) {
        if (_pTC.GetCurrentlySelectedToolbeltItemSlot().ItemId == _hayItem.ItemId && _hayInFeeder < MAX_HAY_CAPACITY) {
            _hayInFeeder++;
            _pIC.InventoryContainer.RemoveItem(new ItemSlot(_hayItem.ItemId, 1, 0));
        } else {
            _hayInFeeder--;
            _pIC.InventoryContainer.AddItem(new ItemSlot(_hayItem.ItemId, 1, 0), false);
        }
    }

    private void OnNextDay() {
        int needed = MAX_HAY_CAPACITY - _hayInFeeder;
        if (needed > 0) {
            // Prüfe, wie viel Heu im Silo ist. 
            // Angenommen Silo ist einfach Teil des PlayerInventars mit ItemId von _hayItem
            int siloCount = 0;

            if (siloCount > 0) {
                int toTake = Mathf.Min(needed, siloCount);
                siloCount -= toTake; // Pseudo-Code
                _hayInFeeder += toTake;
            }
        }
    }

    public void PickUpItemsInPlacedObject(Player player) { }

    public void InitializePreLoad(int itemId) { }
}
