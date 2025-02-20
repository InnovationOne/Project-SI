using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragItemUI : MonoBehaviour {
    [SerializeField] private Image _itemIconImage;
    [SerializeField] private TextMeshProUGUI _itemAmountText;

    private void Start() {
        gameObject.SetActive(false);
    }

    public void SetItemSlot(ItemSlot itemSlot) {
        if (itemSlot.IsEmpty) {
            gameObject.SetActive(false);
            return;
        }

        var item = GameManager.Instance.ItemManager.ItemDatabase[itemSlot.ItemId];
        _itemIconImage.sprite = item.ItemIcon;
        _itemAmountText.text = item.IsStackable ? itemSlot.Amount.ToString() : string.Empty;
        gameObject.SetActive(true);
    }
}
