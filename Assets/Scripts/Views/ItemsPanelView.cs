using System;
using System.Collections.Generic;
using InventorySystem.Enums;
using UnityEngine;
using UnityEngine.UI;

public class ItemsPanelView : MonoBehaviour
{
    [Header("Item Buttons")]
    [SerializeField]
    private Button _keyButton;
    
    [SerializeField]
    private Button _moneyButton;
    
    [SerializeField]
    private Button _appleButton;

    private ItemPanelMode _currentMode;
    private Action<ItemType> _onItemSelected;

    private void Start()
    {
        SetupButtonListeners();
        Hide();
    }

    public void ShowPanel(ItemPanelMode mode, List<ItemType> availableItems, Action<ItemType> onItemSelected)
    {
        _currentMode = mode;
        _onItemSelected = onItemSelected;

        ConfigureButtons(availableItems);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _onItemSelected = null;
    }

    private void SetupButtonListeners()
    {
        if (_keyButton != null)
        {
            _keyButton.onClick.AddListener(() => OnItemButtonClicked(ItemType.Key));
        }

        if (_moneyButton != null)
        {
            _moneyButton.onClick.AddListener(() => OnItemButtonClicked(ItemType.Money));
        }

        if (_appleButton != null)
        {
            _appleButton.onClick.AddListener(() => OnItemButtonClicked(ItemType.Apple));
        }
    }

    private void ConfigureButtons(List<ItemType> availableItems)
    {
        ConfigureButton(_keyButton, availableItems.Contains(ItemType.Key));
        ConfigureButton(_moneyButton, availableItems.Contains(ItemType.Money));
        ConfigureButton(_appleButton, availableItems.Contains(ItemType.Apple));
    }

    private void ConfigureButton(Button button, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(isActive);
    }

    private void OnItemButtonClicked(ItemType itemType)
    {
        if (_onItemSelected == null)
        {
            return;
        }

        _onItemSelected.Invoke(itemType);
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    private void RemoveButtonListeners()
    {
        if (_keyButton != null)
        {
            _keyButton.onClick.RemoveAllListeners();
        }

        if (_moneyButton != null)
        {
            _moneyButton.onClick.RemoveAllListeners();
        }

        if (_appleButton != null)
        {
            _appleButton.onClick.RemoveAllListeners();
        }
    }
}
