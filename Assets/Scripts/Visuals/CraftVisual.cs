using UnityEngine;

public class CraftVisual : MonoBehaviour {
    public static CraftVisual Instance { get; private set; }

    [SerializeField] private CraftButton _buttonPrefab;
    [SerializeField] private Transform _content;

    private int _lastIndex = 0;


    private void Awake() {
        Instance = this;
    }

    public void SpawnCraftItemSlot(int recipeId) {
        CraftButton craftButton = Instantiate(_buttonPrefab, _content);

        craftButton.SetItemImage(GameManager.Instance.ItemManager.ItemDatabase[GameManager.Instance.RecipeManager.RecipeDatabase[recipeId].ItemsToProduce[0].ItemId].ItemIcon);
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
        PlayerController.LocalInstance.PlayerCraftController.OnLeftClick(buttonIndex);
    }
}
