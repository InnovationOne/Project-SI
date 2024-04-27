using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragItemPanel : MonoBehaviour {
    public static DragItemPanel Instance { get; private set; }

    [SerializeField] private Image _itemIconImage;
    [SerializeField] private Image _toolRarityImage;
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

    public void SetItemSlot(ItemSlot itemSlot, Sprite raritySprite) {
        _itemIconImage.gameObject.SetActive(true);
        _itemIconImage.sprite = itemSlot.Item.ItemIcon;

        if (itemSlot.Item.IsStackable) {
            _itemAmountBackgroundImage.gameObject.SetActive(true);
            _itemAmountText.text = itemSlot.Amount.ToString();
        } else {
            _itemAmountBackgroundImage.gameObject.SetActive(false);
            _itemAmountText.text = string.Empty;
        }

        if (raritySprite != null) {
            if (itemSlot.Item.ItemType == ItemSO.ItemTypes.Tools) {
                _toolRarityImage.gameObject.SetActive(true);
                _toolRarityImage.sprite = raritySprite;
            } else {
                _toolRarityImage.gameObject.SetActive(false);
            }

        } else {
            _toolRarityImage.gameObject.SetActive(false);
        }
    }

    public void ClearItemSlot() {
        _itemIconImage.gameObject.SetActive(false);
        _itemAmountText.text = string.Empty;
        _itemAmountBackgroundImage.gameObject.SetActive(false);
        _toolRarityImage.sprite = null;
        _toolRarityImage.gameObject.SetActive(false);
    }
}
