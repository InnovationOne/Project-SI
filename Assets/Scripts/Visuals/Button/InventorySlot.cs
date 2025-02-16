using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ClothingSO;

// This script is used on every item slot button
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class InventorySlot : MonoBehaviour, 
    IPointerClickHandler, 
    IPointerDownHandler, 
    IBeginDragHandler, 
    IEndDragHandler, 
    IDropHandler, 
    IDragHandler, 
    IPointerEnterHandler, 
    IPointerExitHandler {

    public event Action<int> OnNewItem;

    [Header("Item button referenzes")]
    [SerializeField] Image _inventorySlotNormal;
    [SerializeField] Image _inventorySlotLocked;
    [SerializeField] Image _itemRarityImage;
    [SerializeField] Image _itemIconImage;
    [SerializeField] Image _selectedImage;
    [SerializeField] TextMeshProUGUI _itemAmountText;
    [SerializeField] TextMeshProUGUI _hotkeyText;

    [Header("Restrictions")]
    public bool IsClothingSlot;
    [ConditionalHide("IsClothingSlot", true)]
    public ClothingType[] AcceptedClothingType;

    int _buttonIdx;
    ItemContainerUI _itemPanel;
    ItemSlot _itemSlot;
    ItemManager _itemManager;
    CanvasGroup _dragItemCanvasGroup;

    // Called when the object is created.
    void Awake() => ClearItemSlot();

    // Caches references and initializes visuals.
    void Start() {
        _itemManager = GameManager.Instance.ItemManager;
        if (_itemManager == null)
            Debug.LogError("ItemManager instance is not available.");

        _dragItemCanvasGroup = DragItemUI.Instance.GetComponent<CanvasGroup>();
        _selectedImage.enabled = false;
    }

    // Shows item information when the pointer enters.
    public void OnPointerEnter(PointerEventData eventData) {
        if (_itemPanel != null) _itemPanel.TriggerItemInfo(_itemSlot);
    }

    // Hides item information when the pointer exits.
    public void OnPointerExit(PointerEventData eventData) {
        if (_itemPanel != null) _itemPanel.HideItemInfo();
    }

    // Sets the slot index and caches the parent UI container.
    public void SetButtonIndex(int idx) {
        _buttonIdx = idx;
        if (_itemPanel == null) {
            _itemPanel = GetComponentInParent<ItemContainerUI>();
            if (_itemPanel == null) Debug.LogError("ItemContainerUI not found in parent.");
        }
    }

    // Enables or disables interaction on this slot.
    public void SetInteractable(bool interactable) {
        _inventorySlotNormal.enabled = interactable;
        _inventorySlotLocked.enabled = !interactable;
        GetComponent<Button>().interactable = interactable;
    }

    // Assigns an item to the slot and updates its UI.
    public void SetItemSlot(ItemSlot newItemSlot, Sprite[] raritySprites) {
        _itemSlot = newItemSlot;
        if (_itemSlot == null) {
            OnNewItem?.Invoke(0);
            ClearItemSlot();
            return;
        }

        OnNewItem?.Invoke(newItemSlot.ItemId);
        _itemIconImage.enabled = true;

        var item = GameManager.Instance.ItemManager.ItemDatabase[_itemSlot.ItemId];
        _itemIconImage.sprite = item.ItemIcon;

        /* TODO Rarity system
        switch (item) {
            case ToolSO tool:
                if (_itemSlot.RarityId > 0 && _itemSlot.RarityId - 1 < tool.ToolItemRarity.Length) {
                    _itemIconImage.sprite = tool.ToolItemRarity[_itemSlot.RarityId - 1];
                }
                break;
            default:
                if (_itemSlot.RarityId > 0 && _itemSlot.RarityId - 1 < raritySprites.Length) {
                    _itemRarityImage.enabled = true;
                    _itemRarityImage.sprite = raritySprites[_itemSlot.RarityId - 1];
                }
                break;
        }
        */

        _itemAmountText.SetText(item.IsStackable ? _itemSlot.Amount.ToString() : string.Empty);
    }

    // Clears the slot and resets its UI.
    public void ClearItemSlot() {
        OnNewItem?.Invoke(0);
        _itemIconImage.enabled = false;
        _itemAmountText.SetText(string.Empty);
        _itemRarityImage.sprite = null;
        _itemRarityImage.enabled = false;
        _itemSlot = null;
    }

    // Updates the hotkey text on the slot.
    public void SetHotkey(string newHotkey) => _hotkeyText.text = newHotkey;

    // Highlights or unhighlights this slot.
    public void SetButtonHighlight(bool highlight) => _selectedImage.enabled = highlight;

    // Retrieves the UI sprite for player clothing if available.
    public Sprite GetPlayerClothingUiSprite() {
        if (_itemSlot == null || _itemSlot.IsEmpty || _itemManager.ItemDatabase[_itemSlot.ItemId] as ClothingSO == null) return null;
        return (_itemManager.ItemDatabase[_itemSlot.ItemId] as ClothingSO).PlayerClothingUiSprite; 
    }

    #region  -------------------- Drag and Drop Handlers --------------------

    public void OnPointerClick(PointerEventData eventData) {
        // Right-click: process right-click action or show right-click menu.
        if (eventData.button == PointerEventData.InputButton.Right) {
            if (DragItemUI.Instance.gameObject.activeSelf) _itemPanel.OnPlayerRightClick(_buttonIdx);
            else _itemPanel.ShowRightClickMenu(_buttonIdx, transform.position);
        }
    }

    // Left-click: process left-click action.
    public void OnPointerDown(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) _itemPanel.OnPlayerLeftClick(_buttonIdx);
    }

    // Allow dragging without blocking raycasts.
    public void OnBeginDrag(PointerEventData eventData) {
        if (_dragItemCanvasGroup != null) _dragItemCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData) {
        // Re-enable raycasts after dragging.
        if (_dragItemCanvasGroup != null) _dragItemCanvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag != null) _itemPanel.OnPlayerLeftClick(_buttonIdx);
    }

    #endregion -------------------- Drag and Drop Handlers --------------------
}
