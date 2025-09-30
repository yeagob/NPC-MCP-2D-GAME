using UnityEngine;
using MapSystem.Enums;
using InventorySystem.Enums;

namespace MapSystem.Elements
{
    public class ItemElement : MapElement
    {
        [Header("Item Properties")]
        [SerializeField] private bool isCollectable = true;
        [SerializeField] private int pickupRange = 1;
        
        [Header("Item Type")]
        [SerializeField] private ItemType itemType = ItemType.Apple;
        
        public bool IsCollectable => isCollectable;
        public int PickupRange => pickupRange;
        public ItemType ItemType => itemType;
        
        protected override void InitializeMapElement()
        {
            elementType = MapElementType.Item;
            visionDistance = 1;
            base.InitializeMapElement();
        }
        
        protected override void CreateDefaultContext()
        {
            base.CreateDefaultContext();
            
            context.SetProperty("isCollectable", isCollectable);
            context.SetProperty("pickupRange", pickupRange);
            context.SetProperty("itemType", itemType.ToString());
            
            context.AddInteraction("Item created and initialized");
        }
        
        public override void OnElementInteraction(MapElement interactor)
        {
            if (interactor == null)
            {
                return;
            }
            
            string interactionMessage = $"Interacted with by {interactor.name} ({interactor.ElementType})";
            context.AddInteraction(interactionMessage);
            
            if (isCollectable && interactor.ElementType == MapElementType.Character)
            {
                OnItemCollected(interactor);
            }
        }
        
        protected virtual void OnItemCollected(MapElement collector)
        {
            context.AddInteraction($"Collected by {collector.name}");
            
            if (_mapSystem != null)
            {
                _mapSystem.UnregisterElement(this);
            }
            
            gameObject.SetActive(false);
        }
        
        public override bool CanBeTraversed()
        {
            return true;
        }
        
        public void SetCollectable(bool collectable)
        {
            isCollectable = collectable;
            context.SetProperty("isCollectable", isCollectable);
            context.AddInteraction($"Collectable status changed to {isCollectable}");
        }
        
        public void SetItemType(ItemType newItemType)
        {
            itemType = newItemType;
            context.SetProperty("itemType", itemType.ToString());
            context.AddInteraction($"Item type changed to {itemType}");
        }
        
        public void SetPickupRange(int newPickupRange)
        {
            pickupRange = Mathf.Max(1, newPickupRange);
            context.SetProperty("pickupRange", pickupRange);
            context.AddInteraction($"Pickup range changed to {pickupRange}");
        }
        
        protected override void DrawVisionRadius()
        {
            base.DrawVisionRadius();
            
            if (isCollectable)
            {
                Color itemColor = GetItemTypeColor();
                Gizmos.color = itemColor;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
            }
        }
        
        private Color GetItemTypeColor()
        {
            switch (itemType)
            {
                case ItemType.Key:
                    return Color.yellow;
                case ItemType.Money:
                    return Color.green;
                case ItemType.Apple:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
    }
}