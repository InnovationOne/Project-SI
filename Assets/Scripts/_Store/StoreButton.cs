using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StoreButton : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private Image _item;
    [SerializeField] private Image _isSelected;

    private int _price;
    private ItemSO _itemSO;
    private StoreVisual _storeVisual;


    private void Start() {
        _storeVisual = GetComponentInParent<StoreVisual>();
    }

    public void SetIndex(ItemSO itemSO) {
        _itemSO = itemSO;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            _storeVisual.OnLeftClick(_itemSO);
        }
    }

    public void SetItemImage(Sprite sprite, int prize) {
        _item.sprite = sprite;
        _price = prize;
    }
}
