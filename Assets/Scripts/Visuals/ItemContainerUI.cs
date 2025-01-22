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
    [SerializeField] RectTransform _rightClickMenu;
    [SerializeField] Slider _splitAmountSlider;
    [SerializeField] TextMeshProUGUI _splitAmountSliderText;
    [SerializeField] Button _splitButton;
    [SerializeField] Button _wikiButton;

    int _buttonIndex = -1;
    bool _showRightClickMenu = false;


    [Header("Item info")]
    [SerializeField] RectTransform _itemInfo;
    [SerializeField] RectTransform _itemNameHeader;
    [SerializeField] TextMeshProUGUI _itemNameText;
    [SerializeField] TextMeshProUGUI _itemInfoText;

    const int QUEST_BODY_HIGHT_CORRECTURE = 6;
    const int QUEST_PROGRESS_BAR_HIGHT = 1;
    const float TIME_TO_SHOW_ITEM_INFO = 0.75f;

    ItemSlot _itemSlotForShowInfo;
    bool _showInfo = false;
    float _currentTime = 0f;

    // Initializes UI listeners and hides menus by default.
    public void ItemContainerUIAwake() {
        if (_splitButton != null) _splitButton.onClick.AddListener(() => SplitItem());
        if (_wikiButton != null) _wikiButton.onClick.AddListener(() => ShowItemInWiki());
        if (_rightClickMenu != null) _rightClickMenu.gameObject.SetActive(false);
        if (_itemInfo != null) _itemInfo.gameObject.SetActive(false);

    }

    // Subscribes to the OnItemsUpdated event to refresh UI.
    void Start() {
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        Init();
    }

    void Update() {
        // Handle delayed display of item info
        if (_showInfo) {
            ShowItemInfo();
        }

        // Handle right-click menu and splitting updates
        if (_showRightClickMenu) {
            _splitAmountSliderText.text = _splitAmountSlider.value.ToString();
            if (_showInfo) {
                HideItemInfo();
            }
        }

        // Hides info panels while dragging items.
        if (DragItemUI.Instance.gameObject.activeSelf) {
            HideItemInfo();
            HideRightClickMenu();
        }
    }

    // Sets up slot indices and shows initial item data.
    public void Init() {
        for (int i = 0; i < ItemContainer.ItemSlots.Count && i < ItemButtons.Length; i++) {
            ItemButtons[i].SetButtonIndex(i);
        }

        ShowUIButtonContains();
    }

    // Updates the UI buttons to match the item slots in the container.
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
    // Displays the right-click menu for item splitting, wiki info, etc.
    public void ShowRightClickMenu(int buttonIndex, Vector3 position) {
        if (ItemContainer.ItemSlots[buttonIndex].IsEmpty) return;

        // Toggles the menu if already open on the same button.
        if (_rightClickMenu.gameObject.activeSelf && buttonIndex == _buttonIndex) {
            HideRightClickMenu();
            _showRightClickMenu = false;
            return;
        }

        // Adjust position based on screen scaling
        int scalerWidth = Screen.width / 640;
        int scalerHeight = Screen.height / 360;

        _rightClickMenu.position = position + new Vector3(scalerWidth * 13, scalerHeight * 13);

        _showRightClickMenu = true;
        _splitAmountSlider.maxValue = ItemContainer.ItemSlots[buttonIndex].Amount;
        _splitAmountSlider.value = ItemContainer.ItemSlots[buttonIndex].Amount / 2;
        _rightClickMenu.gameObject.SetActive(true);

        _buttonIndex = buttonIndex;
    }

    // Hides the right-click context menu.
    public void HideRightClickMenu() {
        _rightClickMenu.gameObject.SetActive(false);
        _buttonIndex = -1;
    }

    // TODO: Opens wiki data about the current selected item.
    void ShowItemInWiki() {
        if (_buttonIndex >= 0) {
            //PlayerWikiController.LocalInstance.ShowItemInWiki(ItemContainer.ItemSlots[_buttonIndex].Item.ItemID);
        } else {
            Debug.LogWarning("No valid button index was given!");
        }

        HideRightClickMenu();
    }

    // Splits an item stack into two stacks and places the newly split stack on the cursor.
    void SplitItem() {
        if (_buttonIndex >= 0) {
            int splitValue = (int)_splitAmountSlider.value;
            if (splitValue > 0) {
                var originalSlot = ItemContainer.ItemSlots[_buttonIndex];
                var newItemSlot = new ItemSlot(originalSlot.ItemId, splitValue, originalSlot.RarityId);

                // Call the drag controller to pick up the newly split stack
                PlayerController.LocalInstance.PlayerItemDragAndDropController.OnLeftClick(newItemSlot);

                // Clear or update the original slot
                if ((int)_splitAmountSlider.maxValue == splitValue) {
                    originalSlot.Clear();
                } else {
                    int newAmount = (int)_splitAmountSlider.maxValue - splitValue;
                    originalSlot.Set(new ItemSlot(originalSlot.ItemId, newAmount, originalSlot.RarityId));
                }

                ShowUIButtonContains();
            } else {
                return;
            }
        } else {
            Debug.LogWarning("No valid button index was set!");
        }

        HideRightClickMenu();
    }
    #endregion -------------------- Right Click Menu --------------------


    #region -------------------- Item Info --------------------
    // Triggers delayed display of item information.
    public void TriggerItemInfo(ItemSlot itemSlot) {
        if (itemSlot == null) return;

        _showInfo = true;
        _itemSlotForShowInfo = itemSlot;
    }

    // Hides the item info panel immediately.
    public void HideItemInfo() {
        if (_itemSlotForShowInfo == null) return;

        _itemInfo.gameObject.SetActive(false);
        _currentTime = 0f;
        _showInfo = false;
        _itemSlotForShowInfo = null;
    }

    // TODO: Displays item info after a short delay.
    void ShowItemInfo() {
        if (_itemSlotForShowInfo == null) return;

        // Show panel once the time threshold is reached
        if (_currentTime >= TIME_TO_SHOW_ITEM_INFO && !_itemInfo.gameObject.activeSelf) {
            _itemNameText.text = GameManager.Instance.ItemManager.ItemDatabase[_itemSlotForShowInfo.ItemId].ItemName;

            StringBuilder itemSlotInfoStringBuilder = new();
            //int rarityOffset = 0;
            if (_itemSlotForShowInfo.RarityId > 0) {
                //rarityOffset = 1;
            }

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
            }
            */

            // Assign text to the UI
            _itemInfoText.text = itemSlotInfoStringBuilder.ToString();
            SetItemInfoNewSize();
            SetItemInfoPosition();
            _itemInfo.gameObject.SetActive(true);
        } else if (_itemInfo.gameObject.activeSelf) {
            // Update position if already visible
            SetItemInfoPosition();
        } else {
            // Accumulate time until threshold
            _currentTime += Time.deltaTime;
        }
    }

    // Adjusts the size of the item info panel to fit text and headers.
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
        else             _itemInfo.position = Input.mousePosition + new Vector3(15, -15);

        _itemInfo.anchorMin = new Vector2(xValue, yValue);
        _itemInfo.anchorMax = new Vector2(xValue, yValue);
        _itemInfo.pivot = new Vector2(xValue, yValue);
    }
    #endregion -------------------- Item Info --------------------

    // Override this to customize behavior for left-clicks on item slots.
    public virtual void OnPlayerLeftClick(int buttonIndex) { }

    // Override this to customize behavior for right-clicks on item slots.
    public virtual void OnPlayerRightClick(int buttonIndex) { }
}
