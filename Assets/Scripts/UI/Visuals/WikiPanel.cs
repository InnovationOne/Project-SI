using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This script handels the collection panel
public class WikiPanel : MonoBehaviour {
    public static WikiPanel Instance { get; private set; }

    [Header("Category Buttons")]
    [SerializeField] private Button _foodCategoryButton;
    [SerializeField] private Image _foodCategorySelectedImage;
    [SerializeField] private Button _toolCategoryButton;
    [SerializeField] private Image _toolCategorySelectedImage;
    [SerializeField] private Button _plantCategoryButton;
    [SerializeField] private Image _plantCategorySelectedImage;
    [SerializeField] private Button _fishCategoryButton;
    [SerializeField] private Image _fishCategorySelectedImage;
    [SerializeField] private Button _insectCategoryButton;
    [SerializeField] private Image _insectCategorySelectedImage;
    [SerializeField] private Button _letterCategoryButton;
    [SerializeField] private Image _letterCategorySelectedImage;
    [SerializeField] private Button _fossilCategoryButton;
    [SerializeField] private Image _fossilCategorySelectedImage;
    [SerializeField] private Button _mineralCategoryButton;
    [SerializeField] private Image _mineralCategorySelectedImage;
    [SerializeField] private Button _achievementCategoryButton;
    [SerializeField] private Image _achievementCategorySelectedImage;

    [Header("Item Category Group")]
    [SerializeField] private Transform _wikiContent;
    [SerializeField] private WikiButton _wikiButtonPrefab;

    [Header("Book")]
    [SerializeField] private TextMeshProUGUI _itemName;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private Image _toolRarityImage;
    [SerializeField] private TextMeshProUGUI _itemDescription;
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Transform _museumCheck;
    [SerializeField] private Transform _museumUncheck;
    [SerializeField] private Transform _museumBox;
    [SerializeField] private Transform _museumText;
    [SerializeField] private Button _searchboxButton;

    [Header("Searchbox")]
    [SerializeField] private Transform _searchbox;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Transform _searchboxContent;
    [SerializeField] private SearchboxResult _searchboxResultPrefab;
    [SerializeField] private Button _searchboxCloseButton;


    private WikiTypes _lastWikiType = WikiTypes.Food;
    private int _lastWikiItemID;
    private ItemDatabaseSO _itemDatabase;

    public WikiContainerSO WikiContainer;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of WikiPanel in the scene!");
            return;
        }
        Instance = this;

        _foodCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.Food));
        _toolCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.ToolAndCraft));
        _plantCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.PlantAndSeed));
        _fishCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.Fish));
        _insectCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.Insect));
        _letterCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.Letter));
        _fossilCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.Fossil));
        _mineralCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.Mineral));
        _achievementCategoryButton.onClick.AddListener(() => ShowWikiType(WikiTypes.Achievement));

        _previousButton.onClick.AddListener(() => {
            int currentIndex = WikiContainer.Items.FindIndex(item => item.ItemID == _lastWikiItemID);
            
            while (currentIndex > 0) {
                currentIndex--;

                if (WikiContainer.Items[currentIndex].WikiType == _lastWikiType) {
                    ShowDetailedItem(WikiContainer.Items[currentIndex]);
                    break;
                } else {
                    break;
                }
            }
        });
        _nextButton.onClick.AddListener(() => {
            int currentIndex = WikiContainer.Items.FindIndex(item => item.ItemID == _lastWikiItemID);

            while (currentIndex < WikiContainer.Items.Count) {
                currentIndex++;

                if (WikiContainer.Items[currentIndex].WikiType == _lastWikiType) {
                    ShowDetailedItem(WikiContainer.Items[currentIndex]);
                    break;
                } else {
                    break;
                }
            }
        });

        _searchboxButton.onClick.AddListener(() => ToggleSearchbox());
        _searchboxCloseButton.onClick.AddListener(() => ToggleSearchbox());
        _inputField.onValueChanged.AddListener(delegate { ReadInputField(); });
    }

    private void Start() {
        _itemDatabase = ItemManager.Instance.ItemDatabase;

        ShowWikiType(_lastWikiType);
        ShowDetailedItem(_itemDatabase.Items[_lastWikiItemID]);

        _searchbox.gameObject.SetActive(false);
    }

    private void ShowWikiType(WikiTypes wikiType) {
        foreach (Transform child in _wikiContent) {
            Destroy(child.gameObject);
        }

        SetSelectedImage(wikiType);
        foreach (ItemSO itemSO in WikiContainer.Items) {
            if (itemSO.WikiType == wikiType) {
                WikiButton wikiButton = Instantiate(_wikiButtonPrefab, _wikiContent);
                wikiButton.SetIndex(itemSO.ItemID);

                if (itemSO.ItemType == ItemTypes.Tools) {
                    wikiButton.SetItemImage(itemSO.ItemIcon, itemSO.ToolItemRarity[^1]);
                } else {
                    wikiButton.SetItemImage(itemSO.ItemIcon, null);
                }
            }
        }

        _lastWikiType = wikiType;
    }

    private void SetSelectedImage(WikiTypes wikiType) {
        bool[] selectedImages = new bool[9];

        switch (wikiType) {
            case WikiTypes.Food:
                selectedImages[0] = true;
                break;
            case WikiTypes.ToolAndCraft:
                selectedImages[1] = true;
                break;
            case WikiTypes.PlantAndSeed:
                selectedImages[2] = true;
                break;
            case WikiTypes.Fish:
                selectedImages[3] = true;
                break;
            case WikiTypes.Insect:
                selectedImages[4] = true;
                break;
            case WikiTypes.Letter:
                selectedImages[5] = true;
                break;
            case WikiTypes.Fossil:
                selectedImages[6] = true;
                break;
            case WikiTypes.Mineral:
                selectedImages[7] = true;
                break;
            case WikiTypes.Achievement:
                selectedImages[8] = true;
                break;
        }

        _foodCategorySelectedImage.gameObject.SetActive(selectedImages[0]);
        _toolCategorySelectedImage.gameObject.SetActive(selectedImages[1]);
        _plantCategorySelectedImage.gameObject.SetActive(selectedImages[2]);
        _fishCategorySelectedImage.gameObject.SetActive(selectedImages[3]);
        _insectCategorySelectedImage.gameObject.SetActive(selectedImages[4]);
        _letterCategorySelectedImage.gameObject.SetActive(selectedImages[5]);
        _fossilCategorySelectedImage.gameObject.SetActive(selectedImages[6]);
        _mineralCategorySelectedImage.gameObject.SetActive(selectedImages[7]);
        _achievementCategorySelectedImage.gameObject.SetActive(selectedImages[8]);
    }

    private void ShowDetailedItem(ItemSO itemSO) {
        _itemName.text = itemSO.ItemName;
        _itemIcon.sprite = itemSO.ItemIcon;
        if (itemSO.ItemType == ItemTypes.Tools) {
            _toolRarityImage.gameObject.SetActive(true);
            _toolRarityImage.sprite = itemSO.ToolItemRarity[^1];
        } else {
            _toolRarityImage.gameObject.SetActive(false);
        }

        _itemDescription.text = itemSO.FullDescription;
        _itemDescription.rectTransform.sizeDelta = new Vector2(_itemDescription.rectTransform.sizeDelta.x, _itemDescription.preferredHeight);

        /*
        if (itemSO.canBeMuseum) {
            _museumCheck.gameObject.SetActive(false);
            _museumUncheck.gameObject.SetActive(true);
            _museumBox.gameObject.SetActive(true);
            _museumText.gameObject.SetActive(true);


            foreach (ItemSO museumItem in _museumContainer.Items) {
                if (itemSO.itemID == museumItem.itemID) {
                    _museumCheck.gameObject.SetActive(true);
                    _museumUncheck.gameObject.SetActive(false);
                    break;
                }
            }
        } else {
            _museumUncheck.gameObject.SetActive(false);
            _museumBox.gameObject.SetActive(false);
            _museumText.gameObject.SetActive(false);
        }
        */

        _lastWikiItemID = itemSO.ItemID;
    }

    public void OnLeftClick(int buttonID) {
        ShowDetailedItem(_itemDatabase.GetItem(buttonID));
        _lastWikiItemID = buttonID;
    }

    private void ToggleSearchbox() {
        _searchbox.gameObject.SetActive(!_searchbox.gameObject.activeSelf);
        _inputField.text = string.Empty;
        ReadInputField();
    }

    private void ReadInputField() {
        foreach (Transform child in _searchboxContent) {
            Destroy(child.gameObject);
        }

        foreach (ItemSO itemSO in WikiContainer.Items) {
            if (itemSO.ItemName.ToLower().Contains(_inputField.text.ToLower())) {
                SearchboxResult setSearchboxResultButton = Instantiate(_searchboxResultPrefab, _searchboxContent);
                setSearchboxResultButton.SetSearchboxResultButton(itemSO.ItemID, itemSO.ItemName);
            }
        }
    }

    public void ResultPressed(int itemID) {
        _searchbox.gameObject.SetActive(false);
        ShowItemInWiki(itemID);
    }

    public void ShowItemInWiki(int itemID) {
        ShowWikiType(_itemDatabase.Items[itemID].WikiType);
        ShowDetailedItem(_itemDatabase.Items[itemID]);
    }
}
