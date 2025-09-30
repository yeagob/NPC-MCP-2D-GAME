using System.Collections.Generic;
using Grid.Models.Grid;
using MapSystem.Models.Map;
using MapSystem.Models.Vision;
using MapSystem.Enums;

namespace MapSystem.Vision
{
    public static class MapVisionExtensions
    {
        public static VisionResult GetElementsInDirection(this MapSystem mapSystem, GridCell observerCell, ViewDirection direction, int maxRange = int.MaxValue)
        {
            if (!mapSystem.IsInitialized)
            {
                return VisionResult.Empty(direction, observerCell, maxRange);
            }
            
            GridConfiguration gridConfig = mapSystem.GridSystem.GetGridConfiguration();
            if (!gridConfig.IsValidGridCell(observerCell))
            {
                return VisionResult.Empty(direction, observerCell, maxRange);
            }
            
            List<MapElement> visibleElements = new List<MapElement>();
            List<GridCell> scannedCells = new List<GridCell>();
            
            switch (direction)
            {
                case ViewDirection.Left:
                    ScanHorizontalDirection(mapSystem, observerCell, -1, maxRange, visibleElements, scannedCells);
                    break;
                case ViewDirection.Right:
                    ScanHorizontalDirection(mapSystem, observerCell, 1, maxRange, visibleElements, scannedCells);
                    break;
            }
            
            return VisionResult.Success(direction, observerCell, maxRange, visibleElements, scannedCells);
        }
        
        public static VisionResult GetElementsToLeft(this MapSystem mapSystem, GridCell observerCell, int maxRange = int.MaxValue)
        {
            return mapSystem.GetElementsInDirection(observerCell, ViewDirection.Left, maxRange);
        }
        
        public static VisionResult GetElementsToRight(this MapSystem mapSystem, GridCell observerCell, int maxRange = int.MaxValue)
        {
            return mapSystem.GetElementsInDirection(observerCell, ViewDirection.Right, maxRange);
        }
        
 
        
        public static VisionResult GetElementsInVisionRange(this MapSystem mapSystem, MapElement observer)
        {
            
            GridCell observerCell = observer.CurrentGridCell;
            int visionRange = observer.VisionDistance;
            
            List<MapElement> visibleElements = new List<MapElement>();
            List<GridCell> scannedCells = new List<GridCell>();
            
            GridConfiguration gridConfig = mapSystem.GridSystem.GetGridConfiguration();
            
            for (int row = observerCell.row - visionRange; row <= observerCell.row + visionRange; row++)
            {
                for (int column = observerCell.column - visionRange; column <= observerCell.column + visionRange; column++)
                {
                    GridCell targetCell = new GridCell(row, column);
                    
                    if (gridConfig.IsValidGridCell(targetCell))
                    {
                        int distance = Grid.Grid.GridUtilities.GetManhattanDistance(observerCell, targetCell);
                        
                        if (distance <= visionRange && distance > 0)
                        {
                            scannedCells.Add(targetCell);
                            MapCell mapCell = mapSystem.GetMapCell(targetCell);
                            
                            if (mapCell != null && mapCell.HasElements())
                            {
                                foreach (MapElement element in mapCell.GetElements())
                                {
                                    if (element != observer && element.VisionDistance >= distance)
                                    {
                                        visibleElements.Add(element);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return VisionResult.Success(ViewDirection.Left, observerCell, visionRange, visibleElements, scannedCells);
        }
        
        private static void ScanHorizontalDirection(MapSystem mapSystem, GridCell observerCell, int direction, int maxRange, 
                                                   List<MapElement> visibleElements, List<GridCell> scannedCells)
        {
            GridConfiguration gridConfig = mapSystem.GridSystem.GetGridConfiguration();
            
            for (int columnOffset = 0; columnOffset <= maxRange; columnOffset++)
            {
                int targetColumn = observerCell.column + (columnOffset * direction);
                
                for (int row = 0; row < gridConfig.gridHeight; row++)
                {
                    GridCell targetCell = new GridCell(row, targetColumn);
                    
                    if (gridConfig.IsValidGridCell(targetCell))
                    {
                        scannedCells.Add(targetCell);
                        MapCell mapCell = mapSystem.GetMapCell(targetCell);
                        
                        if (mapCell != null && mapCell.HasElements())
                        {
                            int distance = Grid.Grid.GridUtilities.GetManhattanDistance(observerCell, targetCell);
                            
                            foreach (MapElement element in mapCell.GetElements())
                            {
                                if (element.VisionDistance >= distance)
                                {
                                    visibleElements.Add(element);
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        
        private static void ScanVerticalDirection(MapSystem mapSystem, GridCell observerCell, int direction, int maxRange, 
                                                 List<MapElement> visibleElements, List<GridCell> scannedCells)
        {
            GridConfiguration gridConfig = mapSystem.GridSystem.GetGridConfiguration();
            
            for (int rowOffset = 0; rowOffset <= maxRange; rowOffset++)
            {
                int targetRow = observerCell.row + (rowOffset * direction);
                
                for (int column = 0; column < gridConfig.gridWidth; column++)
                {
                    GridCell targetCell = new GridCell(targetRow, column);
                    
                    if (gridConfig.IsValidGridCell(targetCell))
                    {
                        scannedCells.Add(targetCell);
                        MapCell mapCell = mapSystem.GetMapCell(targetCell);
                        
                        if (mapCell != null && mapCell.HasElements())
                        {
                            int distance = Grid.Grid.GridUtilities.GetManhattanDistance(observerCell, targetCell);
                            
                            foreach (MapElement element in mapCell.GetElements())
                            {
                                if (element.VisionDistance >= distance)
                                {
                                    visibleElements.Add(element);
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        
        private static void ScanForwardDirection(MapSystem mapSystem, GridCell observerCell, int maxRange, 
                                               List<MapElement> visibleElements, List<GridCell> scannedCells)
        {
            ScanVerticalDirection(mapSystem, observerCell, -1, maxRange, visibleElements, scannedCells);
        }
        
        private static void ScanBackwardDirection(MapSystem mapSystem, GridCell observerCell, int maxRange, 
                                                List<MapElement> visibleElements, List<GridCell> scannedCells)
        {
            ScanVerticalDirection(mapSystem, observerCell, 1, maxRange, visibleElements, scannedCells);
        }
        
        public static bool CanElementSeeElement(this MapSystem mapSystem, MapElement observer, MapElement target)
        {
            if (observer == null || target == null)
            {
                return false;
            }
            
            int distance = mapSystem.GetGridDistanceBetweenElements(observer, target);
            return distance <= observer.VisionDistance && distance <= target.VisionDistance;
        }
        
        public static MapElement[] GetElementsVisibleFrom(this MapSystem mapSystem, GridCell observerCell, int visionRange)
        {
            VisionResult visionResult = mapSystem.GetElementsInVisionRange(new MockObserver(observerCell, visionRange));
            return visionResult.visibleElements.ToArray();
        }
        
        private class MockObserver : MapElement
        {
            public MockObserver(GridCell cell, int vision)
            {
                currentGridCell = cell;
                visionDistance = vision;
            }
            
            public override void OnElementInteraction(MapElement interactor) { }
            public override bool CanBeTraversed() { return true; }
        }
    }
}