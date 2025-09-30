using System.Collections.Generic;
using Grid.Models.Grid;
using MapSystem.Models.Map;
using Grid.Grid;

namespace MapSystem.Navigation
{
    public static class MapNavigationExtensions
    {
        public static bool IsNavigationViable(this MapSystem mapSystem, GridCell fromCell, GridCell toCell)
        {
            if (!mapSystem.IsInitialized)
            {
                return false;
            }
            
            GridConfiguration gridConfig = mapSystem.GridSystem.GetGridConfiguration();
            
            if (!gridConfig.IsValidGridCell(fromCell) || !gridConfig.IsValidGridCell(toCell))
            {
                return false;
            }
            
            return mapSystem.IsCellTraversable(toCell);
        }
        
        public static bool IsNavigationViable(this MapSystem mapSystem, MapElement element, GridCell targetCell)
        {
            if (element == null)
            {
                return false;
            }
            
            return mapSystem.IsNavigationViable(element.CurrentGridCell, targetCell);
        }
        
        public static List<GridCell> GetTraversableCellsAround(this MapSystem mapSystem, GridCell centerCell, int radius = 1)
        {
            List<GridCell> traversableCells = new List<GridCell>();
            
            if (!mapSystem.IsInitialized)
            {
                return traversableCells;
            }
            
            List<GridCell> cellsInRadius = mapSystem.GridSystem.GetCellsInRadius(centerCell, radius);
            
            foreach (GridCell cell in cellsInRadius)
            {
                if (mapSystem.IsCellTraversable(cell))
                {
                    traversableCells.Add(cell);
                }
            }
            
            return traversableCells;
        }
        
        public static List<GridCell> FindSimplePath(this MapSystem mapSystem, GridCell startCell, GridCell targetCell)
        {
            List<GridCell> path = new List<GridCell>();
            
            if (!mapSystem.IsNavigationViable(startCell, targetCell))
            {
                return path;
            }
            
            List<GridCell> lineOfSight = mapSystem.GridSystem.GetCellsInLine(startCell, targetCell);
            
            foreach (GridCell cell in lineOfSight)
            {
                if (!mapSystem.IsCellTraversable(cell))
                {
                    return FindAlternatePath(mapSystem, startCell, targetCell);
                }
                path.Add(cell);
            }
            
            return path;
        }
        
        private static List<GridCell> FindAlternatePath(MapSystem mapSystem, GridCell startCell, GridCell targetCell)
        {
            List<GridCell> path = new List<GridCell>();
            
            int rowDifference = targetCell.row - startCell.row;
            int columnDifference = targetCell.column - startCell.column;
            
            GridCell currentCell = startCell;
            path.Add(currentCell);
            
            while (currentCell != targetCell)
            {
                GridCell nextCell = currentCell;
                
                if (rowDifference != 0)
                {
                    int rowDirection = rowDifference > 0 ? 1 : -1;
                    GridCell candidateCell = new GridCell(currentCell.row + rowDirection, currentCell.column);
                    
                    if (mapSystem.IsCellTraversable(candidateCell))
                    {
                        nextCell = candidateCell;
                        rowDifference -= rowDirection;
                    }
                }
                
                if (nextCell == currentCell && columnDifference != 0)
                {
                    int columnDirection = columnDifference > 0 ? 1 : -1;
                    GridCell candidateCell = new GridCell(currentCell.row, currentCell.column + columnDirection);
                    
                    if (mapSystem.IsCellTraversable(candidateCell))
                    {
                        nextCell = candidateCell;
                        columnDifference -= columnDirection;
                    }
                }
                
                if (nextCell == currentCell)
                {
                    break;
                }
                
                currentCell = nextCell;
                path.Add(currentCell);
                
                if (path.Count > 100)
                {
                    break;
                }
            }
            
            return path;
        }
        
        public static float GetTraversalCost(this MapSystem mapSystem, GridCell fromCell, GridCell toCell)
        {
            if (!mapSystem.IsNavigationViable(fromCell, toCell))
            {
                return float.MaxValue;
            }
            
            MapCell targetMapCell = mapSystem.GetMapCell(toCell);
            if (targetMapCell == null)
            {
                return float.MaxValue;
            }
            
            float baseCost = GridUtilities.GetManhattanDistance(fromCell, toCell);
            return baseCost * targetMapCell.traversalCost;
        }
        
        public static GridCell[] GetAdjacentTraversableCells(this MapSystem mapSystem, GridCell centerCell)
        {
            List<GridCell> traversableCells = new List<GridCell>();
            
            GridResult<GridCell>[] adjacentCells = mapSystem.GridSystem.GetAllAdjacentCells(centerCell);
            
            foreach (GridResult<GridCell> cellResult in adjacentCells)
            {
                if (cellResult.hasValue && mapSystem.IsCellTraversable(cellResult.value))
                {
                    traversableCells.Add(cellResult.value);
                }
            }
            
            return traversableCells.ToArray();
        }
        
        public static bool HasDirectLineOfSight(this MapSystem mapSystem, GridCell fromCell, GridCell toCell)
        {
            List<GridCell> lineOfSight = mapSystem.GridSystem.GetCellsInLine(fromCell, toCell);
            
            foreach (GridCell cell in lineOfSight)
            {
                if (!mapSystem.IsCellTraversable(cell))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public static bool CanMoveToAdjacentCell(this MapSystem mapSystem, MapElement element, GridCell targetCell)
        {
            if (element == null)
            {
                return false;
            }
            
            GridCell currentCell = element.CurrentGridCell;
            
            if (!GridUtilities.AreAdjacentCells(currentCell, targetCell))
            {
                return false;
            }
            
            return mapSystem.IsNavigationViable(currentCell, targetCell);
        }
        
        public static GridCell GetNearestTraversableCell(this MapSystem mapSystem, GridCell targetCell, int maxSearchRadius = 5)
        {
            if (mapSystem.IsCellTraversable(targetCell))
            {
                return targetCell;
            }
            
            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                List<GridCell> cellsInRadius = mapSystem.GridSystem.GetCellsInRadius(targetCell, radius);
                
                GridCell nearestCell = new GridCell(-1, -1);
                float nearestDistance = float.MaxValue;
                
                foreach (GridCell cell in cellsInRadius)
                {
                    if (mapSystem.IsCellTraversable(cell))
                    {
                        float distance = GridUtilities.GetEuclideanDistance(targetCell, cell);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestCell = cell;
                        }
                    }
                }
                
                if (nearestCell.row != -1)
                {
                    return nearestCell;
                }
            }
            
            return new GridCell(-1, -1);
        }
        
        public static int CountTraversableCellsInArea(this MapSystem mapSystem, GridCell topLeft, GridCell bottomRight)
        {
            List<GridCell> cellsInArea = mapSystem.GridSystem.GetCellsInSquareArea(topLeft, bottomRight);
            int traversableCount = 0;
            
            foreach (GridCell cell in cellsInArea)
            {
                if (mapSystem.IsCellTraversable(cell))
                {
                    traversableCount++;
                }
            }
            
            return traversableCount;
        }
        
        public static bool IsAreaFullyTraversable(this MapSystem mapSystem, GridCell topLeft, GridCell bottomRight)
        {
            List<GridCell> cellsInArea = mapSystem.GridSystem.GetCellsInSquareArea(topLeft, bottomRight);
            
            foreach (GridCell cell in cellsInArea)
            {
                if (!mapSystem.IsCellTraversable(cell))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public static float CalculatePathLength(this MapSystem mapSystem, List<GridCell> path)
        {
            if (path == null || path.Count < 2)
            {
                return 0.0f;
            }
            
            float totalLength = 0.0f;
            
            for (int i = 1; i < path.Count; i++)
            {
                totalLength += GridUtilities.GetEuclideanDistance(path[i - 1], path[i]);
            }
            
            return totalLength;
        }
    }
}