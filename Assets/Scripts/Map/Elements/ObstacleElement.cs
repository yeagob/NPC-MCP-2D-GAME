using UnityEngine;
using MapSystem.Enums;

namespace MapSystem.Elements
{
    public class ObstacleElement : MapElement
    {
        [Header("Obstacle Properties")]
        [SerializeField] private bool isDestructible = false;
        [SerializeField] private int durability = 100;
        [SerializeField] private int maxDurability = 100;
        [SerializeField] private bool blocksMovement = true;
        [SerializeField] private bool blocksVision = false;
        [SerializeField] private float destructionForce = 50.0f;
        
        [Header("Obstacle Type")]
        [SerializeField] private ObstacleType obstacleType;
        
        public bool IsDestructible => isDestructible;
        public int Durability => durability;
        public int MaxDurability => maxDurability;
        public bool BlocksMovement => blocksMovement;
        public bool BlocksVision => blocksVision;
        public float DestructionForce => destructionForce;
        public ObstacleType ObstacleType => obstacleType;
        
        protected override void InitializeMapElement()
        {
            elementType = MapElementType.Obstacle;
            visionDistance = 0;
            base.InitializeMapElement();
        }
        
        protected override void CreateDefaultContext()
        {
            base.CreateDefaultContext();
            
            context.SetProperty("isDestructible", isDestructible);
            context.SetProperty("durability", durability);
            context.SetProperty("maxDurability", maxDurability);
            context.SetProperty("blocksMovement", blocksMovement);
            context.SetProperty("blocksVision", blocksVision);
            context.SetProperty("destructionForce", destructionForce);
            context.SetProperty("obstacleType", obstacleType);
            
            context.AddInteraction("Obstacle created and initialized");
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
                OnCharacterInteraction(interactor);
            }
        }
        
        protected virtual void OnCharacterInteraction(MapElement character)
        {
            context.AddInteraction($"Character {character.name} interacted with obstacle");
            
            if (isDestructible)
            {
                context.AddInteraction("Character attempted to interact with destructible obstacle");
            }
        }
        
        public override bool CanBeTraversed()
        {
            return !blocksMovement;
        }
        
        public bool TakeDamage(int damage)
        {
            if (!isDestructible)
            {
                context.AddInteraction($"Received {damage} damage but is indestructible");
                return false;
            }
            
            int previousDurability = durability;
            durability = Mathf.Max(0, durability - damage);
            
            context.SetProperty("durability", durability);
            context.AddInteraction($"Took {damage} damage (was {previousDurability}, now {durability})");
            
            if (durability <= 0)
            {
                OnObstacleDestroyed();
                return true;
            }
            
            return false;
        }
        
        protected virtual void OnObstacleDestroyed()
        {
            context.AddInteraction("Obstacle was destroyed");
            
            if (_mapSystem != null)
            {
                _mapSystem.UnregisterElement(this);
            }
            
            gameObject.SetActive(false);
        }
        
        public void RepairObstacle(int repairAmount)
        {
            if (!isDestructible)
            {
                return;
            }
            
            int previousDurability = durability;
            durability = Mathf.Min(maxDurability, durability + repairAmount);
            
            context.SetProperty("durability", durability);
            context.AddInteraction($"Repaired {repairAmount} durability (was {previousDurability}, now {durability})");
        }
        
        public void SetDestructible(bool destructible)
        {
            isDestructible = destructible;
            context.SetProperty("isDestructible", isDestructible);
            context.AddInteraction($"Destructible status changed to {isDestructible}");
        }
        
        public void SetBlocksMovement(bool blocks)
        {
            blocksMovement = blocks;
            context.SetProperty("blocksMovement", blocksMovement);
            context.AddInteraction($"Movement blocking changed to {blocksMovement}");
            
            if (_mapSystem != null)
            {
                var mapCell = _mapSystem.GetMapCell(CurrentGridCell);
                if (mapCell != null)
                {
                    mapCell.SetTraversable(!blocksMovement);
                }
            }
        }
        
        public void SetBlocksVision(bool blocks)
        {
            blocksVision = blocks;
            context.SetProperty("blocksVision", blocksVision);
            context.AddInteraction($"Vision blocking changed to {blocksVision}");
        }
        
        public void SetObstacleType(ObstacleType newType)
        {
            obstacleType = newType;
            context.SetProperty("obstacleType", obstacleType);
            context.AddInteraction($"Obstacle type changed to {obstacleType}");
            
            UpdatePropertiesForType();
        }
        
        private void UpdatePropertiesForType()
        {
            switch (obstacleType)
            {
                case ObstacleType.Wall:
                    SetBlocksMovement(true);
                    SetBlocksVision(true);
                    break;
                case ObstacleType.Decoration:
                    SetBlocksMovement(false);
                    SetBlocksVision(false);
                    break;
            }
        }
        
        public float GetDurabilityPercentage()
        {
            if (maxDurability <= 0)
            {
                return 0.0f;
            }
            
            return (float)durability / maxDurability;
        }
        
        public bool IsDestroyed()
        {
            return isDestructible && durability <= 0;
        }
        
        protected override void DrawVisionRadius()
        {
            Color gizmosColor = Color.gray;
            
            switch (obstacleType)
            {
                case ObstacleType.Wall:
                    gizmosColor = Color.black;
                    break;
                case ObstacleType.Decoration:
                    gizmosColor = Color.green;
                    break;
            }
            
            Gizmos.color = gizmosColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
            
            if (isDestructible)
            {
                float healthPercentage = GetDurabilityPercentage();
                Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercentage);
                Gizmos.DrawCube(transform.position + Vector3.up * 0.6f, new Vector3(0.8f, 0.1f, 0.1f));
            }
        }
    }
    
    public enum ObstacleType
    {
        Wall,
        Decoration
    }
}