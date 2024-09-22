using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ItemContainerUI : MonoBehaviour {
    [Header("Params")]
    public ItemContainerSO ItemContainer;
    public Button[] ItemButtons;
    public Sprite[] RaritySprites;

    [Header("Right click menu")]
    [SerializeField] private RectTransform _rightClickMenu;
    [SerializeField] private Slider _splitAmountSlider;
    [SerializeField] private TextMeshProUGUI _splitAmountSliderText;
    [SerializeField] private Button _splitButton;
    [SerializeField] private Button _wikiButton;

    private int _buttonIndex = -1;
    private bool _showRightClickMenu = false;


    [Header("Item info")]
    [SerializeField] private RectTransform _itemInfo;
    [SerializeField] private RectTransform _itemNameHeader;
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _itemInfoText;

    private const int QUEST_BODY_HIGHT_CORRECTURE = 6;
    private const int QUEST_PROGRESS_BAR_HIGHT = 1;
    private const float TIME_TO_SHOW_ITEM_INFO = 0.75f;

    private ItemSlot _itemSlotForShowInfo;
    private bool _showInfo = false;
    private float _currentTime = 0f;
    

    public void ItemContainerPanelAwake() {
        if (_splitButton != null) {
            _splitButton.onClick.AddListener(() => SplitItem());
        }
        if (_wikiButton != null) {
            _wikiButton.onClick.AddListener(() => ShowItemInWiki());
        }
        if (_rightClickMenu != null) {
            _rightClickMenu.gameObject.SetActive(false);
        }
        if (_itemInfo != null) {
            _itemInfo.gameObject.SetActive(false);
        }
    }
    
    private void Start() {
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;

        Init();
    }
    
    private void Update() {
        if (_showInfo) {
            ShowItemInfo();
        } 
        if (_showRightClickMenu) {
            _splitAmountSliderText.text = _splitAmountSlider.value.ToString();
            if (_showInfo) {
                HideItemInfo();
            }
        } 
        if (DragItemUI.Instance.gameObject.activeSelf) {
            HideItemInfo();
            HideRightClickMenu();
        }
    }

    public void Init() {
        //Set the button index on the button
        for (int i = 0; i < ItemContainer.ItemSlots .Count && i < ItemButtons.Length; i++) {
            ItemButtons[i].GetComponent<BackpackButton>().SetButtonIndex(i);
        }

        ShowUIButtonContains();
    }

    public void ShowUIButtonContains() {
        //Set the buttons with the item slots of the container
        for (int i = 0; i < ItemContainer.ItemSlots.Count && i < ItemButtons.Length; i++) {
            //If the slot has no item, clear the button
            if (ItemContainer.ItemSlots[i].ItemId == -1) {
                ItemButtons[i].GetComponent<BackpackButton>().ClearItemSlot();
            } else {
                ItemButtons[i].GetComponent<BackpackButton>().SetItemSlot(ItemContainer.ItemSlots[i], RaritySprites);
            }
        }
    }

    #region RightClickMenu
    public void ShowRightClickMenu(int buttonIndex, Vector3 position) {
        if (_rightClickMenu == null || _splitAmountSlider == null || _splitButton == null || _splitAmountSliderText == null || ItemContainer.ItemSlots[buttonIndex].ItemId == -1) {
            return;
        }

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
        int value = ItemContainer.ItemSlots[buttonIndex].Amount / 2;
        _splitAmountSlider.value = value;
        _rightClickMenu.gameObject.SetActive(true);

        _buttonIndex = buttonIndex;
    }

    public void HideRightClickMenu() {
        if (_rightClickMenu == null || _splitAmountSlider == null || _splitButton == null || _splitAmountSliderText == null) {
            return;
        }

        _rightClickMenu.gameObject.SetActive(false);

        _buttonIndex = -1;
    }

    private void ShowItemInWiki() {
        if (_buttonIndex >= 0) {
            //PlayerWikiController.LocalInstance.ShowItemInWiki(ItemContainer.ItemSlots[_buttonIndex].Item.ItemID);
        } else {
            Debug.LogWarning("No valid button index was given!");
        }

        HideRightClickMenu();
    }

    private void SplitItem() {
        if (_buttonIndex >= 0) {
            if ((int)_splitAmountSlider.value > 0) {
                ItemSlot newItemSlot = new();
                newItemSlot.Set(new ItemSlot(
                    ItemContainer.ItemSlots[_buttonIndex].ItemId, 
                    (int)_splitAmountSlider.value, 
                    ItemContainer.ItemSlots[_buttonIndex].RarityId));
                PlayerItemDragAndDropController.LocalInstance.OnLeftClick(newItemSlot);

                // When all items are "Splitted" clear the item slot
                if ((int)_splitAmountSlider.maxValue == (int)_splitAmountSlider.value) {
                    ItemContainer.ItemSlots[_buttonIndex].Clear();
                } else {
                    ItemContainer.ItemSlots[_buttonIndex].Amount = (int)_splitAmountSlider.maxValue - (int)_splitAmountSlider.value;
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
    #endregion


    #region Item Info
    public void TriggerItemInfo(ItemSlot itemSlot) {
        if (_itemInfo == null || _itemNameText == null || _itemInfoText == null || itemSlot == null) {
            return;
        }

        _showInfo = true;
        _itemSlotForShowInfo = itemSlot;
    }

    public void HideItemInfo() {
        if (_itemInfo == null || _itemNameText == null || _itemInfoText == null || _itemSlotForShowInfo == null) {
            return;
        }

        _itemInfo.gameObject.SetActive(false);
        _currentTime = 0f;
        _showInfo = false;
        _itemSlotForShowInfo = null;
    }

    private void ShowItemInfo() {
        if (_itemInfo == null || _itemNameText == null || _itemInfoText == null || _itemSlotForShowInfo == null) {
            return;
        }

        if (_currentTime >= TIME_TO_SHOW_ITEM_INFO && !_itemInfo.gameObject.activeSelf) {
            _itemNameText.text = ItemManager.Instance.ItemDatabase[_itemSlotForShowInfo.ItemId].ItemName;

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

            _itemInfoText.text = itemSlotInfoStringBuilder.ToString();
            SetItemInfoNewSize();
            SetItemInfoPosition();
            _itemInfo.gameObject.SetActive(true);
        } else if (_itemInfo.gameObject.activeSelf) {
            SetItemInfoPosition();
        } else {
            _currentTime += Time.deltaTime;
        }
    }

    private void SetItemInfoNewSize() {
        _itemInfo.sizeDelta = new Vector2(_itemInfo.sizeDelta.x,
            _itemNameHeader.sizeDelta.y + _itemInfoText.preferredHeight + QUEST_BODY_HIGHT_CORRECTURE + QUEST_PROGRESS_BAR_HIGHT);
    }

    private void SetItemInfoPosition() {
        int scalerWidth = Screen.width / 640;
        int scalerHeight = Screen.height / 360;
        int xValue, yValue;
        
        // Right
        if (_itemInfo.position.x / scalerWidth + _itemInfo.sizeDelta.x > GetComponentInParent<Canvas>().GetComponent<RectTransform>().sizeDelta.x) {
            xValue = 1;
        } else {
            xValue = 0;
        }
        
        // Bottom
        if (_itemInfo.position.y / scalerHeight - _itemInfo.sizeDelta.y < 0) {
            yValue = 0;
        } else {
            yValue = 1;
        }

        if (xValue == 1) {
            _itemInfo.position = Input.mousePosition + new Vector3(-5, 5);
        } else {
            _itemInfo.position = Input.mousePosition + new Vector3(15, -15);
        }

        _itemInfo.anchorMin = new Vector2(xValue, yValue);
        _itemInfo.anchorMax = new Vector2(xValue, yValue);
        _itemInfo.pivot = new Vector2(xValue, yValue);
    }
    #endregion


    public virtual void OnPlayerLeftClick(int buttonIndex) { }

    public virtual void OnPlayerRightClick(int buttonIndex) { }
}
