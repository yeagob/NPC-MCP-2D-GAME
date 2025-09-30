using UnityEngine;
using Grid.Models.Grid;
using MapSystem.Enums;
using MapSystem.Models.Vision;
using MapSystem.Vision;
using MapSystem.Navigation;
using InventorySystem.Components;

namespace MapSystem.Elements
{
    public class CharacterElement : MapElement
    {
        [Header("Character Properties")]
        [SerializeField] private float movementSpeed ;
        [SerializeField] private bool canMove ;
        [SerializeField] private bool isPlayerControlled ;
        [SerializeField] private ViewDirection facingDirection;
        
        [Header("Character Stats")]
        [SerializeField] private int healthPoints = 100;
        [SerializeField] private int maxHealthPoints = 100;
        [SerializeField] private int experiencePoints = 0;

        public float MovementSpeed => movementSpeed;
        public bool CanMove => canMove;
        public bool IsPlayerControlled => isPlayerControlled;
        public ViewDirection FacingDirection => facingDirection;
        public int HealthPoints => healthPoints;
        public int MaxHealthPoints => maxHealthPoints;
        public int ExperiencePoints => experiencePoints;
        
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
            return false;
        }
        
        public bool TryMoveTo(int row, int col)
        {
            Debug.Log($"Trying to move to row {row}, col {col}");

            GridCell targetCell = _mapSystem.GetGridCell(row, col);
            return TryMoveToCell(targetCell);
        }
        
        public bool TryMoveToCell(GridCell targetCell)
        {
            if (!canMove || isMoving || _mapSystem == null)
            {
                return false;
            }
            
            Debug.Log("Trying to move to cell");
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
            facingDirection = !left ? ViewDirection.Right : ViewDirection.Left;
            
            elementSprite.flipX = facingDirection == ViewDirection.Right;
            
            context.SetProperty("facingDirection", facingDirection);
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
            return LookInDirection(facingDirection);
        }
        
        public VisionResult ScanSurroundings()
        {
            return _mapSystem.GetElementsInVisionRange(this);
        }
        
        public void SetMovementSpeed(float newSpeed)
        {
            movementSpeed = Mathf.Max(0.1f, newSpeed);
            context.SetProperty("movementSpeed", movementSpeed);
            context.AddInteraction($"Movement speed changed to {movementSpeed}");
        }
        
        public void SetCanMove(bool canMoveValue)
        {
            canMove = canMoveValue;
            context.SetProperty("canMove", canMove);
            context.AddInteraction($"Movement ability changed to {canMove}");
        }
        
        public void SetFacingDirection(ViewDirection direction)
        {
            facingDirection = direction;
            context.SetProperty("facingDirection", facingDirection);
            context.AddInteraction($"Facing direction changed to {facingDirection}");
        }
        
        public void SetPlayerControlled(bool playerControlled)
        {
            isPlayerControlled = playerControlled;
            context.SetProperty("isPlayerControlled", isPlayerControlled);
            context.AddInteraction($"Player control changed to {isPlayerControlled}");
        }
        
        public void ModifyHealth(int amount)
        {
            int previousHealth = healthPoints;
            healthPoints = Mathf.Clamp(healthPoints + amount, 0, maxHealthPoints);
            
            context.SetProperty("healthPoints", healthPoints);
            
            if (amount > 0)
            {
                context.AddInteraction($"Healed {amount} HP (was {previousHealth}, now {healthPoints})");
            }
            else if (amount < 0)
            {
                context.AddInteraction($"Took {-amount} damage (was {previousHealth}, now {healthPoints})");
            }
            
            if (healthPoints <= 0)
            {
                OnCharacterDefeated();
            }
        }
        
        protected virtual void OnCharacterDefeated()
        {
            context.AddInteraction("Character was defeated");
            canMove = false;
        }
        
        public void AddExperience(int experience)
        {
            experiencePoints += experience;
            context.SetProperty("experiencePoints", experiencePoints);
            context.AddInteraction($"Gained {experience} experience (total: {experiencePoints})");
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
            
            Gizmos.color = isPlayerControlled ? Color.blue : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 1.2f);
            
            DrawFacingDirection();
        }
        
        private void DrawFacingDirection()
        {
            Vector3 directionVector = GetDirectionVector(facingDirection);
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
            if (facingDirection == ViewDirection.Right)
            {
                facingDirection = ViewDirection.Left;
            }
            else
            {
                facingDirection = ViewDirection.Right;
            }
            
            elementSprite.flipX = facingDirection == ViewDirection.Right;
            
            return facingDirection;
        }

        public void Listen(string message)
        {
            
        }
    }
}