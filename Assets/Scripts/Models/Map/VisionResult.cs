using System;
using System.Collections.Generic;
using Grid.Models.Grid;
using MapSystem.Enums;

namespace MapSystem.Models.Vision
{
    [Serializable]
    public struct VisionResult
    {
        public bool hasElements;
        public ViewDirection direction;
        public GridCell observerCell;
        public int visionRange;
        public List<MapElement> visibleElements;
        public List<GridCell> scannedCells;
        
        public VisionResult(ViewDirection direction, GridCell observerCell, int visionRange)
        {
            this.hasElements = false;
            this.direction = direction;
            this.observerCell = observerCell;
            this.visionRange = visionRange;
            this.visibleElements = new List<MapElement>();
            this.scannedCells = new List<GridCell>();
        }
        
        public static VisionResult Success(ViewDirection direction, GridCell observerCell, int visionRange, 
                                         List<MapElement> elements, List<GridCell> cells)
        {
            return new VisionResult
            {
                hasElements = elements.Count > 0,
                direction = direction,
                observerCell = observerCell,
                visionRange = visionRange,
                visibleElements = elements,
                scannedCells = cells
            };
        }
        
        public static VisionResult Empty(ViewDirection direction, GridCell observerCell, int visionRange)
        {
            return new VisionResult
            {
                hasElements = false,
                direction = direction,
                observerCell = observerCell,
                visionRange = visionRange,
                visibleElements = new List<MapElement>(),
                scannedCells = new List<GridCell>()
            };
        }
        
        public MapElement[] GetElementsByType(MapElementType elementType)
        {
            List<MapElement> filteredElements = new List<MapElement>();
            
            foreach (MapElement element in visibleElements)
            {
                if (element.ElementType == elementType)
                {
                    filteredElements.Add(element);
                }
            }
            
            return filteredElements.ToArray();
        }
        
        public int GetElementCount()
        {
            return visibleElements.Count;
        }
        
        public int GetElementCountByType(MapElementType elementType)
        {
            int count = 0;
            foreach (MapElement element in visibleElements)
            {
                if (element.ElementType == elementType)
                {
                    count++;
                }
            }
            return count;
        }
    }
}