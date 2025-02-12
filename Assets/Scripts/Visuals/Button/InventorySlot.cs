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
public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDropHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler {
    public event Action<int> OnNewItem;

    [Header("Item button referenzes")]
    [SerializeField] private Image _inventorySlot_Normal;
    [SerializeField] private Image _inventorySlot_Locked;

    [SerializeField] private Image _itemRarityImage;
    [SerializeField] private Image _itemIconImage;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private TextMeshProUGUI _itemAmountText;

    [Header("Restrictions")]
    public bool IsClothingSlot;
    [ConditionalHide("IsClothingSlot", true)]
    public ClothingType AcceptedClothingType;

    private int _buttonIndex;
    private ItemContainerUI _itemPanel;
    private ItemSlot _itemSlot;

    // Cached references for optimization
    private ItemManager _itemManager;
    private CanvasGroup _dragItemUICanvasGroup;

    private void Awake() {
        ClearItemSlot();
    }

    private void Start() {
        _itemManager = GameManager.Instance.ItemManager;
        if (_itemManager == null) {
            Debug.LogError("ItemManager instance is not available.");
        }

        _dragItemUICanvasGroup = DragItemUI.Instance.GetComponent<CanvasGroup>();

        _selectedImage.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (_itemPanel != null && _itemSlot != null) {
            _itemPanel.TriggerItemInfo(_itemSlot);
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (_itemPanel != null) {
            _itemPanel.HideItemInfo();
        }
    }

    public void SetButtonIndex(int buttonIndex) {
        _buttonIndex = buttonIndex;

        // Cache the ItemContainerUI reference
        if (_itemPanel == null) {
            _itemPanel = GetComponentInParent<ItemContainerUI>();
            if (_itemPanel == null) {
                Debug.LogError("ItemContainerUI not found in parent.");
            }
        }
    }

    public void SetInteractable(bool interactable) {
        _inventorySlot_Normal.enabled = interactable;
        _inventorySlot_Locked.enabled = !interactable;
        GetComponent<Button>().interactable = interactable;
    }

    public void SetItemSlot(ItemSlot newItemSlot, Sprite[] raritySprites) {
        _itemSlot = newItemSlot;

        if (_itemSlot == null) {
            OnNewItem?.Invoke(0);
            ClearItemSlot();
            return;
        }

        OnNewItem?.Invoke(newItemSlot.ItemId);

        _itemIconImage.gameObject.SetActive(true);

        var item = GameManager.Instance.ItemManager.ItemDatabase[_itemSlot.ItemId];
        _itemIconImage.sprite = item.ItemIcon;

        /* TODO
        switch (item) {
            case ToolSO tool:
                if (_itemSlot.RarityId > 0 && _itemSlot.RarityId - 1 < tool.ToolItemRarity.Length) {
                    _itemIconImage.sprite = tool.ToolItemRarity[_itemSlot.RarityId - 1];
                }
                break;
            default:
                if (_itemSlot.RarityId > 0 && _itemSlot.RarityId - 1 < raritySprites.Length) {
                    _itemRarityImage.gameObject.SetActive(true);
                    _itemRarityImage.sprite = raritySprites[_itemSlot.RarityId - 1];
                }
                break;
        }
        */

        // Handle stackable items
        if (item.IsStackable) {
            _itemAmountText.SetText(_itemSlot.Amount.ToString());
        } else {
            _itemAmountText.SetText(string.Empty);
        }
    }

    public void ClearItemSlot() {
        OnNewItem?.Invoke(0);
        _itemIconImage.gameObject.SetActive(false);
        _itemAmountText.SetText(string.Empty);
        _itemRarityImage.sprite = null;
        _itemRarityImage.gameObject.SetActive(false);
        _itemSlot = null;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Right) { // RMB Click
            if (DragItemUI.Instance.gameObject.activeSelf) {
                _itemPanel.OnPlayerRightClick(_buttonIndex);
            } else {
                _itemPanel.ShowRightClickMenu(_buttonIndex, transform.position);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) _itemPanel.OnPlayerLeftClick(_buttonIndex); // LMB Click
    }

    public void OnBeginDrag(PointerEventData eventData) {
        // Dragged item cannot block the button to trigger events
        if (_dragItemUICanvasGroup != null) _dragItemUICanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData) {
        // Draged item can be clicked again
        if (_dragItemUICanvasGroup != null) _dragItemUICanvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag != null) _itemPanel.OnPlayerLeftClick(_buttonIndex);
    }

    public void SetButtonHighlight(bool highlight) => _selectedImage.gameObject.SetActive(highlight);

    public Sprite GetPlayerClothingUiSprite() {
        if (_itemSlot == null || _itemSlot.IsEmpty) return null;
        return (GameManager.Instance.ItemManager.ItemDatabase[_itemSlot.ItemId] as ClothingSO).PlayerClothingUiSprite;
    }
}
