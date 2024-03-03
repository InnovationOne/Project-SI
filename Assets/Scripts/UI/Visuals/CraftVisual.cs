using UnityEngine;

public class CraftVisual : MonoBehaviour {
    public static CraftVisual Instance { get; private set; }

    [SerializeField] private CraftButton _buttonPrefab;
    [SerializeField] private Transform _content;

    private int _lastIndex = 0;


    private void Awake() {
        Instance = this;
    }

    public void SpawnCraftItemSlot(RecipeSO recipeSO) {
        CraftButton craftButton = Instantiate(_buttonPrefab, _content);

        craftButton.SetItemImage(recipeSO.ItemsToProduce[0].Item.ItemIcon);
        craftButton.SetIndex(_lastIndex);

        _lastIndex++;
    }

    // Remove all the spawned itemslots from the content list
    public void ClearContentBox() {
        foreach (Transform child in _content) {
            Destroy(child.gameObject);
        }

        _lastIndex = 0;
    }

    public void OnLeftClick(int buttonIndex) {
        PlayerCraftController.LocalInstance.OnLeftClick(buttonIndex);
    }
}
