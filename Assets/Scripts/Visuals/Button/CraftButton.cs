using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftButton : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private Image _item;
    [SerializeField] private Image _isSelected;

    private int _buttonIndex;
    private CraftVisual _craftVisual;


    public void SetIndex(int index) {
        _buttonIndex = index;
        _craftVisual = GetComponentInParent<CraftVisual>();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            _craftVisual.OnLeftClick(_buttonIndex);
        }
    }

    public void SetItemImage(Sprite sprite) {
        _item.sprite = sprite;
    }
}
