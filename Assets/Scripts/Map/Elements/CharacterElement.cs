using UnityEngine;
using Grid.Models.Grid;
using MapSystem.Enums;
using MapSystem.Models.Vision;
using MapSystem.Vision;
using MapSystem.Navigation;
using InventorySystem.Components;
using UnityEngine.Serialization;

namespace MapSystem.Elements
{
    public class CharacterElement : MapElement
    {
        [Header("Character Properties")]
        [SerializeField] 
        private float _movementSpeed ;
        
        [SerializeField] 
        private bool _canMove;
        
        [SerializeField] 
        private bool _isPlayerControlled ;
        
        [SerializeField] 
        private ViewDirection _facingDirection;
        
        [Header("Character Stats")]
        [SerializeField] 
        private int _healthPoints = 100;
        
        [SerializeField] 
        private int _maxHealthPoints = 100;
        
        [SerializeField] 
        private int _experiencePoints = 0;
        
        [SerializeField] 
        private bool _canBeTraversed;

        public float MovementSpeed => _movementSpeed;
        public bool CanMove => _canMove;
        public bool IsPlayerControlled => _isPlayerControlled;
        public ViewDirection FacingDirection => _facingDirection;
        public int HealthPoints => _healthPoints;
        public int MaxHealthPoints => _maxHealthPoints;
        public int ExperiencePoints => _experiencePoints;
        
        private bool isMoving = false;
        
        private InventoryComponent inventoryComponent;

        protected override void InitializeMapElement()
        {
            elementType = MapElementType.Character;
            visionDistance = 5;
            base.InitializeMapElement();
            
            inventoryComponent = GetComponent<InventoryComponent>();
            if (inventoryComponent == null)
            {
                inventoryComponent = gameObject.AddComponent<InventoryComponent>();
            }
        }
        
        public override void OnElementInteraction(MapElement interactor)
        {
            if (interactor == null)
            {
                return;
            }
            
            string interactionMessage = $"Interacted with by {interactor.name} ({interactor.ElementType})";
            context.AddInteraction(interactionMessage);
            
            if (interactor.ElementType == MapElementType.Character)
            {
                OnCharacterEncounter(interactor as CharacterElement);
            }
            else if (interactor.ElementType == MapElementType.Item)
            {
                OnItemInteraction(interactor);
            }
        }
        
        protected virtual void OnCharacterEncounter(CharacterElement otherCharacter)
        {
            if (otherCharacter != null)
            {
                context.AddInteraction($"Encountered character: {otherCharacter.name}");
            }
        }
        
        protected virtual void OnItemInteraction(MapElement item)
        {
            context.AddInteraction($"Interacted with item: {item.name}");
        }
        
        public override bool CanBeTraversed()
        {
            return _canBeTraversed;
        }
        
        public bool TryMoveTo(int row, int col)
        {

            GridCell targetCell = _mapSystem.GetGridCell(row, col);
            return TryMoveToCell(targetCell);
        }
        
        public bool TryMoveToCell(GridCell targetCell)
        {
            if (!_canMove || isMoving || _mapSystem == null)
            {
                return false;
            }
            
            if (!_mapSystem.IsNavigationViable(this, targetCell))
            {
                context.AddInteraction($"Failed to move to {targetCell} - not traversable");
                return false;
            }
            
            GridCell currentCell = CurrentGridCell;
            _mapSystem.MoveElement(this, targetCell);
            
            Debug.Log("Moving to cell");
            
            UpdateFacingDirection(currentCell, targetCell);
            
            Debug.Log("Moved to cell");

            context.AddInteraction($"Moved from {currentCell} to {targetCell}");
            
            UpdateInventoryContext();
            
            return true;
        }
        
        private void UpdateFacingDirection(GridCell fromCell, GridCell toCell)
        {
            int columnDelta = toCell.column - fromCell.column;
            
            if (columnDelta != 0)
            {
                UpdateFacingDirection(columnDelta < 0);
            }
        }
        
        private void UpdateFacingDirection(bool left)
        {
            _facingDirection = !left ? ViewDirection.Right : ViewDirection.Left;
            
            elementSprite.flipX = _facingDirection == ViewDirection.Right;
            
            context.SetProperty("facingDirection", _facingDirection);
        }
        
        public VisionResult LookInDirection(ViewDirection direction)
        {
            if (_mapSystem == null)
            {
                return VisionResult.Empty(direction, CurrentGridCell, visionDistance);
            }
            
            return _mapSystem.GetElementsInDirection(CurrentGridCell, direction, visionDistance);
        }
        
        public VisionResult LookForward()
        {
            return LookInDirection(_facingDirection);
        }
        
        public VisionResult ScanSurroundings()
        {
            return _mapSystem.GetElementsInVisionRange(this);
        }
        
        public void SetMovementSpeed(float newSpeed)
        {
            _movementSpeed = Mathf.Max(0.1f, newSpeed);
            context.SetProperty("movementSpeed", _movementSpeed);
            context.AddInteraction($"Movement speed changed to {_movementSpeed}");
        }
        
        public void SetCanMove(bool canMoveValue)
        {
            _canMove = canMoveValue;
            context.SetProperty("canMove", _canMove);
            context.AddInteraction($"Movement ability changed to {_canMove}");
        }
        
        public void SetFacingDirection(ViewDirection direction)
        {
            _facingDirection = direction;
            context.SetProperty("facingDirection", _facingDirection);
            context.AddInteraction($"Facing direction changed to {_facingDirection}");
        }
        
        public void SetPlayerControlled(bool playerControlled)
        {
            _isPlayerControlled = playerControlled;
            context.SetProperty("isPlayerControlled", _isPlayerControlled);
            context.AddInteraction($"Player control changed to {_isPlayerControlled}");
        }
        
        public void ModifyHealth(int amount)
        {
            int previousHealth = _healthPoints;
            _healthPoints = Mathf.Clamp(_healthPoints + amount, 0, _maxHealthPoints);
            
            context.SetProperty("healthPoints", _healthPoints);
            
            if (amount > 0)
            {
                context.AddInteraction($"Healed {amount} HP (was {previousHealth}, now {_healthPoints})");
            }
            else if (amount < 0)
            {
                context.AddInteraction($"Took {-amount} damage (was {previousHealth}, now {_healthPoints})");
            }
            
            if (_healthPoints <= 0)
            {
                OnCharacterDefeated();
            }
        }
        
        protected virtual void OnCharacterDefeated()
        {
            context.AddInteraction("Character was defeated");
            _canMove = false;
        }
        
        public void AddExperience(int experience)
        {
            _experiencePoints += experience;
            context.SetProperty("experiencePoints", _experiencePoints);
            context.AddInteraction($"Gained {experience} experience (total: {_experiencePoints})");
        }
        
        public void UpdateInventoryContext()
        {
            if (inventoryComponent != null)
            {
                context.SetProperty("inventoryDescription", inventoryComponent.GetInventoryDescription());
            }
        }
        
        protected override void DrawVisionRadius()
        {
            base.DrawVisionRadius();
            
            Gizmos.color = _isPlayerControlled ? Color.blue : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 1.2f);
            
            DrawFacingDirection();
        }
        
        private void DrawFacingDirection()
        {
            Vector3 directionVector = GetDirectionVector(_facingDirection);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, directionVector * 0.8f);
        }
        
        private Vector3 GetDirectionVector(ViewDirection direction)
        {
            switch (direction)
            {
                case ViewDirection.Left: return Vector3.left;
                case ViewDirection.Right: return Vector3.right;
                default: return Vector3.up;
            }
        }

        public ViewDirection Flip()
        {
            if (_facingDirection == ViewDirection.Right)
            {
                _facingDirection = ViewDirection.Left;
            }
            else
            {
                _facingDirection = ViewDirection.Right;
            }
            
            elementSprite.flipX = _facingDirection == ViewDirection.Right;
            
            return _facingDirection;
        }

        public void Listen(string message)
        {
            
        }
    }
}