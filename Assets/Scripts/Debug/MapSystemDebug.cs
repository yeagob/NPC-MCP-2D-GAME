using UnityEngine;
using Grid.Models.Grid;
using MapSystem.Enums;
using MapSystem.Models.Vision;
using MapSystem.Vision;
using MapSystem.Navigation;

namespace MapSystem.MapDebug
{
    public class MapSystemDebug : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private MapSystem mapSystem;
        [SerializeField] private bool enableDebugMode = true;
        
        [Header("Test Parameters")]
        [SerializeField] private int testRow = 2;
        [SerializeField] private int testColumn = 2;
        [SerializeField] private ViewDirection testDirection = ViewDirection.Right;
        [SerializeField] private int testVisionRange = 3;
        [SerializeField] private MapElementType filterElementType = MapElementType.Character;
        
        [Header("Debug Colors")]
        [SerializeField] private Color visionCellColor = Color.cyan;
        [SerializeField] private Color elementHighlightColor = Color.yellow;
        [SerializeField] private Color traversableCellColor = Color.green;
        [SerializeField] private Color blockedCellColor = Color.red;
        
        private VisionResult lastVisionResult;
        private GridCell[] lastTraversableCells;
        private MapElement[] lastFoundElements;
        
        private void Awake()
        {
            if (mapSystem == null)
            {
                mapSystem = GetComponent<MapSystem>();
                if (mapSystem == null)
                {
                    mapSystem = FindObjectOfType<MapSystem>();
                }
            }
        }
        
        [ContextMenu("Test Vision Query")]
        public void TestVisionQuery()
        {
            if (!enableDebugMode || mapSystem == null || !mapSystem.IsInitialized)
            {
                UnityEngine.Debug.LogWarning("MapSystemDebug: Cannot test vision - system not ready");
                return;
            }
            
            GridCell testCell = new GridCell(testRow, testColumn);
            lastVisionResult = mapSystem.GetElementsInDirection(testCell, testDirection, testVisionRange);
            
            UnityEngine.Debug.Log($"Vision Test Results for cell {testCell} looking {testDirection}:");
            UnityEngine.Debug.Log($"- Scanned {lastVisionResult.scannedCells.Count} cells");
            UnityEngine.Debug.Log($"- Found {lastVisionResult.GetElementCount()} elements");
            
            foreach (MapElement element in lastVisionResult.visibleElements)
            {
                UnityEngine.Debug.Log($"  * {element.name} ({element.ElementType}) at {element.CurrentGridCell}");
            }
        }
        
        [ContextMenu("Test Navigation")]
        public void TestNavigation()
        {
            if (!enableDebugMode || mapSystem == null || !mapSystem.IsInitialized)
            {
                UnityEngine.Debug.LogWarning("MapSystemDebug: Cannot test navigation - system not ready");
                return;
            }
            
            GridCell testCell = new GridCell(testRow, testColumn);
            lastTraversableCells = mapSystem.GetAdjacentTraversableCells(testCell);
            
            UnityEngine.Debug.Log($"Navigation Test Results for cell {testCell}:");
            UnityEngine.Debug.Log($"- Cell is traversable: {mapSystem.IsCellTraversable(testCell)}");
            UnityEngine.Debug.Log($"- Adjacent traversable cells: {lastTraversableCells.Length}");
            
            foreach (GridCell cell in lastTraversableCells)
            {
                UnityEngine.Debug.Log($"  * {cell}");
            }
        }
        
        [ContextMenu("Test Element Search")]
        public void TestElementSearch()
        {
            if (!enableDebugMode || mapSystem == null || !mapSystem.IsInitialized)
            {
                UnityEngine.Debug.LogWarning("MapSystemDebug: Cannot test elements - system not ready");
                return;
            }
            
            lastFoundElements = mapSystem.GetElementsByType(filterElementType);
            
            UnityEngine.Debug.Log($"Element Search Results for type {filterElementType}:");
            UnityEngine.Debug.Log($"- Total elements of this type: {lastFoundElements.Length}");
            UnityEngine.Debug.Log($"- Total elements in map: {mapSystem.GetTotalElementCount()}");
            
            foreach (MapElement element in lastFoundElements)
            {
                UnityEngine.Debug.Log($"  * {element.name} at {element.CurrentGridCell} (Vision: {element.VisionDistance})");
            }
        }
        
        [ContextMenu("Test Element Interactions")]
        public void TestElementInteractions()
        {
            if (!enableDebugMode || mapSystem == null || !mapSystem.IsInitialized)
            {
                return;
            }
            
            MapElement[] allElements = mapSystem.GetAllElements();
            UnityEngine.Debug.Log($"Element Interaction Analysis ({allElements.Length} total elements):");
            
            int itemCount = mapSystem.GetElementCountByType(MapElementType.Item);
            int characterCount = mapSystem.GetElementCountByType(MapElementType.Character);
            int obstacleCount = mapSystem.GetElementCountByType(MapElementType.Obstacle);
            
            UnityEngine.Debug.Log($"- Items: {itemCount}");
            UnityEngine.Debug.Log($"- Characters: {characterCount}");
            UnityEngine.Debug.Log($"- Obstacles: {obstacleCount}");
            
            foreach (MapElement element in allElements)
            {
                string[] recentInteractions = element.Context.GetRecentInteractions(3);
                if (recentInteractions.Length > 0)
                {
                    UnityEngine.Debug.Log($"{element.name} recent interactions:");
                    foreach (string interaction in recentInteractions)
                    {
                        UnityEngine.Debug.Log($"  - {interaction}");
                    }
                }
            }
        }
        
        [ContextMenu("Test Distance Calculations")]
        public void TestDistanceCalculations()
        {
            if (!enableDebugMode || mapSystem == null || !mapSystem.IsInitialized)
            {
                return;
            }
            
            MapElement[] allElements = mapSystem.GetAllElements();
            
            if (allElements.Length < 2)
            {
                UnityEngine.Debug.LogWarning("Need at least 2 elements to test distances");
                return;
            }
            
            UnityEngine.Debug.Log("Distance Test Results:");
            
            for (int i = 0; i < allElements.Length - 1; i++)
            {
                for (int j = i + 1; j < allElements.Length; j++)
                {
                    MapElement elementA = allElements[i];
                    MapElement elementB = allElements[j];
                    
                    float worldDistance = mapSystem.GetDistanceBetweenElements(elementA, elementB);
                    int gridDistance = mapSystem.GetGridDistanceBetweenElements(elementA, elementB);
                    bool canSee = mapSystem.CanElementSeeElement(elementA, elementB);
                    
                    UnityEngine.Debug.Log($"{elementA.name} ↔ {elementB.name}:");
                    UnityEngine.Debug.Log($"  World Distance: {worldDistance:F2}");
                    UnityEngine.Debug.Log($"  Grid Distance: {gridDistance}");
                    UnityEngine.Debug.Log($"  Can See Each Other: {canSee}");
                }
            }
        }
        
        [ContextMenu("Show Map Statistics")]
        public void ShowMapStatistics()
        {
            if (!enableDebugMode || mapSystem == null || !mapSystem.IsInitialized)
            {
                return;
            }
            
            var allCells = mapSystem.GetAllMapCells();
            int traversableCells = 0;
            int occupiedCells = 0;
            
            foreach (var cell in allCells)
            {
                if (cell.isTraversable)
                    traversableCells++;
                if (cell.HasElements())
                    occupiedCells++;
            }
            
            UnityEngine.Debug.Log("=== MAP STATISTICS ===");
            UnityEngine.Debug.Log($"Total Cells: {allCells.Length}");
            UnityEngine.Debug.Log($"Traversable Cells: {traversableCells}");
            UnityEngine.Debug.Log($"Occupied Cells: {occupiedCells}");
            UnityEngine.Debug.Log($"Empty Cells: {allCells.Length - occupiedCells}");
            UnityEngine.Debug.Log($"Total Elements: {mapSystem.GetTotalElementCount()}");
        }
        
        private void OnDrawGizmos()
        {
            if (!enableDebugMode || mapSystem == null || !mapSystem.IsInitialized)
            {
                return;
            }
            
            DrawDebugVisualizations();
        }
        
        private void DrawDebugVisualizations()
        {
            DrawVisionResults();
            DrawTraversableCells();
            DrawElementHighlights();
        }
        
        private void DrawVisionResults()
        {
            if (lastVisionResult.scannedCells == null)
            {
                return;
            }
            
            Gizmos.color = visionCellColor;
            foreach (GridCell cell in lastVisionResult.scannedCells)
            {
                Vector3 worldPos = mapSystem.GetWorldPositionFromGridCell(cell);
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
            }
        }
        
        private void DrawTraversableCells()
        {
            if (lastTraversableCells == null)
            {
                return;
            }
            
            Gizmos.color = traversableCellColor;
            foreach (GridCell cell in lastTraversableCells)
            {
                Vector3 worldPos = mapSystem.GetWorldPositionFromGridCell(cell);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.7f);
            }
        }
        
        private void DrawElementHighlights()
        {
            if (lastFoundElements == null)
            {
                return;
            }
            
            Gizmos.color = elementHighlightColor;
            foreach (MapElement element in lastFoundElements)
            {
                Vector3 worldPos = mapSystem.GetWorldPositionFromGridCell(element.CurrentGridCell);
                Gizmos.DrawWireSphere(worldPos, 0.8f);
            }
        }
        
        public void SetTestCell(int row, int column)
        {
            testRow = row;
            testColumn = column;
        }
        
        public void SetTestDirection(ViewDirection direction)
        {
            testDirection = direction;
        }
        
        public void SetElementFilter(MapElementType elementType)
        {
            filterElementType = elementType;
        }
    }
}