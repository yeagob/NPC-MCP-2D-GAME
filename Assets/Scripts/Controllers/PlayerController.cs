using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatSystem.Characters;
using ChatSystem.Configuration.ScriptableObjects;
using Grid;
using Grid.Models.Grid;
using InventorySystem.Components;
using InventorySystem.Enums;
using MapSystem;
using MapSystem.Elements;
using MapSystem.Models.Map;
using PlayerSystem.Configuration;
using PlayerSystem.Enums;
using TMPro;
using UnityEngine;

public class PlayerController : TurnCharacter
{
    [SerializeField]
    private string _myName = "Santiago";
    
    [Header("Turn Configuration")]
    [SerializeField]
    private int _actionPoints = 3;
    
    [Header("Game References")]
    [SerializeField]
    private CharacterElement _characterElement;
    
    [SerializeField]
    private MapSystem.MapSystem _mapSystem;

    [SerializeField]
    private GridSystem _gridSystem;

    [SerializeField]
    private Camera _mainCamera;

    [Header("UI References")]
    [SerializeField]
    private ActionMenuView _actionMenu;
    
    [SerializeField] 
    private TextMeshProUGUI _dialogText;
        
    [SerializeField] 
    private GameObject _dialogObject;
    
    private bool _myTurn;
    private int _currentActionPoints;
    private PlayerActionState _currentActionState;
    private ItemType _selectedItemTypeForGive;
    private InventoryComponent _inventoryComponent;

    public override void Initialize()
    {
        ShowActions(false);
        HideDialog();
        
        _myTurn = false;
        _currentActionState = PlayerActionState.None;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        _inventoryComponent = _characterElement.GetComponent<InventoryComponent>();

        if (_inventoryComponent == null)
        {
            Debug.LogError("InventoryComponent not found on CharacterElement");
        }
    }

    public override async Task ExecuteTurn()
    {
        _currentActionPoints = _actionPoints;
        
        _myTurn = true;
        ShowActions(true);
        HideDialog();

        while (_currentActionPoints > 0 && _myTurn)
        {
            await Task.Yield();
        }
        
        _myTurn = false;
        ShowActions(false);
        _currentActionState = PlayerActionState.None;
    }

    private void Update()
    {
        if (!_myTurn)
        {
            return;
        }

        if (_currentActionState == PlayerActionState.None)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        ProcessMouseClick();
    }

    public void ExecuteMoveAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        _currentActionState = PlayerActionState.WaitingForMoveTarget;
        Debug.Log("Click on map to select movement destination");
    }

    public void ExecuteTalkAction(string message)
    {
        if (!CanExecuteAction())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("Cannot talk with empty message");
            return;
        }

        BroadcastMessageToNearbyCharacters(message);
        ConsumeActionPoint();
        _dialogText.text = message;
        ShowDialog();
    }

    public void ExecutePickupAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        GridCell currentCell = _characterElement.CurrentGridCell;
        List<ItemType> availableItems = GetAvailableItemsInCell(currentCell);

        if (availableItems.Count == 0)
        {
            Debug.LogWarning("No items available to pickup in this cell");
            return;
        }

        _actionMenu.ShowItemsPanel(ItemPanelMode.Pickup, availableItems, OnPickupItemSelected);
    }

    public void ExecuteDropAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        List<ItemType> inventoryItems = GetInventoryItems();

        if (inventoryItems.Count == 0)
        {
            Debug.LogWarning("No items in inventory to drop");
            return;
        }

        _actionMenu.ShowItemsPanel(ItemPanelMode.Drop, inventoryItems, OnDropItemSelected);
    }

    public void ExecuteGiveAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        List<ItemType> inventoryItems = GetInventoryItems();

        if (inventoryItems.Count == 0)
        {
            Debug.LogWarning("No items in inventory to give");
            return;
        }

        _actionMenu.ShowItemsPanel(ItemPanelMode.Give, inventoryItems, OnGiveItemSelected);
    }

    public void ExecuteHitAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        _currentActionState = PlayerActionState.WaitingForAttackTarget;
        Debug.Log("Click on a character to attack");
    }

    public int GetCurrentActionPoints()
    {
        return _currentActionPoints;
    }

    private void OnPickupItemSelected(ItemType itemType)
    {
        _actionMenu.HideItemsPanel();

        if (_inventoryComponent.HasItem(itemType))
        {
            Debug.LogWarning($"Cannot pickup {itemType}: already have one in inventory");
            return;
        }

        GridCell currentCell = _characterElement.CurrentGridCell;
        ItemElement itemElement = FindItemInCell(currentCell, itemType);

        if (itemElement == null)
        {
            Debug.LogWarning($"Item {itemType} not found in current cell");
            return;
        }

        bool added = _inventoryComponent.AddItem(itemElement.Id.ToString(), itemType, itemElement);

        if (!added)
        {
            Debug.LogWarning($"Failed to add {itemType} to inventory");
            return;
        }

        itemElement.gameObject.SetActive(false);
        _mapSystem.UnregisterElement(itemElement);

        ConsumeActionPoint();
        Debug.Log($"Picked up {itemType}");
    }

    private void OnDropItemSelected(ItemType itemType)
    {
        _actionMenu.HideItemsPanel();

        if (!_inventoryComponent.HasItem(itemType))
        {
            Debug.LogWarning($"Cannot drop {itemType}: not in inventory");
            return;
        }

        InventorySystem.Models.InventoryItem item = _inventoryComponent.GetItem(itemType);
        bool removed = _inventoryComponent.RemoveItem(itemType);

        if (!removed)
        {
            Debug.LogWarning($"Failed to remove {itemType} from inventory");
            return;
        }

        GridCell currentCell = _characterElement.CurrentGridCell;
        DropItemInCell(currentCell, item.itemElement);

        ConsumeActionPoint();
        Debug.Log($"Dropped {itemType}");
    }

    private void OnGiveItemSelected(ItemType itemType)
    {
        _actionMenu.HideItemsPanel();

        if (!_inventoryComponent.HasItem(itemType))
        {
            Debug.LogWarning($"Cannot give {itemType}: not in inventory");
            return;
        }

        _selectedItemTypeForGive = itemType;
        _currentActionState = PlayerActionState.WaitingForGiveTarget;
        Debug.Log($"Click on a character to give {itemType}");
    }

    private List<ItemType> GetAvailableItemsInCell(GridCell cell)
    {
        List<ItemType> availableItems = new List<ItemType>();
        MapCell mapCell = _mapSystem.GetMapCell(cell);

        if (mapCell == null)
        {
            return availableItems;
        }

        MapElement[] elements = mapCell.GetElementsByType(MapSystem.Enums.MapElementType.Item);

        foreach (MapElement element in elements)
        {
            ItemElement itemElement = element as ItemElement;

            if (itemElement == null)
            {
                continue;
            }

            if (!_inventoryComponent.HasItem(itemElement.ItemType))
            {
                availableItems.Add(itemElement.ItemType);
            }
        }

        return availableItems;
    }

    private List<ItemType> GetInventoryItems()
    {
        List<ItemType> items = new List<ItemType>();

        if (_inventoryComponent.HasItem(ItemType.Key))
        {
            items.Add(ItemType.Key);
        }

        if (_inventoryComponent.HasItem(ItemType.Money))
        {
            items.Add(ItemType.Money);
        }

        if (_inventoryComponent.HasItem(ItemType.Apple))
        {
            items.Add(ItemType.Apple);
        }

        return items;
    }

    private ItemElement FindItemInCell(GridCell cell, ItemType itemType)
    {
        MapCell mapCell = _mapSystem.GetMapCell(cell);

        if (mapCell == null)
        {
            return null;
        }

        MapElement[] elements = mapCell.GetElementsByType(MapSystem.Enums.MapElementType.Item);

        foreach (MapElement element in elements)
        {
            ItemElement itemElement = element as ItemElement;

            if (itemElement != null && itemElement.ItemType == itemType)
            {
                return itemElement;
            }
        }

        return null;
    }

    private void DropItemInCell(GridCell cell, ItemElement itemElement)
    { 
        if (itemElement == null)
        {
            Debug.LogError("ItemElement component not found on instantiated prefab");
            return;
        }

        itemElement.gameObject.SetActive(true);
        
        if (itemElement is MapElement mapElement)
        {
            _mapSystem.RegisterElement(mapElement);
            _mapSystem.MoveElement(mapElement, cell);
        }
    }

    private void ProcessMouseClick()
    {
        Vector3 mousePosition = Input.mousePosition;

        if (_currentActionState == PlayerActionState.WaitingForMoveTarget)
        {
            ProcessMovementClick(mousePosition);
        }
        else if (_currentActionState == PlayerActionState.WaitingForAttackTarget)
        {
            ProcessAttackClick(mousePosition);
        }
        else if (_currentActionState == PlayerActionState.WaitingForGiveTarget)
        {
            ProcessGiveTargetClick(mousePosition);
        }
    }

    private void ProcessMovementClick(Vector3 mousePosition)
    {
        GridResult<GridCell> result = _gridSystem.ScreenPointToGridCell(mousePosition);

        GridCell targetCell = result.value;
        bool moved = _characterElement.TryMoveTo(targetCell.row, targetCell.column);

        if (moved)
        {
            ConsumeActionPoint();
            Debug.Log($"Player moved to ({targetCell.row}, {targetCell.column})");
        }
        else
        {
            Debug.LogWarning("Movement failed: invalid target cell");
        }

        _currentActionState = PlayerActionState.None;
    }

    private void ProcessAttackClick(Vector3 mousePosition)
    {
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider == null)
        {
            Debug.LogWarning("No target found");
            _currentActionState = PlayerActionState.None;
            return;
        }

        CharacterElement targetCharacter = hit.collider.GetComponent<CharacterElement>();

        if (targetCharacter == null)
        {
            Debug.LogWarning("Clicked object is not a character");
            _currentActionState = PlayerActionState.None;
            return;
        }

        if (targetCharacter == _characterElement)
        {
            Debug.LogWarning("Cannot attack yourself");
            _currentActionState = PlayerActionState.None;
            return;
        }

        if (!IsTargetInRange(targetCharacter, PlayerActionConfiguration.AttackRange))
        {
            Debug.LogWarning("Target is out of attack range");
            _currentActionState = PlayerActionState.None;
            return;
        }

        ExecuteAttack(targetCharacter);
        _currentActionState = PlayerActionState.None;
    }

    private void ProcessGiveTargetClick(Vector3 mousePosition)
    {
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider == null)
        {
            Debug.LogWarning("No target found");
            _currentActionState = PlayerActionState.None;
            return;
        }

        CharacterElement targetCharacter = hit.collider.GetComponent<CharacterElement>();

        if (targetCharacter == null)
        {
            Debug.LogWarning("Clicked object is not a character");
            _currentActionState = PlayerActionState.None;
            return;
        }

        if (targetCharacter == _characterElement)
        {
            Debug.LogWarning("Cannot give item to yourself");
            _currentActionState = PlayerActionState.None;
            return;
        }

        ExecuteGiveItem(targetCharacter);
        _currentActionState = PlayerActionState.None;
    }

    private void ExecuteAttack(CharacterElement target)
    {
        int damage = PlayerActionConfiguration.AttackDamage;
        
        target.ModifyHealth(-damage);
        
        NotifyCharacterOfAttack(target, damage);
        ConsumeActionPoint();
        
        Debug.Log($"Player attacked {target.name} for {damage} damage");
    }

    private void ExecuteGiveItem(CharacterElement target)
    {
        InventoryComponent targetInventory = target.GetComponent<InventoryComponent>();

        if (targetInventory == null)
        {
            Debug.LogWarning("Target character has no inventory");
            return;
        }

        if (targetInventory.HasItem(_selectedItemTypeForGive))
        {
            Debug.LogWarning($"Target already has {_selectedItemTypeForGive} in inventory");
            return;
        }

        InventorySystem.Models.InventoryItem item = _inventoryComponent.GetItem(_selectedItemTypeForGive);
        bool removed = _inventoryComponent.RemoveItem(_selectedItemTypeForGive);

        if (!removed)
        {
            Debug.LogWarning($"Failed to remove {_selectedItemTypeForGive} from inventory");
            return;
        }

        bool added = targetInventory.AddItem(item.itemId, _selectedItemTypeForGive, item.itemElement);

        if (!added)
        {
            _inventoryComponent.AddItem(item.itemId, _selectedItemTypeForGive, item.itemElement);
            Debug.LogWarning($"Failed to add {_selectedItemTypeForGive} to target inventory - rolled back");
            return;
        }

        ConsumeActionPoint();
        Debug.Log($"Gave {_selectedItemTypeForGive} to {target.name}");
    }

    private bool IsTargetInRange(CharacterElement target, int range)
    {
        GridCell currentCell = _characterElement.CurrentGridCell;
        GridCell targetCell = target.CurrentGridCell;

        int distance = _mapSystem.GetGridDistanceBetweenElements(_characterElement, target);
        return distance <= range;
    }

    private void NotifyCharacterOfAttack(CharacterElement target, int damage)
    {
        CharacterAgent agent = target.GetComponent<CharacterAgent>();

        if (agent == null)
        {
            return;
        }

        int currentHealth = target.HealthPoints;
        string attackerName = _characterElement.name;

        PromptConfig attackPrompt = ScriptableObject.CreateInstance<PromptConfig>();
        attackPrompt.promptId = "combat-damage-received";
        attackPrompt.promptName = "Combat";
        attackPrompt.category = "";
        attackPrompt.description = "Damage received in combat";
        attackPrompt.enabled = true;
        attackPrompt.priority = 10;
        attackPrompt.version = "1.0";

        attackPrompt.content = $"{attackerName} has attacked you for {damage} damage. Your current health: {currentHealth}";

        agent.AddContextPrompt(attackPrompt);
    }

    private bool CanExecuteAction()
    {
        if (!_myTurn)
        {
            Debug.LogWarning("Cannot execute action: not player's turn");
            return false;
        }

        if (_currentActionPoints <= 0)
        {
            Debug.LogWarning("Cannot execute action: no action points remaining");
            return false;
        }

        return true;
    }

    private void ConsumeActionPoint()
    {
         _currentActionPoints--;
         _actionMenu.UpdateActionPoints();
    }

    private void ShowActions(bool show)
    {
        _actionMenu.gameObject.SetActive(show);
    }

    private void ShowDialog()
    {
        _dialogObject.SetActive(true);
    }

    private void HideDialog()
    {
        _dialogObject.SetActive(false);
    }

    private void BroadcastMessageToNearbyCharacters(string message)
    {
        GridCell currentCell = _characterElement.CurrentGridCell;
        List<CharacterElement> nearbyCharacters = _mapSystem.GetAllCharactesAtDistance(
            PlayerActionConfiguration.TalkRange, 
            currentCell
        );

        foreach (CharacterElement character in nearbyCharacters)
        {
            if (character == _characterElement)
            {
                continue;
            }

            NotifyCharacterOfMessage(character, message);
        }
    }

    private void NotifyCharacterOfMessage(CharacterElement character, string message)
    {
        CharacterAgent agent = character.GetComponent<CharacterAgent>();

        if (agent == null)
        {
            return;
        }

        string senderName = _myName;

        PromptConfig conversationPrompt = ScriptableObject.CreateInstance<PromptConfig>();
        conversationPrompt.promptId = "conversation-message";
        conversationPrompt.promptName = "Conversaciones";
        conversationPrompt.category = "";
        conversationPrompt.description = "Conversaciones acumuladas en el turno actual";
        conversationPrompt.enabled = true;
        conversationPrompt.priority = 10;
        conversationPrompt.version = "1.0";

        conversationPrompt.content = $"{senderName} ha dicho: {message}";

        agent.AddContextPrompt(conversationPrompt);
    }
}
