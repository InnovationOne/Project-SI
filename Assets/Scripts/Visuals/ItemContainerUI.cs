using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ItemContainerUI : MonoBehaviour {
    [Header("Params")]
    [SerializeField] protected ItemContainerSO ItemContainer;
    [SerializeField] protected InventorySlot[] ItemButtons;
    [SerializeField] protected Sprite[] RaritySprites;

    [Header("Right click menu")]
    [SerializeField] bool _hasRightClickMenu;
    [ConditionalHide("_hasRightClickMenu", true)]
    [SerializeField] RectTransform _rightClickMenu;
    [ConditionalHide("_hasRightClickMenu", true)]
    [SerializeField] Slider _splitAmountSlider;
    [ConditionalHide("_hasRightClickMenu", true)]
    [SerializeField] TextMeshProUGUI _splitAmountSliderText;
    [ConditionalHide("_hasRightClickMenu", true)]
    [SerializeField] Button _splitButton;
    [ConditionalHide("_hasRightClickMenu", true)]
    [SerializeField] Button _wikiButton;

    int _buttonIndex = -1;
    bool _showRightClickMenu = false;


    [Header("Item info")]
    [SerializeField] bool _hasItemInfo;
    [ConditionalHide("_hasItemInfo", true)]
    [SerializeField] RectTransform _itemInfo;
    [ConditionalHide("_hasItemInfo", true)]
    [SerializeField] RectTransform _itemNameHeader;
    [ConditionalHide("_hasItemInfo", true)]
    [SerializeField] TextMeshProUGUI _itemNameText;
    [ConditionalHide("_hasItemInfo", true)]
    [SerializeField] TextMeshProUGUI _itemInfoText;

    const int QUEST_BODY_HIGHT_CORRECTURE = 6;
    const int QUEST_PROGRESS_BAR_HIGHT = 1;
    const float TIME_TO_SHOW_ITEM_INFO = 0.75f;

    ItemSlot _itemSlotForShowInfo;
    bool _showInfo = false;
    float _currentTime = 0f;

    #region -------------------- Unity Lifecycle --------------------

    // Initializes UI listeners and hides menus.
    public void ItemContainerUIAwake() {
        if (_splitButton != null) _splitButton.onClick.AddListener(() => SplitItem());
        if (_wikiButton != null) _wikiButton.onClick.AddListener(() => ShowItemInWiki());
        if (_rightClickMenu != null) _rightClickMenu.gameObject.SetActive(false);
        if (_itemInfo != null) _itemInfo.gameObject.SetActive(false);

    }

    void Start() {
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        Init();
    }

    void Update() {
        if (_showInfo) ShowItemInfo();
        if (_showRightClickMenu) {
            _splitAmountSliderText.text = _splitAmountSlider.value.ToString();
            if (_showInfo) HideItemInfo();
        }

        if (DragItemUI.Instance.gameObject.activeSelf) {
            HideItemInfo();
            HideRightClickMenu();
        }
    }

    #endregion -------------------- Unity Lifecycle --------------------

    // Initializes button indices and refreshes display.
    public void Init() {
        for (int i = 0; i < ItemContainer.ItemSlots.Count && i < ItemButtons.Length; i++) {
            ItemButtons[i].SetButtonIndex(i);
        }
        ShowUIButtonContains();
    }

    // Updates each button to match the corresponding item slot.
    public void ShowUIButtonContains() {
        for (int i = 0; i < ItemContainer.ItemSlots.Count && i < ItemButtons.Length; i++) {
            if (ItemContainer.ItemSlots[i].ItemId == -1) {
                ItemButtons[i].ClearItemSlot();
            } else {
                ItemButtons[i].SetItemSlot(ItemContainer.ItemSlots[i], RaritySprites);
            }
        }
    }

    #region -------------------- Right Click Menu --------------------

    // Displays the right-click menu if valid, else hides it.
    public void ShowRightClickMenu(int buttonIndex, Vector3 position) {
        if (_rightClickMenu == null || ItemContainer.ItemSlots[buttonIndex].IsEmpty) return;
        if (_rightClickMenu.gameObject.activeSelf && buttonIndex == _buttonIndex) {
            HideRightClickMenu();
            _showRightClickMenu = false;
            return;
        }

        int scalerWidth = Screen.width / 640;
        int scalerHeight = Screen.height / 360;
        _rightClickMenu.position = position + new Vector3(scalerWidth * 13, scalerHeight * 13);
        _showRightClickMenu = true;
        _splitAmountSlider.maxValue = ItemContainer.ItemSlots[buttonIndex].Amount;
        _splitAmountSlider.value = ItemContainer.ItemSlots[buttonIndex].Amount / 2;
        _rightClickMenu.gameObject.SetActive(true);
        _buttonIndex = buttonIndex;
    }

    // Hides the right-click menu.
    public void HideRightClickMenu() {
        if (_rightClickMenu == null) return;
        _rightClickMenu.gameObject.SetActive(false);
        _buttonIndex = -1;
    }

    // Opens a wiki interface for the selected item (placeholder).
    void ShowItemInWiki() {
        if (_buttonIndex >= 0) {
            // TODO Implement wiki display if available, e.g.:
            //PlayerWikiController.LocalInstance.ShowItemInWiki(ItemContainer.ItemSlots[_buttonIndex].Item.ItemID);
        } else {
            Debug.LogWarning("No valid button index was given!");
        }
        HideRightClickMenu();
    }

    // Splits an item stack into two, placing the new stack on the cursor.
    void SplitItem() {
        if (_buttonIndex < 0) return;

        int splitValue = (int)_splitAmountSlider.value;
        if (splitValue <= 0) return;

        var originalSlot = ItemContainer.ItemSlots[_buttonIndex];
        var newItemSlot = new ItemSlot(originalSlot.ItemId, splitValue, originalSlot.RarityId);
        PlayerController.LocalInstance.PlayerItemDragAndDropController.OnLeftClick(newItemSlot);

        if ((int)_splitAmountSlider.maxValue == splitValue) originalSlot.Clear();
        else {
            int newAmount = (int)_splitAmountSlider.maxValue - splitValue;
            originalSlot.Set(new ItemSlot(originalSlot.ItemId, newAmount, originalSlot.RarityId));
        }

        ShowUIButtonContains();
        HideRightClickMenu();
    }
    #endregion -------------------- Right Click Menu --------------------


    #region -------------------- Item Info --------------------

    // Delays item info display.
    public void TriggerItemInfo(ItemSlot itemSlot) {
        if (itemSlot == null || _itemInfo == null) return;
        _showInfo = true;
        _itemSlotForShowInfo = itemSlot;
    }

    // Hides the item info UI.
    public void HideItemInfo() {
        if (_itemSlotForShowInfo == null || _itemInfo == null) return;
        _itemInfo.gameObject.SetActive(false);
        _currentTime = 0f;
        _showInfo = false;
        _itemSlotForShowInfo = null;
    }

    // Shows item info after a short delay.
    void ShowItemInfo() {
        if (_itemSlotForShowInfo == null || _itemInfo == null) return;
        if (_currentTime >= TIME_TO_SHOW_ITEM_INFO && !_itemInfo.gameObject.activeSelf) {
            _itemNameText.text = GameManager.Instance.ItemManager.ItemDatabase[_itemSlotForShowInfo.ItemId].ItemName;

            StringBuilder itemSlotInfoStringBuilder = new();
            //int rarityOffset = 0;
            if (_itemSlotForShowInfo.RarityId > 0) {
                //rarityOffset = 1;
            }

            // TODO Add restore HP or sell price info
            // TODO Display a side-by-side comparison with currently equipped items (e.g., for weapons, armor) to help players make quick decisions on upgrades.
            /*
            if (_itemSlotForShowInfo.Item.CanRestoreHpOrEnergy) {
                itemSlotInfoStringBuilder.Append("<color=#992e2e>" + "+" + (int)(_itemSlotForShowInfo.Item.LowestRarityRestoringHpAmount *
                    _itemSlotForShowInfo.Item.ItemRarityScaler[_itemSlotForShowInfo.RarityID - rarityOffset]) + " HP" + "</color>" + " ");
                itemSlotInfoStringBuilder.Append("<color=#2e6c99>" + "+" + (int)(_itemSlotForShowInfo.Item.LowestRarityRestoringEnergyAmount *
                    _itemSlotForShowInfo.Item.ItemRarityScaler[_itemSlotForShowInfo.RarityID - rarityOffset]) + " Energy" + "</color>" + "\n");
            }
            if (_itemSlotForShowInfo.Item.CanBeSold) {
                itemSlotInfoStringBuilder.Append("<color=#2e6c99>" + "+" + (int)(_itemSlotForShowInfo.Item.LowestRaritySellPrice *
                    _itemSlotForShowInfo.Item.ItemRarityScaler[_itemSlotForShowInfo.RarityID - rarityOffset]) + " Gold" + "</color>");
            }*/


            // Assign text to the UI
            _itemInfoText.text = itemSlotInfoStringBuilder.ToString();

            SetItemInfoNewSize();
            SetItemInfoPosition();
            _itemInfo.gameObject.SetActive(true);
        } 
        else if (_itemInfo.gameObject.activeSelf) SetItemInfoPosition();
        else _currentTime += Time.deltaTime;
    }

    // Resizes info panel to fit text.
    void SetItemInfoNewSize() {
        _itemInfo.sizeDelta = new Vector2(
            _itemInfo.sizeDelta.x,
            _itemNameHeader.sizeDelta.y +
            _itemInfoText.preferredHeight +
            QUEST_BODY_HIGHT_CORRECTURE +
            QUEST_PROGRESS_BAR_HIGHT
        );
    }

    // Positions the item info panel near the mouse cursor without overflowing screen bounds.
    void SetItemInfoPosition() {
        int scalerWidth = Screen.width / 640;
        int scalerHeight = Screen.height / 360;

        // Determine anchor pivot based on screen edges
        int xValue = (_itemInfo.position.x / scalerWidth + _itemInfo.sizeDelta.x >
                      GetComponentInParent<Canvas>().GetComponent<RectTransform>().sizeDelta.x) ? 1 : 0;
        int yValue = (_itemInfo.position.y / scalerHeight - _itemInfo.sizeDelta.y < 0) ? 0 : 1;

        // Offset the info panel near the cursor
        if (xValue == 1) _itemInfo.position = Input.mousePosition + new Vector3(-5, 5);
        else _itemInfo.position = Input.mousePosition + new Vector3(15, -15);

        _itemInfo.anchorMin = new Vector2(xValue, yValue);
        _itemInfo.anchorMax = new Vector2(xValue, yValue);
        _itemInfo.pivot = new Vector2(xValue, yValue);
    }
    #endregion -------------------- Item Info --------------------

    // Override for handling left-click events.
    public virtual void OnPlayerLeftClick(int buttonIndex) { }

    // Override for handling right-click events.
    public virtual void OnPlayerRightClick(int buttonIndex) { }
}
