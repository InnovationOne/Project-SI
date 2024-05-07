using UnityEngine;

public class GroceryStore : Store, IDataPersistance {
    public static GroceryStore Instance { get; private set; }

    [Header("Grocery Store")]
    [SerializeField] private SpriteRenderer _groceryStoreVisual;
    [SerializeField] private SpriteRenderer _groceryStoreHighlight;

    [Header("Debugging")]
    [SerializeField] private int _groceryStoreLevel = 0;


    private void Awake() {
        Instance = this;

        _groceryStoreHighlight.gameObject.SetActive(false);
    }

    public void ShowGroceryStore() {
        _storeVisual.ClearContentBox();
        /*
        for (int i = 0; i < _storeContainer.Items.Count; i++) {
            // When the store is upgraded for the crops
            foreach (TimeAndWeatherManager.SeasonName seasonName in (_storeContainer[i] as SeedSO).CropToGrow.SeasonsToGrow) {
                if (seasonName == (TimeAndWeatherManager.SeasonName)TimeAndWeatherManager.Instance.CurrentSeason) {
                    // When the season is the current season
                    _storeVisual.SpawnStoreItemSlot(_storeContainer[i]);
                }
            }
        }
        */
    }

    public override void Interact(Player character) {
        // Store is not build yet
        if (_groceryStoreLevel == 0) {
            return;
        }

        ShowGroceryStore();

        InventoryMasterVisual.Instance.ToggleStorePanel();
    }


    #region Save & Load
    public void LoadData(GameData data) {
        _groceryStoreLevel = data.GroceryStoreLevel;
    }

    public void SaveData(GameData data) {
        data.GroceryStoreLevel = _groceryStoreLevel;
    }
    #endregion
}
