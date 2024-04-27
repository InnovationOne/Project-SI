using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// This script is used on every item slot button
public class BackpackButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDropHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler {
    [Header("Item button referenzes")]
    [SerializeField] private Image _itemRarityImage;
    [SerializeField] private Image _itemIconImage;
    [SerializeField] private Image _toolRarityImage;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private Image _itemAmountBackgroundImage;
    [SerializeField] private TextMeshProUGUI _itemAmountText;

    private int _buttonIndex;
    private ItemContainerPanel _itemPanel;
    private ItemSlot _itemSlot;


    private void Start() {
        _selectedImage.gameObject.SetActive(false); 
    }

    public void OnPointerEnter(PointerEventData eventData) {
        _itemPanel.TriggerItemInfo(_itemSlot);
    }

    public void OnPointerExit(PointerEventData eventData) {
        _itemPanel.HideItemInfo();
    }

    public void SetButtonIndex(int buttonIndex) {
        _buttonIndex = buttonIndex;

        _itemPanel = transform.GetComponentInParent<ItemContainerPanel>();
    }

    public void SetItemSlot(ItemSlot itemSlot, Sprite raritySprite) {
        _itemSlot = itemSlot;
        _itemIconImage.gameObject.SetActive(true);
        _itemIconImage.sprite = _itemSlot.Item.ItemIcon;

        if (_itemSlot.Item.IsStackable) {
            _itemAmountBackgroundImage.gameObject.SetActive(true);
            _itemAmountText.text = _itemSlot.Amount.ToString();
        } else {
            _itemAmountBackgroundImage.gameObject.SetActive(false);
            _itemAmountText.text = string.Empty;
        }

        if (raritySprite != null) {
            if (_itemSlot.Item.ItemType == ItemSO.ItemTypes.Tools) {
                _itemRarityImage.gameObject.SetActive(false);
                _toolRarityImage.gameObject.SetActive(true);
                _toolRarityImage.sprite = raritySprite;
            } else {
                _toolRarityImage.gameObject.SetActive(false);
                _itemRarityImage.gameObject.SetActive(true);
                _itemRarityImage.sprite = raritySprite;
            }
            
        } else {
            _toolRarityImage.gameObject.SetActive(false);
            _itemRarityImage.gameObject.SetActive(false);
        }
    }

    public void ClearItemSlot() {
        _itemIconImage.gameObject.SetActive(false);
        _itemAmountText.text = string.Empty;
        _itemAmountBackgroundImage.gameObject.SetActive(false);
        _itemRarityImage.sprite = null;
        _itemRarityImage.gameObject.SetActive(false);
        _toolRarityImage.sprite = null;
        _toolRarityImage.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Right) { // Right click
            if (DragItemPanel.Instance.gameObject.activeSelf) {
                _itemPanel.OnPlayerRightClick(_buttonIndex);
            } else {
                _itemPanel.ShowRightClickMenu(_buttonIndex, transform.position);
            }
            
        }
    }

    // This function is called on mouse button down
    public void OnPointerDown(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) { // Left click
            _itemPanel.OnPlayerLeftClick(_buttonIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        //Dragged item cannot block the button to trigger events
        DragItemPanel.Instance.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData) {
        //Dragged item can be clicked again
        DragItemPanel.Instance.GetComponent<CanvasGroup>().blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag != null) {
            _itemPanel.OnPlayerLeftClick(_buttonIndex);
        }
    }

    public void SetButtonHighlight(bool highlight) {
        _selectedImage.gameObject.SetActive(highlight);
    }
}
