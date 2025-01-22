using UnityEngine;
using UnityEngine.TextCore.Text;

public class SellChest : MonoBehaviour, IInteractable {
    [SerializeField] ItemContainerSO _sellBoxContainer;

    public float MaxDistanceToPlayer => 0.5f;

    public void Interact(PlayerController playerController) {
        if (playerController == null) return;

        if (!playerController.TryGetComponent<PlayerToolbeltController>(out var toolbelt)) return;

        var slot = toolbelt.GetCurrentlySelectedToolbeltItemSlot();
        if (slot == null || slot.ItemId <= 0) return;

        var database = GameManager.Instance.ItemManager.ItemDatabase;
        if (!database[slot.ItemId]) return;

        var item = database[slot.ItemId];
        if (item == null || !item.CanBeSold) return;

        _sellBoxContainer.AddItem(slot, false);
        toolbelt.ClearCurrentItemSlot();
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }

    public void InitializePreLoad(int itemId) { }
}
