    using UnityEngine;
    using Grid.Models.Grid;
    using MapSystem.Enums;
    using MapSystem.Models.Context;

    namespace MapSystem
    {
        public abstract class MapElement : MonoBehaviour
        {
            [Header("Map Element Configuration")]
            [SerializeField] 
            protected int elementId;
            [SerializeField] 
            protected MapElementType elementType;
            [SerializeField] 
            protected int visionDistance = 3;
            [SerializeField] 
            protected int currentGridIndex ;
            
            protected  GridCell currentGridCell;
            
            [Header("Visual Representation")]
            protected SpriteRenderer elementSprite;
            protected Sprite defaultSprite;
            
            [Header("Context Data")]
            [SerializeField]
            protected MapElementContext context;
            
            [SerializeField]
            protected MapSystem _mapSystem;
            
            protected bool isInitialized = false;

            public Vector2 GridPosition => GetGridPosition();

            public int Id => elementId;
            
            public MapElementType ElementType => elementType;
            public int VisionDistance => visionDistance;
            public GridCell CurrentGridCell => currentGridCell;
            public int CurrentGridIndex => currentGridIndex;
            public MapElementContext Context => context;
            public SpriteRenderer ElementSprite => elementSprite;
            
            protected virtual void Start()
            {
                InitializeMapElement();
            }
            
            protected virtual void InitializeMapElement()
            {
                if (elementSprite == null)
                {
                    elementSprite = GetComponent<SpriteRenderer>();
                    if (elementSprite == null)
                    {
                        elementSprite = gameObject.AddComponent<SpriteRenderer>();
                    }
                }
                
                if (elementSprite.sprite == null && defaultSprite != null)
                {
                    elementSprite.sprite = defaultSprite;
                }
                
                if (context == null)
                {
                    CreateDefaultContext();
                }
                
                isInitialized = true;
            }
            
            protected virtual void CreateDefaultContext()
            {
                context = new MapElementContext(
                    gameObject.name,
                    $"A {elementType} element in the map"
                );
                
                context.SetProperty("elementType", elementType);
                context.SetProperty("visionDistance", visionDistance);
                context.initialPosition = transform.position;
            }
            
            public virtual void SetGridPosition(GridCell newCell, MapSystem mapSystem)
            {
                GridCell previousCell = currentGridCell;
                currentGridCell = newCell;
                
                if (mapSystem != null)
                {
                    Vector3 worldPosition = mapSystem.GetWorldPositionFromGridCell(newCell);
                    transform.position = worldPosition;
                    
                    context.AddInteraction($"Moved from {previousCell} to {currentGridCell}");
                }
            }
            
            public Vector2 GetGridPosition()
            {
                return new (currentGridCell.row, currentGridCell.column);
            }
            
            public virtual void UpdateWorldPosition()
            {
                if (_mapSystem != null)
                {
                    Vector3 worldPosition = _mapSystem.GetWorldPositionFromGridCell(currentGridCell);
                    transform.position = worldPosition;
                }
            }
            
            public virtual void SetVisionDistance(int newVisionDistance)
            {
                visionDistance = Mathf.Max(0, newVisionDistance);
                context.SetProperty("visionDistance", visionDistance);
                context.AddInteraction($"Vision distance changed to {visionDistance}");
            }
            
            public virtual void SetSprite(Sprite newSprite)
            {
                if (elementSprite != null && newSprite != null)
                {
                    elementSprite.sprite = newSprite;
                    context.AddInteraction($"Sprite changed to {newSprite.name}");
                }
            }
            
            public virtual void AddInteraction(string interaction)
            {
                context.AddInteraction(interaction);
            }
            
            public virtual float GetDistanceToElement(MapElement otherElement)
            {
                if (otherElement == null || _mapSystem == null)
                {
                    return float.MaxValue;
                }
                
                return _mapSystem.GetDistanceBetweenElements(this, otherElement);
            }
            
            public virtual bool CanSeeElement(MapElement otherElement)
            {
                if (otherElement == null)
                {
                    return false;
                }
                
                float distance = GetDistanceToElement(otherElement);
                return distance <= visionDistance && distance <= otherElement.visionDistance;
            }
            
            public abstract void OnElementInteraction(MapElement interactor);
            
            public abstract bool CanBeTraversed();
            
            
            protected virtual void OnDrawGizmosSelected()
            {
                if (!Application.isPlaying)
                {
                    return;
                }
                
                DrawVisionRadius();
            }
            
            protected virtual void DrawVisionRadius()
            {
                if (_mapSystem == null)
                {
                    return;
                }
                
                Gizmos.color = Color.cyan;
                float cellSize = 1.0f;
                float radius = visionDistance * cellSize;
                
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }