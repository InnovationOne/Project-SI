using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragItemUI : MonoBehaviour {
    public static DragItemUI Instance { get; private set; }

    [SerializeField] private Image _itemIconImage;
    [SerializeField] private Image _itemAmountBackgroundImage;
    [SerializeField] private TextMeshProUGUI _itemAmountText;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of DragItemPanel in the scene!");
            return;
        }
        Instance = this;
    }

    private void Start() => gameObject.SetActive(false);

    public void SetItemSlot(ItemSlot itemSlot) {
        _itemIconImage.gameObject.SetActive(true);
        _itemIconImage.sprite = ItemManager.Instance.ItemDatabase[itemSlot.ItemId].ItemIcon;

        if (ItemManager.Instance.ItemDatabase[itemSlot.ItemId].IsStackable) {
            _itemAmountBackgroundImage.gameObject.SetActive(true);
            _itemAmountText.text = itemSlot.Amount.ToString();
        } else {
            _itemAmountBackgroundImage.gameObject.SetActive(false);
            _itemAmountText.text = string.Empty;
        }
    }

    public void ClearItemSlot() {
        _itemIconImage.gameObject.SetActive(false);
        _itemAmountText.text = string.Empty;
        _itemAmountBackgroundImage.gameObject.SetActive(false);
    }
}
