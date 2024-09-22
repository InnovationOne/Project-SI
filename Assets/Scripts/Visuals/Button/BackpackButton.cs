using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// This script is used on every item slot button
public class BackpackButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDropHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler {
    [Header("Item button referenzes")]
    [SerializeField] private Image _itemRarityImage;
    [SerializeField] private Image _itemIconImage;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private Image _itemAmountBackgroundImage;
    [SerializeField] private TextMeshProUGUI _itemAmountText;

    private int _buttonIndex;
    private ItemContainerUI _itemPanel;
    private ItemSlot _itemSlot;

    // Cached references for optimization
    private ItemManager _itemManager;
    private CanvasGroup _dragCanvasGroup;

    private void Awake() {
        ClearItemSlot();

        _dragCanvasGroup = DragItemUI.Instance.GetComponent<CanvasGroup>();
        // Cache the ItemManager instance if it's not already cached

    }

    private void Start() {
        _itemManager = ItemManager.Instance;
        if (_itemManager == null) {
            Debug.LogError("ItemManager instance is not available.");
        }

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

    public void SetItemSlot(ItemSlot itemSlot, Sprite[] raritySprites) {
        _itemSlot = itemSlot;

        if (_itemSlot == null) {
            ClearItemSlot();
            return;
        }

        _itemIconImage.gameObject.SetActive(true);

        // Retrieve the item once and reuse the reference
        var item = _itemManager.ItemDatabase[_itemSlot.ItemId];
        _itemIconImage.sprite = item.ItemIcon;

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

        // Handle stackable items
        if (item.IsStackable) {
            _itemAmountBackgroundImage.gameObject.SetActive(true);
            _itemAmountText.SetText(_itemSlot.Amount.ToString());
        }
    }

    public void ClearItemSlot() {
        _itemIconImage.gameObject.SetActive(false);
        _itemAmountText.SetText(string.Empty);
        _itemAmountBackgroundImage.gameObject.SetActive(false);
        _itemRarityImage.sprite = null;
        _itemRarityImage.gameObject.SetActive(false);
        _itemSlot = null;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (_itemPanel == null) {
            return;
        }

        // Right click
        if (eventData.button == PointerEventData.InputButton.Right) {
            if (DragItemUI.Instance.gameObject.activeSelf) {
                _itemPanel.OnPlayerRightClick(_buttonIndex);
            } else {
                _itemPanel.ShowRightClickMenu(_buttonIndex, transform.position);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (_itemPanel == null) {
            return;
        }

        // Left click
        if (eventData.button == PointerEventData.InputButton.Left) {
            _itemPanel.OnPlayerLeftClick(_buttonIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (_dragCanvasGroup != null) {
            // Dragged item cannot block the button to trigger events
            _dragCanvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData) {
        if (_dragCanvasGroup != null) {
            // Dragged item can be clicked again
            _dragCanvasGroup.blocksRaycasts = true;
        }
    }

    public void OnDrop(PointerEventData eventData) {
        if (_itemPanel == null) {
            return;
        }

        if (eventData.pointerDrag != null) {
            _itemPanel.OnPlayerLeftClick(_buttonIndex);
        }
    }

    public void SetButtonHighlight(bool highlight) {
        _selectedImage.gameObject.SetActive(highlight);
    }
}
