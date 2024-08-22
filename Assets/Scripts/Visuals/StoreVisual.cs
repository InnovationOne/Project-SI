using UnityEngine;

public class StoreVisual : MonoBehaviour
{
    [SerializeField] private StoreButton _buttonPrefab;
    [SerializeField] private Transform _content;


    public void SpawnStoreItemSlot(ItemSO itemSO) {
        StoreButton storeButton = Instantiate(_buttonPrefab, _content);

        storeButton.SetItemImage(itemSO.ItemIcon, itemSO.BuyPrice);
        storeButton.SetIndex(itemSO);
    }

    // Remove all the spawned itemslots from the content list
    public void ClearContentBox() {
        foreach (Transform child in _content) {
            Destroy(child.gameObject);
        }
    }

    public void OnLeftClick(ItemSO itemSO) {
        //GroceryStore.Instance.OnLeftClick(itemSO);
    }
}
