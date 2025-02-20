using UnityEngine;
using UnityEngine.UI;

public class ClothingUI : ItemContainerUI {
    [Header("Character Parts Visuals")]
    [SerializeField] Image[] _playerClothingUiImages;

    PlayerItemDragAndDropController _playerItemDragAndDropController;

    public InventorySlot[] PlayerClothingUIItemButtons => ItemButtons;
    public ItemContainerSO PlayerClothingUIItemContainer => ItemContainer;


    void Awake() {
        PlayerController.OnLocalPlayerSpawned += CatchReferences;
    }

    void Start() {
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        ItemContainerUIAwake();
        Init();
        UpdatePlayerVisual();
    }

    void CatchReferences(PlayerController playerController) {
        _playerItemDragAndDropController = playerController.PlayerItemDragAndDropController;
    }

    void OnDestroy() {
        ItemContainer.OnItemsUpdated -= ShowUIButtonContains;
        PlayerController.OnLocalPlayerSpawned -= CatchReferences;
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        var clothingSlot = ItemContainer.ItemSlots[buttonIndex];
        var dragItemSlot = _playerItemDragAndDropController.DragItemSlot;

        // 1) SHIFT-click => return clothing to inventory
        if (GameManager.Instance.InputManager.GetShiftPressed()) {
            if (!clothingSlot.IsEmpty) {
                int leftover = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(clothingSlot, true);
                if (leftover > 0) {
                    leftover = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(new ItemSlot(clothingSlot.ItemId, leftover, clothingSlot.RarityId), false);
                    if (leftover <= 0) clothingSlot.Clear();
                    else clothingSlot.Set(new ItemSlot(clothingSlot.ItemId, leftover, clothingSlot.RarityId));
                } else clothingSlot.Clear();

                RefreshUIAndReturn();
            }
            return;
        }

        // 2) Check whether we are drawing an item at all:
        if (!dragItemSlot.IsEmpty) {
            var clothingItem = GameManager.Instance.ItemManager.ItemDatabase[dragItemSlot.ItemId] as ClothingSO;
            if (clothingItem != null) {
                int torsoSlotIndex = (int)ClothingSO.ClothingType.Torso;
                int legsSlotIndex = (int)ClothingSO.ClothingType.Legs;

                var slots = ItemContainer.ItemSlots;
                var torsoSlot = slots[torsoSlotIndex];
                var legsSlot = slots[legsSlotIndex];
                bool torsoNotEmpty = !torsoSlot.IsEmpty;
                bool legsNotEmpty = !legsSlot.IsEmpty;

                // -----------------------------------------------
                // SCENARIO 1:
                // Dress is drag item + torso slot occupied + legs slot occupied
                // => place the dress in torso, swap with torso item, 
                //    then move the legs slot item to inventory or spawn leftover.
                // -----------------------------------------------
                if (clothingItem.Type == ClothingSO.ClothingType.Dress && torsoNotEmpty && legsNotEmpty) {
                    // 1a) Dress in Torso slot
                    var oldTorso = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Set(dragItemSlot);
                    dragItemSlot.Set(oldTorso);

                    // 1b) Move or drop the Legs item into the inventory
                    MoveItemToInventoryOrSpawn(legsSlot);
                    legsSlot.Clear();

                    RefreshUIAndReturn();
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 2:
                // Dress is drag item + only legs slot occupied
                // => place the dress in torso; the legs item becomes the new drag item
                // -----------------------------------------------
                if (clothingItem.Type == ClothingSO.ClothingType.Dress && legsNotEmpty && !torsoNotEmpty) {
                    // Dress in torso
                    torsoSlot.Set(dragItemSlot);

                    // What was in the legs goes into the DragSlot
                    var oldLegs = new ItemSlot(legsSlot.ItemId, legsSlot.Amount, legsSlot.RarityId);
                    legsSlot.Clear();
                    dragItemSlot.Set(oldLegs);

                    RefreshUIAndReturn();
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 3:
                // Dress is drag item + only torso slot occupied
                // => swap them directly (dress <-> torso)
                // -----------------------------------------------
                if (clothingItem.Type == ClothingSO.ClothingType.Dress && torsoNotEmpty && !legsNotEmpty) {
                    var oldTorso = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Set(dragItemSlot);
                    dragItemSlot.Set(oldTorso);

                    RefreshUIAndReturn();
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 4:
                // Pants as DragItem + Dress in torso
                // => place pants in legs, the dress goes to drag item
                // -----------------------------------------------
                if (clothingItem.Type == ClothingSO.ClothingType.Legs && IsDress(torsoSlot)) {
                    var oldDress = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Clear();

                    legsSlot.Set(dragItemSlot);
                    dragItemSlot.Set(oldDress);

                    RefreshUIAndReturn();
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 5:
                // Top as DragItem + Dress in torso
                // => top is placed in torso, dress is swapped into drag item
                // -----------------------------------------------
                if (clothingItem.Type == ClothingSO.ClothingType.Torso && IsDress(torsoSlot)) {
                    var oldDress = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Set(dragItemSlot);
                    dragItemSlot.Set(oldDress);

                    RefreshUIAndReturn();
                    return;
                }
            }
            // If it is NOT one of the above dress cases, you could
            // e.g. check whether the slot `buttonIndex` accepts this ClothingType.
            // If yes => normal drag
            // If no => cancel
            var slotButton = ItemButtons[buttonIndex];
            if (clothingItem == null || !slotButton.AllowsThisClothingType(clothingItem.Type)) return;
        }

        // If no special clothing scenario was triggered, do regular inventory logic: pick up, stack, swap, etc.
        _playerItemDragAndDropController.OnLeftClick(clothingSlot, ItemButtons[buttonIndex]);

        RefreshUIAndReturn();
    }

    void RefreshUIAndReturn() {
        PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.UpdateUI();
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
        ShowUIButtonContains();
        UpdatePlayerVisual();
    }

    bool IsDress(ItemSlot slot) {
        if (slot.IsEmpty) return false;
        var cso = GameManager.Instance.ItemManager.ItemDatabase[slot.ItemId] as ClothingSO;
        return cso != null && cso.Type == ClothingSO.ClothingType.Dress;
    }

    void MoveItemToInventoryOrSpawn(ItemSlot slotToMove) {
        int leftover = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(slotToMove, true);
        if (leftover > 0) {
            GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                slotToMove,
                PlayerController.LocalInstance.transform.position,
                PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                true
            );
            GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
        }
    }

    public void UpdatePlayerVisual() {
        for (int i = 0; i < _playerClothingUiImages.Length; i++) {
            _playerClothingUiImages[i].sprite = ItemButtons[i].GetPlayerClothingUiSprite();
            _playerClothingUiImages[i].enabled = _playerClothingUiImages[i].sprite != null;
        }
    }
}
