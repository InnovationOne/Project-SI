using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// This script handels the buttons in the collect panel
public class WikiButton : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private Image _itemImage;
    [SerializeField] private Image _toolRarityImage;

    private int buttonIndex;
    private WikiPanel wikiVisual;

    public void SetIndex(int index) {
        buttonIndex = index;
        wikiVisual = GetComponentInParent<WikiPanel>();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            wikiVisual.OnLeftClick(buttonIndex);
        }
    }

    public void SetItemImage(Sprite itemSprite, Sprite raritySprite) {
        _itemImage.sprite = itemSprite;

        if (raritySprite != null) {
            _toolRarityImage.gameObject.SetActive(true);
            _toolRarityImage.sprite = raritySprite;
        } else {
            _toolRarityImage.gameObject.SetActive(false);
        }
    }
}
